using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.SimpleSystemsManagement;

namespace MediaIngester.WorkflowStepFunctions
{
    public class InspectForCelebritiesTask
    {
        private IAmazonRekognition RekognitionClient { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }

        public InspectForCelebritiesTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            RekognitionClient = new AmazonRekognitionClient();
            SSMClient = new AmazonSimpleSystemsManagementClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            if (!state.Keywords.Contains("Person", StringComparer.OrdinalIgnoreCase)
                    && !state.Keywords.Contains("Human", StringComparer.OrdinalIgnoreCase))
            {
                context.Logger.LogLine("Content keywords do not indicate human presence, skipping celebrity check");
                return state;
            }

            switch (state.ContentType)
            {
                case State.ContentTypes.Image:
                    {
                        context.Logger.LogLine($"Content keywords indicate presence of a person, performing celebrity check on image {state.Bucket}::/{state.InputObjectKey}");
                        var response = await RekognitionClient.RecognizeCelebritiesAsync(new RecognizeCelebritiesRequest
                        {
                            Image = new Image
                            {
                                S3Object = new S3Object
                                {
                                    Bucket = state.Bucket,
                                    Name = state.InputObjectKey
                                }
                            }
                        });

                        // like keywords (labels), we restrict ourself to only the first 10
                        foreach (var celeb in response.CelebrityFaces)
                        {
                            state.Celebrities.Add(celeb.Name);
                            if (state.Celebrities.Count == Constants.MaxKeywordsOrCelebrities)
                                break;
                        }
                    }
                    break;

                case State.ContentTypes.Video:
                    {
                        context.Logger.LogLine($"Content keywords indicate presence of a person, starting job to perform celebrity check on video {state.Bucket}::/{state.InputObjectKey}");

                        var asyncJobCompletedTopicArn = await Helpers.GetParameterValue(SSMClient, Constants.AsyncOperationCompletedTopicArnParameterKey);
                        var serviceRoleArn = await Helpers.GetParameterValue(SSMClient, Constants.RekognitionServiceRoleParameterKey);

                        var response = await RekognitionClient.StartCelebrityRecognitionAsync(new StartCelebrityRecognitionRequest
                        {
                            Video = new Video
                            {
                                S3Object = new S3Object
                                {
                                    Bucket = state.Bucket,
                                    Name = state.InputObjectKey
                                }
                            },
                            NotificationChannel = new NotificationChannel
                            {
                                SNSTopicArn = asyncJobCompletedTopicArn,
                                RoleArn = serviceRoleArn
                            }
                        });

                        state.PendingScanResults = State.PendingScans.CelebrityDetection;
                        state.PendingJobId = response.JobId;

                        await Helpers.WriteJobState(new AmazonDynamoDBClient(),
                                                    await Helpers.GetParameterValue(SSMClient, Constants.PendingJobsTableParameterKey),
                                                    response.JobId,
                                                    state);
                    }
                    break;

                default:
                    {
                        context.Logger.LogLine($"InspectForCelebritiesTask incorrectly called for non-image or video content in {state.Bucket}::/{state.InputObjectKey}, skipping inspection");
                    }
                    break;
            }

            return state;
        }
    }
}
