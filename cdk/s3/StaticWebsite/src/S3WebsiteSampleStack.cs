using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.IAM;

public class S3WebsiteSampleStack : Stack
{
	public S3WebsiteSampleStack(Construct parent, string id, IStackProps props) : base(parent, id, props)
	{
		// Create a bucket to host a static website. The bucket will be
		// fronted by a CloudFront distribution, so we can keep the bucket
		// contents private.
		var bucket = new Bucket(this, "WebsiteBucket", new BucketProps
		{
			WebsiteIndexDocument = "index.html",

			// Handy for demos, but NOT RECOMMENDED FOR PRODUCTION!
			AutoDeleteObjects = true,
            RemovalPolicy = RemovalPolicy.DESTROY
		});

		// Deploy the site contents
		new BucketDeployment(this, "S3WebsiteSampleDeployment", new BucketDeploymentProps
		{
			DestinationBucket = bucket,

			// To deploy the Blazor client-side sample instead, first publish
			// the Blazor sample to a folder, then update the asset reference
			// folder below, for example
			// Sources = new [] { Source.Asset("./blazor-site-contents/wwwroot") }

			Sources = new [] { Source.Asset("./static-site-contents") }
		});

		// permit CloudFront access to the otherwise private bucket
		var cloudfrontOAI = new OriginAccessIdentity(this, "cloudfront-OAI");

		var policyProps = new PolicyStatementProps
		{
			Actions = new[] { "s3:GetObject" },
			Resources = new[] { bucket.ArnForObjects("*") },
			Principals = new[]
			{
				new CanonicalUserPrincipal
				(
					cloudfrontOAI.CloudFrontOriginAccessIdentityS3CanonicalUserId
				)
			}
		};

		bucket.AddToResourcePolicy(new PolicyStatement(policyProps));

		var distProps = new CloudFrontWebDistributionProps
		{
			OriginConfigs = new[]
			{
				new SourceConfiguration
				{
					S3OriginSource = new S3OriginConfig
					{
						S3BucketSource = bucket,
						OriginAccessIdentity = cloudfrontOAI
					},
					Behaviors = new []
					{
						new Behavior
						{
							IsDefaultBehavior = true,
							Compress = true,
							AllowedMethods = CloudFrontAllowedMethods.GET_HEAD_OPTIONS
						}
					}
				}
			},
			// Require HTTPS between viewer and CloudFront; CloudFront to
			// origin will use HTTP but could also be set to require HTTPS
			ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS
		};

		var distribution = new CloudFrontWebDistribution(this, "SiteDistribution", distProps);

		// Output the url of the CloudFront distribution; if we had a
		// domain name, we could also wire that up instead
		new CfnOutput(this, "websiteUrl", new CfnOutputProps
		{
			Value = $"https://{distribution.DistributionDomainName}"
		});
	}
}
