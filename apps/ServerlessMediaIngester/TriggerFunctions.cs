using System;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.SNSEvents;

using Amazon.S3;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.DynamoDBv2;

using Amazon.XRay.Recorder.Handlers.AwsSdk;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace MediaIngester
{
    // Defines the Lambda function handlers that triggers our step functions workflow
    // to start running, first by responding to an S3 event that an object has been created
    // or updated in a bucket and secondly in response to an SNS notification that some async
    // task has completed and the workflow can 'resume' where it left off.
    public class TriggerFunctions
    {
        IAmazonS3 S3Client { get; set; }
        IAmazonStepFunctions StepFunctionsClient { get; }
        IAmazonSimpleSystemsManagement SystemsManagerClient { get; }
        IAmazonDynamoDB DynamoDBClient { get; }

        HashSet<string> ObjectKeysToIgnore { get; } = new HashSet<string>(StringComparer.Ordinal);
        const string VsToolkitFolderMarkerSuffix = "$folder$";

        public TriggerFunctions()
        {
            AWSSDKHandler.RegisterXRayForAllServices();

            S3Client = new AmazonS3Client();
            StepFunctionsClient = new AmazonStepFunctionsClient();
            SystemsManagerClient = new AmazonSimpleSystemsManagementClient();
            DynamoDBClient = new AmazonDynamoDBClient();
        }

        /// <summary>
        /// Can be used for testing
        /// </summary>
        public TriggerFunctions(IAmazonS3 s3Client, IAmazonStepFunctions stepFunctionsClient, IAmazonSimpleSystemsManagement systemsManagerClient, IAmazonDynamoDB dynamoDBClient)
        {
            this.S3Client = s3Client;
            this.StepFunctionsClient = stepFunctionsClient;
            this.SystemsManagerClient = systemsManagerClient;
            this.DynamoDBClient = dynamoDBClient;
        }

        /// <summary>
        /// The Lambda function that is invoked due to an object being created or updated
        /// in an S3 bucket that is configured as an event source. It constructs the json
        /// payload that represents our state for the step function's state machine, then 
        /// started execution of the workflow.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        public async Task NewObjectHandler(S3Event evnt, ILambdaContext context)
        {
            var stateMachineArn = await Helpers.GetParameterValue(SystemsManagerClient, Constants.StateMachineArnParamStoreKey);

            // if the bucket paths are created using the AWS Visual Studio toolkit,
            // 'hidden' marker files denoting the keypath hierarchy to allow the toolkit
            // to show as a file system get created, which we want to ignore
            var inputsRootPath = await Helpers.GetParameterValue(SystemsManagerClient, Constants.InputsRootPathParameterKey);

            ObjectKeysToIgnore.Add(inputsRootPath);
            ObjectKeysToIgnore.Add($"{inputsRootPath}/{inputsRootPath}_{VsToolkitFolderMarkerSuffix}");

            foreach (var s3Event in evnt.Records)
            {
                var originalKey = System.Net.WebUtility.UrlDecode(s3Event.S3.Object.Key);

                if (ObjectKeysToIgnore.Contains(originalKey))
                {
                    context.Logger.LogLine($"Skipping marker object from AWS VS toolkit {originalKey}");
                    continue;
                }

                context.Logger.LogLine($"Starting workflow for object {s3Event.S3.Bucket.Name}::/{originalKey}");
                await StepFunctionsClient.StartExecutionAsync(new StartExecutionRequest
                {
                    Name = MakeSafeWorkflowName(originalKey), // must be unique for 90 days
                    StateMachineArn = stateMachineArn,
                    Input = JsonConvert.SerializeObject(new State
                    {
                        Bucket = s3Event.S3.Bucket.Name,
                        InputObjectKey = originalKey
                    })
                });
            }
        }

        /// <summary>
        /// The Lambda function that is invoked when we receive a notification from an SNS topic
        /// that an async operation on a video has completed and we can 'resume' the workflow
        /// from where we left off.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ResumeWorkflowHandler(SNSEvent evnt, ILambdaContext context)
        {
            var stateMachineArn = await Helpers.GetParameterValue(SystemsManagerClient, Constants.StateMachineArnParamStoreKey);
            var pendingJobsTableName = await Helpers.GetParameterValue(SystemsManagerClient, Constants.PendingJobsTableParameterKey);

            foreach (var snsRecord in evnt.Records)
            {
                string jobId = string.Empty;

                try
                {
                    var payload = JObject.Parse(snsRecord.Sns.Message);
                    jobId = (string)payload[Constants.JobCompletionMessageJobIdField];
                    context.Logger.LogLine($"Retrieving workflow state for job id {jobId}");
                    var workflowState = await Helpers.GetJobState(DynamoDBClient, pendingJobsTableName, jobId);

                    var status = (string)payload[Constants.JobCompletionMessageStatusField];
                    context.Logger.LogLine($"Job {jobId} completed with status {status}");

                    if (status == "SUCCEEDED")
                    {
                        context.Logger.LogLine($"Restarting workflow for object {workflowState.Bucket}::/{workflowState.InputObjectKey} after {Enum.GetName(typeof(State.PendingScans), workflowState.PendingScanResults)} scan");
                        await StepFunctionsClient.StartExecutionAsync(new StartExecutionRequest
                        {
                            Name = MakeSafeWorkflowName(workflowState.InputObjectKey), // must be unique for 90 days
                            StateMachineArn = stateMachineArn,
                            Input = JsonConvert.SerializeObject(workflowState)
                        });
                    }
                    else
                    {
                        context.Logger.LogLine($"Job {jobId} on object {workflowState.Bucket}::/{workflowState.InputObjectKey} failed, cancelling further processing");
                    }
                }
                catch (Exception e)
                {
                    context.Logger.LogLine($"Exception restarting workflow for job {jobId}, error: {e.Message}");
                }
                finally
                {
                    await Helpers.RemoveJobState(DynamoDBClient, pendingJobsTableName, jobId);
                }
            }
        }

        private string MakeSafeWorkflowName(string objectKey)
        {
            // am disregarding unicode and control chars here
            var disallowedChars = new HashSet<char>(" <>{}[]?*\"#%\\^|~`$&,;:/".ToCharArray());
            var sb = new StringBuilder();
            foreach (var c in Path.GetFileName(objectKey))
            {
                if (!disallowedChars.Contains(c))
                {
                    sb.Append(c);
                }
            }

            sb.Append(DateTime.UtcNow.Ticks);
            return sb.Length > 80 ? sb.ToString(0, 80) : sb.ToString();
        }

    }
}
