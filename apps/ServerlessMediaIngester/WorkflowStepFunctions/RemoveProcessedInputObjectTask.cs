using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;

namespace MediaIngester.WorkflowStepFunctions
{
    /// <summary>
    /// Contains Lambda function to delete the original input object once it has been
    /// processed fully by the workflow (or declared unsafe).
    /// </summary>
    public class RemoveProcessedInputObjectTask
    {
        private IAmazonS3 S3Client { get; }

        public RemoveProcessedInputObjectTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            S3Client = new AmazonS3Client();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            if (state.IsUnsafe)
            {
                context.Logger.LogLine($"Removing input object {state.Bucket}::/{state.InputObjectKey} as it was declared unsafe.");
            }
            else
            {
                context.Logger.LogLine($"Removing input object {state.Bucket}::/{state.InputObjectKey} now that it has been fully processed.");
            }

            await S3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = state.Bucket,
                Key = state.InputObjectKey
            });

            return state;
        }
    }
}
