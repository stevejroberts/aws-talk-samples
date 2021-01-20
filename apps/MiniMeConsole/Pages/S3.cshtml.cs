using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.Extensions.Logging;

namespace MiniMeConsole.Pages
{
    public class S3Model : PageModel
    {
        private const string USEast1RegionCode = "us-east-1";

        private ILogger<S3Model> Logger { get; }

        private IAmazonS3 S3Client { get; }

        public List<S3Bucket> Buckets { get; private set; }

        public S3Bucket SelectedBucket { get; private set; }

        public string BucketRegion { get; private set; }

        public List<S3Object> BucketObjects { get; private set; }

        public S3Model(IAmazonS3 s3Client, ILogger<S3Model> logger)
        {
            S3Client = s3Client;
            Logger = logger;
        }

        public async Task OnGet(string bucketName)
        {
            try
            {
                var listResponse = await S3Client.ListBucketsAsync();
                Buckets = new List<S3Bucket>(listResponse.Buckets);

                if (!string.IsNullOrEmpty(bucketName))
                {
                    SelectedBucket = Buckets.FirstOrDefault(b => string.Equals(b.BucketName, bucketName));
                    if (SelectedBucket != null)
                    {
                        var locationResponse = await S3Client.GetBucketLocationAsync(bucketName);
                        BucketRegion = locationResponse.Location?.Value;
                        if (string.IsNullOrEmpty(BucketRegion))
                        {
                            BucketRegion = USEast1RegionCode;
                        }

                        // buckets must be addressed with a region-compatible client
                        using (var regionalClient = new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(BucketRegion)))
                        {
                            var objectsResponse = await regionalClient.ListObjectsAsync(bucketName);
                            // list up to 20 objects
                            var take = (objectsResponse.S3Objects.Count > 20) ? 20 : objectsResponse.S3Objects.Count;
                            BucketObjects = new List<S3Object>(objectsResponse.S3Objects.Take(take));
                        }
                    }
                }
            }
            catch (AmazonS3Exception e)
            {
                Logger.LogError(e, "Failed to access S3 API");
            }
        }
    }
}