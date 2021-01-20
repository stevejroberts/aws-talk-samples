using Amazon.Lambda.Core;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaIngester.WorkflowStepFunctions
{
    public class ResumeAfterKeywordInspectionTask
    {
        private IAmazonRekognition RekognitionClient { get; }

        public ResumeAfterKeywordInspectionTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            RekognitionClient = new AmazonRekognitionClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var dedupedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // choosing to drop the indicated confidence level, and we take only the top 10
            // to avoid running into tagging limits later in the workflow
            string nextToken = null;
            do
            {
                var response = await RekognitionClient.GetLabelDetectionAsync(new GetLabelDetectionRequest
                {
                    JobId = state.PendingJobId,
                    NextToken = nextToken
                });

                nextToken = response.NextToken;

                foreach (var lbl in response.Labels)
                {
                    dedupedLabels.Add(lbl.Label.Name);
                }
            } while (!string.IsNullOrEmpty(nextToken));

            foreach (var lbl in dedupedLabels)
            {
                state.Keywords.Add(lbl);
                if (state.Keywords.Count == Constants.MaxKeywordsOrCelebrities)
                    break;
            }

            // clear the pending state and allow the workflow to continue
            state.PendingScanResults = State.PendingScans.None;
            state.PendingJobId = null;

            return state;
        }
    }
}
