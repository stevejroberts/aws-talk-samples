using System;
using System.Threading.Tasks;

using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

using Amazon.S3;
using System.Threading;
using Amazon.DynamoDBv2;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json;

namespace MediaIngester
{
    internal abstract class Helpers
    {
        public static async Task<string> GetParameterValue(IAmazonSimpleSystemsManagement ssmClient, string parameterName)
        {
            try
            {
                var response = await ssmClient.GetParameterAsync(new GetParameterRequest
                {
                    Name = parameterName
                });

                return response.Parameter.Value;
            }
            catch (AmazonSimpleSystemsManagementException e)
            {
                Console.WriteLine($"Exception retrieving Parameter Store key {parameterName}, message {e.Message}");
                throw;
            }
        }

        public static async Task DownloadContent(IAmazonS3 s3Client, string bucket, string key, string outputFile)
        {
            using (var response = await s3Client.GetObjectAsync(bucket, key))
            {
                await response.WriteResponseStreamToFileAsync(outputFile, false, default(CancellationToken));
            }
        }

        /// <summary>
        /// Retrieves the state associated with an async job that has now completed.
        /// </summary>
        /// <param name="ddbClient"></param>
        /// <param name="tableName"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public static async Task<State> GetJobState(IAmazonDynamoDB ddbClient, string tableName, string jobId)
        {
            var response = await ddbClient.GetItemAsync(new GetItemRequest
            {
                TableName = tableName,
                ProjectionExpression = Constants.PendingJobsWorkflowStateProperty,
                Key = new Dictionary<string, AttributeValue>
                {
                    { Constants.PendingJobsJobIdProperty, new AttributeValue { S = jobId } }
                }
            });

            Console.WriteLine($"Reading workflow state for job {jobId} from table {tableName}");
            return JsonConvert.DeserializeObject<State>(response.Item[Constants.PendingJobsWorkflowStateProperty].S);
        }


        /// <summary>
        /// Having started an async job to process video, persists the id of the job along with the
        /// current state to DynamoDB. The workflow trigger will recover the state when the job
        /// completes and pass it into the workflow enabling us to 'resume' where we left off.
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static async Task WriteJobState(IAmazonDynamoDB ddbClient, string tableName, string jobId, State state)
        {
            var attributes = new Dictionary<string, AttributeValue>
            {
                { Constants.PendingJobsJobIdProperty, new AttributeValue { S = jobId } },
                { Constants.PendingJobsWorkflowStateProperty, new AttributeValue { S = JsonConvert.SerializeObject(state, Formatting.None) } }
            };

            Console.WriteLine($"Persisting workflow state for job {jobId} to table {tableName}");
            await ddbClient.PutItemAsync(new PutItemRequest
            {
                TableName = tableName,
                Item = attributes
            });
        }

        public static async Task RemoveJobState(IAmazonDynamoDB ddbClient, string tableName, string jobId)
        {
            Console.WriteLine($"Deleting job {jobId} from table {tableName}");
            var response = await ddbClient.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { Constants.PendingJobsJobIdProperty, new AttributeValue { S = jobId } }
                }
            });
        }

    }
}
