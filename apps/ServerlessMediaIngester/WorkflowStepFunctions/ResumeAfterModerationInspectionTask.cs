using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System.Linq;
using System.Threading.Tasks;

namespace MediaIngester.WorkflowStepFunctions
{
    public class ResumeAfterModerationInspectionTask
    {
        private IAmazonRekognition RekognitionClient { get; }

        public ResumeAfterModerationInspectionTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            RekognitionClient = new AmazonRekognitionClient();
        }

        /// <summary>
        /// Lambda to recover the results of the async operation to scan for moderated
        /// content on a video.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            string nextToken = null;
            do
            {
                var response = await RekognitionClient.GetContentModerationAsync(new GetContentModerationRequest
                {
                    JobId = state.PendingJobId,
                    NextToken = nextToken
                });

                nextToken = response.NextToken;

                state.IsUnsafe = response.ModerationLabels.Any();
                if (state.IsUnsafe)
                {
                    // note we only emit the first set of moderation labels and then abandon
                    context.Logger.LogLine($"WARNING! Content triggered moderation labels below, tagging as unsafe");
                    foreach (var ml in response.ModerationLabels)
                    {
                        context.Logger.LogLine($"......{ml.ModerationLabel.Name}, confidence {ml.ModerationLabel.Confidence} at {ml.Timestamp}ms");
                    }
                }
            } while (!string.IsNullOrEmpty(nextToken) && !state.IsUnsafe);

            if (!state.IsUnsafe)
            {
                context.Logger.LogLine($"Content passed moderation.");
            }

            // clear the pending state and allow the workflow to continue (or not)
            state.PendingScanResults = State.PendingScans.None;
            state.PendingJobId = null;

            return state;
        }
    }
}
