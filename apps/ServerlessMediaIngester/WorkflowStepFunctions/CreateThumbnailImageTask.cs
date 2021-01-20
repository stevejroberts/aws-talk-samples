using System.IO;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement;
using SixLabors.ImageSharp.Processing;

namespace MediaIngester.WorkflowStepFunctions
{
    public class CreateThumbnailImageTask
    {
        private IAmazonS3 S3Client { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }

        public CreateThumbnailImageTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();
            S3Client = new AmazonS3Client();
            SSMClient = new AmazonSimpleSystemsManagementClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            context.Logger.LogLine($"Creating thumbnail for image {state.Bucket}::/{state.InputObjectKey}");

            var outputsRootPath = await Helpers.GetParameterValue(SSMClient, Constants.OutputsRootPathParameterKey);
            var thumbnailsMaxDimensionParameter = await Helpers.GetParameterValue(SSMClient, Constants.ThumbnailsMaxDimensionParameterKey);
            var thumbnailsMaxDimension = 1024;
            if (!string.IsNullOrEmpty(thumbnailsMaxDimensionParameter))
            {
                thumbnailsMaxDimension = int.Parse(thumbnailsMaxDimensionParameter);
            }

            context.Logger.LogLine("...downloading original image");
            var filename = Path.GetFileName(state.InputObjectKey);
            var tmpPath = Path.Combine("/tmp/", filename);
            await Helpers.DownloadContent(S3Client, state.Bucket, state.InputObjectKey, tmpPath);

            using (var sourceImage = SixLabors.ImageSharp.Image.Load(tmpPath))
            {
                ResizeOptions resizeOptions = null;
                if (sourceImage.Height > thumbnailsMaxDimension || sourceImage.Width > thumbnailsMaxDimension)
                {
                    resizeOptions = new ResizeOptions
                    {
                        Size = new SixLabors.Primitives.Size { Height = thumbnailsMaxDimension, Width = thumbnailsMaxDimension },
                        Mode = ResizeMode.Max
                    };
                }

                if (resizeOptions != null)
                {
                    context.Logger.LogLine($"...creating thumbnail with max dimension of {thumbnailsMaxDimension} pixels");
                    var imageBuffer = new MemoryStream();

                    sourceImage.Mutate(x => x.Resize(resizeOptions));
                    sourceImage.Save(imageBuffer, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());

                    imageBuffer.Position = 0;

                    state.OutputObjectKey = Path.Combine(outputsRootPath, Constants.ImageThumbnailsOutputSubPath, filename);
                    context.Logger.LogLine($"...writing resized image to {state.Bucket}::/{state.OutputObjectKey}");
                    await S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = state.Bucket,
                        Key = state.OutputObjectKey,
                        InputStream = imageBuffer
                    });
                }
                else
                {
                    context.Logger.LogLine($"...original image is smaller than max thumbnail dimension of {thumbnailsMaxDimension} pixels, skipping thumbnail creation");
                    state.OutputObjectKey = Path.Combine(outputsRootPath, Constants.ImageThumbnailsOutputSubPath, filename);
                    context.Logger.LogLine($"...copying original image to {state.Bucket}::/{state.OutputObjectKey}");
                    await S3Client.CopyObjectAsync(new CopyObjectRequest
                    {
                        SourceBucket = state.Bucket,
                        SourceKey = state.InputObjectKey,
                        DestinationBucket = state.Bucket,
                        DestinationKey = state.OutputObjectKey
                    });
                }
            }

            return state;
        }
    }
}
