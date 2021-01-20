using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.SimpleSystemsManagement;

namespace MediaIngester.WorkflowStepFunctions
{
    public class InspectForKeywordLabelsTask
    {
        private IAmazonRekognition RekognitionClient { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }

        public InspectForKeywordLabelsTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            RekognitionClient = new AmazonRekognitionClient();
            SSMClient = new AmazonSimpleSystemsManagementClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var minConfidence = float.Parse(await Helpers.GetParameterValue(SSMClient, Constants.MinConfidenceForKeywordingParameterKey));

            switch (state.ContentType)
            {
                case State.ContentTypes.Image:
                    {
                        context.Logger.LogLine($"Looking for labels (keywords) in image content {state.Bucket}::/{state.InputObjectKey} at or above confidence level {minConfidence}");

                        var response = await RekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
                        {
                            Image = new Image
                            {
                                S3Object = new S3Object
                                {
                                    Bucket = state.Bucket,
                                    Name = state.InputObjectKey
                                }
                            },
                            MinConfidence = float.Parse(await Helpers.GetParameterValue(SSMClient, Constants.MinConfidenceForKeywordingParameterKey))
                        });

                        // choosing to drop the indicated confidence level, and we take only the top 10
                        // to avoid running into tagging limits later in the workflow
                        foreach (var kw in response.Labels)
                        {
                            state.Keywords.Add(kw.Name);
                            if (state.Keywords.Count == Constants.MaxKeywordsOrCelebrities)
                                break;
                        }
                    }
                    break;

                case State.ContentTypes.Video:
                    {
                        context.Logger.LogLine($"Starting async job to check for labels (keywords) in video content {state.Bucket}::/{state.InputObjectKey} at or above confidence level {minConfidence}");

                        var asyncJobCompletedTopicArn = await Helpers.GetParameterValue(SSMClient, Constants.AsyncOperationCompletedTopicArnParameterKey);
                        var serviceRoleArn = await Helpers.GetParameterValue(SSMClient, Constants.RekognitionServiceRoleParameterKey);

                        var response = await RekognitionClient.StartLabelDetectionAsync(new StartLabelDetectionRequest
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

                        state.PendingJobId = response.JobId;
                        state.PendingScanResults = State.PendingScans.Keywording;

                        await Helpers.WriteJobState(new AmazonDynamoDBClient(),
                                                    await Helpers.GetParameterValue(SSMClient, Constants.PendingJobsTableParameterKey),
                                                    response.JobId,
                                                    state);
                    }
                    break;

                default:
                    {
                        context.Logger.LogLine($"InspectForKeywordLabelsTask incorrectly called for non-image or video content in {state.Bucket}::/{state.InputObjectKey}, skipping inspection");
                    }
                    break;
            }

            return state;
        }
    }
}
