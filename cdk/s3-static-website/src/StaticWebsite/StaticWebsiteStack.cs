using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;

namespace StaticWebsite
{
    public class StaticWebsiteStack : Stack
    {
        public StaticWebsiteStack(Construct parent, string id, IStackProps props) : base(parent, id, props)
        {
            // Create bucket to host a static website; the absence of the BucketName
            // property means the CDK will auto-name the resource
            var bucket = new Bucket(this, "WebsiteBucket", new BucketProps
            {
                PublicReadAccess = true,
                WebsiteIndexDocument = "index.html"
            });

            // deploy the site
            new BucketDeployment(this, "WebsiteBucketDeployment", new BucketDeploymentProps
            {
                DestinationBucket = bucket,
                Sources = new [] { Source.Asset("./site-contents") }
            });

            // emit the url of the website for convenience
            new CfnOutput(this, "BucketWebsiteUrl", new CfnOutputProps
            {
                Value = bucket.BucketWebsiteUrl
            });
        }
    }
}
