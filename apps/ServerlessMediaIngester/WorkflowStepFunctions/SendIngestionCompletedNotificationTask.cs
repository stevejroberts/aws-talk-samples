using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleSystemsManagement;
using Newtonsoft.Json;

namespace MediaIngester.WorkflowStepFunctions
{
    public class SendIngestionCompletedNotificationTask
    {
        private IAmazonSimpleSystemsManagement SSMClient { get; }
        private IAmazonSimpleNotificationService SNSClient { get; }

        public SendIngestionCompletedNotificationTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            SSMClient = new AmazonSimpleSystemsManagementClient();
            SNSClient = new AmazonSimpleNotificationServiceClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            try
            {
                var topicArn = await Helpers.GetParameterValue(SSMClient, Constants.IngestCompletedTopicArnParameterKey);
                context.Logger.LogLine($"Sending ingest notification for {state.Bucket}::/{state.InputObjectKey}, category {state.ContentType}");

                // Subject is restricted to 100 chars or less
                var subject = $"Ingest completed for {state.Bucket}::/{state.InputObjectKey}";
                if (subject.Length >= 100)
                {
                    subject = "Ingest completed";
                }

                await SNSClient.PublishAsync(new PublishRequest
                {
                    TopicArn = topicArn,
                    Subject = subject,
                    Message = JsonConvert.SerializeObject(state)
                });
            }
            catch (AmazonSimpleSystemsManagementException)
            {
                context.Logger.LogLine($"Notification topic arn not set in parameter {Constants.IngestCompletedTopicArnParameterKey}, skipping notification");
            }

            return state;
        }
    }
}
