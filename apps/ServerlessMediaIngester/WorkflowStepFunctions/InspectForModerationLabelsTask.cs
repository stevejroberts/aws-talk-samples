using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.DynamoDBv2;

namespace MediaIngester.WorkflowStepFunctions
{
    public class InspectForModerationLabelsTask
    {
        // Rekognition supports moderation for jpg and png image types only (currently)
        readonly List<string> _moderatableImageExtensions = new List<string>
        {
            "jpg", "jpeg", "png"
        };

        private IAmazonRekognition RekognitionClient { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }

        public InspectForModerationLabelsTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            RekognitionClient = new AmazonRekognitionClient();
            SSMClient = new AmazonSimpleSystemsManagementClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            context.Logger.LogLine($"Checking content {state.Bucket}::/{state.InputObjectKey} for unsafe content.");

            var minConfidence = float.Parse(await Helpers.GetParameterValue(SSMClient, Constants.MinConfidenceForKeywordingParameterKey));

            switch (state.ContentType)
            {
                case State.ContentTypes.Image:
                    {
                        if (_moderatableImageExtensions.Contains(state.Extension))
                        {
                            var response = await RekognitionClient.DetectModerationLabelsAsync(new DetectModerationLabelsRequest
                            {
                                Image = new Image
                                {
                                    S3Object = new S3Object
                                    {
                                        Bucket = state.Bucket,
                                        Name = state.InputObjectKey
                                    }
                                },
                                MinConfidence = minConfidence
                            });

                            // if any moderation labels came back at our default confidence level or higher, tag the content
                            // as unsafe and report why
                            state.IsUnsafe = response.ModerationLabels.Any();
                            if (state.IsUnsafe)
                            {
                                context.Logger.LogLine($"WARNING! Content triggered moderation labels below, tagging as unsafe");
                                foreach (var ml in response.ModerationLabels)
                                {
                                    context.Logger.LogLine($"......{ml.Name}, confidence {ml.Confidence}");
                                }
                            }
                            else
                            {
                                context.Logger.LogLine($"Image passed moderation check.");
                            }
                        }
                        else
                        {
                            context.Logger.LogLine($"...image file extension of {state.Extension} is not supported for content moderation, skipped moderation step");
                        }
                    }
                    break;

                case State.ContentTypes.Video:
                    {
                        // moderation checks on video is an async task, so start the job and then setup the
                        // state so we exit the workflow pending notification that the job completes
                        var asyncJobCompletedTopicArn = await Helpers.GetParameterValue(SSMClient, Constants.AsyncOperationCompletedTopicArnParameterKey);
                        var serviceRoleArn = await Helpers.GetParameterValue(SSMClient, Constants.RekognitionServiceRoleParameterKey);

                        var response = await RekognitionClient.StartContentModerationAsync(new StartContentModerationRequest
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
                            },
                            MinConfidence = minConfidence
                        });

                        state.PendingScanResults = State.PendingScans.Moderation;
                        state.PendingJobId = response.JobId;

                        await Helpers.WriteJobState(new AmazonDynamoDBClient(), 
                                                    await Helpers.GetParameterValue(SSMClient, Constants.PendingJobsTableParameterKey),
                                                    response.JobId, 
                                                    state);
                    }
                    break;

                default:
                    break;
            }


            return state;
        }
    }
}
