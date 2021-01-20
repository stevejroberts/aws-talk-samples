using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaIngester.WorkflowStepFunctions
{
    public class CopyAndTagSourceContentTask
    {
        private IAmazonS3 S3Client { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }

        public CopyAndTagSourceContentTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            S3Client = new AmazonS3Client();
            SSMClient = new AmazonSimpleSystemsManagementClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var outputsRootKeyPath = await Helpers.GetParameterValue(SSMClient, Constants.OutputsRootPathParameterKey);

            var sourceFilename = Path.GetFileName(state.InputObjectKey);
            string targetKeyPath = null;
            switch (state.ContentType)
            {
                case State.ContentTypes.Image:
                    {
                        targetKeyPath = Path.Combine(outputsRootKeyPath, Constants.ImagesOutputSubPath, sourceFilename);
                    }
                    break;

                case State.ContentTypes.Video:
                    {
                        targetKeyPath = Path.Combine(outputsRootKeyPath, Constants.VideosOutputSubPath, sourceFilename);
                    }
                    break;

                default:
                    break;
            }

            if (!string.IsNullOrEmpty(targetKeyPath))
            {
                context.Logger.LogLine($"Copying {state.Bucket}::/{state.InputObjectKey} to {targetKeyPath} with {state.Keywords.Count} keyword and {state.Celebrities.Count} celebrity tags");
                var tags = new List<Tag>();

                // S3 limits each object to 10 tags, so instead apply a 'keywords' and 'celebrity' tag pair with values
                // containing the comma delimited set of values
                tags.Add(new Tag
                {
                    Key = Constants.KeywordsTagKey,
                    Value = string.Join('/', state.Keywords)
                });
                tags.Add(new Tag
                {
                    Key = Constants.CelebritiesTagKey,
                    Value = string.Join('/', state.Celebrities)
                });

                await S3Client.CopyObjectAsync(new CopyObjectRequest
                {
                    SourceBucket = state.Bucket,
                    SourceKey = state.InputObjectKey,
                    DestinationBucket = state.Bucket,
                    DestinationKey = targetKeyPath,
                    TagSet = tags
                });
            }
            else
            {
                context.Logger.LogLine($"Object {state.Bucket}::/{state.InputObjectKey} is not recognized as video or image media, skipping source copy step.");
            }

            return state;
        }
    }
}
