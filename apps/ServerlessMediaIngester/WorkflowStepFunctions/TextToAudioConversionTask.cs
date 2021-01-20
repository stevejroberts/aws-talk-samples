using System.IO;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleSystemsManagement;

namespace MediaIngester.WorkflowStepFunctions
{
    public class TextToAudioConversionTask
    {
        private IAmazonS3 S3Client { get; }
        private IAmazonSimpleSystemsManagement SSMClient { get; }
        private IAmazonPolly PollyClient { get; }

        public TextToAudioConversionTask()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();
            S3Client = new AmazonS3Client();
            SSMClient = new AmazonSimpleSystemsManagementClient();
            PollyClient = new AmazonPollyClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            context.Logger.LogLine($"Converting text in {state.Bucket}::/{state.InputObjectKey} to audio (mp3)");

            var outputsRootPath = await Helpers.GetParameterValue(SSMClient, Constants.OutputsRootPathParameterKey);
            var voiceId = await Helpers.GetParameterValue(SSMClient, Constants.VoiceIdParameterKey);

            context.Logger.LogLine("...downloading original text content");
            var filename = Path.GetFileName(state.InputObjectKey);
            var tmpPath = Path.Combine("/tmp/", filename);
            await Helpers.DownloadContent(S3Client, state.Bucket, state.InputObjectKey, tmpPath);
            string textToConvert;
            using (var reader = new StreamReader(new FileStream(tmpPath, FileMode.Open)))
            {
                textToConvert = reader.ReadToEnd();
            }

            // for larger files we should really use the StartSpeechSynthesisTask api, and then receive a notification
            // or enter a wait loop (if we know the task will complete in a time less than the max Lambda execution time)
            context.Logger.LogLine($"...converting to mp3 audio with voice {voiceId}");
            var response = await PollyClient.SynthesizeSpeechAsync(new SynthesizeSpeechRequest
            {
                LanguageCode = Amazon.Polly.LanguageCode.EnUS,
                OutputFormat = OutputFormat.Mp3,
                Text = textToConvert,
                TextType = TextType.Text,
                VoiceId = voiceId
            });

            var baseFilename = Path.GetFileNameWithoutExtension(filename);
            state.OutputObjectKey = Path.Combine(outputsRootPath, Constants.ConvertedTextOutputSubPath, baseFilename + ".mp3");
            context.Logger.LogLine($"...writing audio file to {state.Bucket}::/{state.OutputObjectKey}");

            using (var audioStream = new MemoryStream())
            {
                await response.AudioStream.CopyToAsync(audioStream);
                audioStream.Position = 0;
                // for efficiency with data > 5MB, we would want to use the Amazon.S3.Transfer.TransferUtility
                // here
                await S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = state.Bucket,
                    Key = state.OutputObjectKey,
                    InputStream = audioStream
                });

            }

            return state;
        }
    }
}
