using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Newtonsoft.Json.Linq;

namespace MediaIngester.WorkflowStepFunctions
{
    public class AudioToTextConversionTask
    {
        private IAmazonS3 S3Client { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }
        private IAmazonTranscribeService TranscribeClient { get; }

        public AudioToTextConversionTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();
            S3Client = new AmazonS3Client();
            SSMClient = new AmazonSimpleSystemsManagementClient();
            TranscribeClient = new AmazonTranscribeServiceClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            context.Logger.LogLine($"Converting audio in {state.Bucket}::/{state.InputObjectKey} to text");

            var outputsRootKeyPath = await Helpers.GetParameterValue(SSMClient, Constants.OutputsRootPathParameterKey);

            // construct media uri to the S3 object
            var bucketLocationResponse = await S3Client.GetBucketLocationAsync(new GetBucketLocationRequest
            {
                BucketName = state.Bucket
            });

            var bucketLocation = bucketLocationResponse.Location == S3Region.US ? "us-east-1" : bucketLocationResponse.Location.Value;

            // bucket output option doesn't allow you to specify the key so instead accept a uri
            // to the completed transcription which we will then download and transfer to our
            // output bucket
            var jobName = $"{state.Bucket}_{state.InputObjectKey.Replace('/', '_')}_{DateTime.UtcNow.Ticks}";
            context.Logger.LogLine($"...starting transcription job {jobName}");
            await TranscribeClient.StartTranscriptionJobAsync(new StartTranscriptionJobRequest
            {
                LanguageCode = LanguageCode.EnUS,
                Media = new Media
                {
                    MediaFileUri = $"https://s3-{bucketLocation}.amazonaws.com/{state.Bucket}/{state.InputObjectKey}"
                },
                MediaFormat = MediaFormat.Mp3,
                TranscriptionJobName = jobName
            });

            TranscriptionJobStatus jobStatus;
            GetTranscriptionJobResponse jobStatusResponse;
            do
            {
                Thread.Sleep(5000);

                jobStatusResponse = await TranscribeClient.GetTranscriptionJobAsync(new GetTranscriptionJobRequest
                {
                    TranscriptionJobName = jobName
                });

                jobStatus = jobStatusResponse.TranscriptionJob.TranscriptionJobStatus;

                context.Logger.LogLine($"...current job status is {jobStatus}");
            } while (jobStatus == TranscriptionJobStatus.IN_PROGRESS);

            if (jobStatus == TranscriptionJobStatus.COMPLETED)
            {
                var filename = Path.GetFileName(state.InputObjectKey);
                var tmpPath = Path.Combine("/tmp/", filename);
                var baseFilename = Path.GetFileNameWithoutExtension(filename);
                state.OutputObjectKey = Path.Combine(outputsRootKeyPath, Constants.ConvertedAudioOutputSubPath, baseFilename + ".txt");

                var webClient = new WebClient();
                webClient.DownloadFile(jobStatusResponse.TranscriptionJob.Transcript.TranscriptFileUri, tmpPath);

                // the content is a json document
                var jsonObj = JObject.Parse(File.ReadAllText(tmpPath));

                context.Logger.LogLine($"...writing text file to {state.Bucket}::/{state.OutputObjectKey}");
                await S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = state.Bucket,
                    Key = state.OutputObjectKey,
                    ContentBody = (string)jsonObj["results"]["transcripts"][0]["transcript"]
                });
            }
            else
            {
                context.Logger.LogLine($"Audio conversion failed with reason '{jobStatusResponse.TranscriptionJob.FailureReason}'");
            }

            return state;
        }
    }
}
