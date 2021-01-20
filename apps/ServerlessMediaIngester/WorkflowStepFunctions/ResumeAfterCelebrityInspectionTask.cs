using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaIngester.WorkflowStepFunctions
{
    public class ResumeAfterCelebrityInspectionTask
    {
        private IAmazonRekognition RekognitionClient { get; }

        public ResumeAfterCelebrityInspectionTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            RekognitionClient = new AmazonRekognitionClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var dedupedCelebrities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // choosing to drop the indicated confidence level, and we take only the top 10
            // to avoid running into tagging limits later in the workflow
            string nextToken = null;
            do
            {
                var response = await RekognitionClient.GetCelebrityRecognitionAsync(new GetCelebrityRecognitionRequest
                {
                    JobId = state.PendingJobId,
                    NextToken = nextToken
                });

                nextToken = response.NextToken;

                foreach (var c in response.Celebrities)
                {
                    dedupedCelebrities.Add(c.Celebrity.Name);
                }
            } while (!string.IsNullOrEmpty(nextToken));

            foreach (var celeb in dedupedCelebrities)
            {
                state.Celebrities.Add(celeb);
                if (state.Celebrities.Count == Constants.MaxKeywordsOrCelebrities)
                    break;
            }

            // clear the pending state and allow the workflow to continue
            state.PendingScanResults = State.PendingScans.None;
            state.PendingJobId = null;

            return state;
        }
    }
}
