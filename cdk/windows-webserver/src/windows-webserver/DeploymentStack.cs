using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.CodeDeploy;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.CloudTrail;

namespace WindowsWebServer
{
    public class DeploymentStackProps : StackProps
    {
        public AutoScalingGroup ScalingGroup { get; set; }
    }

    // Sets up a stack containing CodeDeploy resources which will be used to deploy pre-built web server bundles
    // to our web server fleet. A CodePipeline will be used to monitor the S3 bucket and trigger the deployment.
    public class DeploymentStack : Stack
    {
        const string DeploymentApplicationNameContextKey = "DeploymentApplicationName";
        const string DeploymentGroupNameContextKey = "DeploymentGroupName";
        const string AppBundleNameContextKey = "AppBundleName";

        public Bucket DeploymentBucket { get; private set; }
        public string DeploymentApplicationName { get; private set; }
        public ServerDeploymentGroup DeploymentGroup { get; private set; }

        internal DeploymentStack(Construct scope, string id, DeploymentStackProps props = null)
            : base(scope, id, props)
        {
            DeploymentBucket = new Bucket(this, "AppDeploymentBucket");

            // Application and deployment group names are defined in cdk.json; we could choose to override
            // at the command line, for example -
            // cdk deploy -c DeploymentApplicationName=abc -c DeploymentGroupName=xyz).
            var applicationName = (string)this.Node.TryGetContext(DeploymentApplicationNameContextKey);
            var deploymentGroupName = (string)this.Node.TryGetContext(DeploymentGroupNameContextKey);

            var codeDeployApp = new ServerApplication(this, "WindowsWebServerFleetDeploymentApplication", new ServerApplicationProps
            {
                ApplicationName = applicationName
            });

            DeploymentGroup = new ServerDeploymentGroup(this, "WindowsWebServerFleetDeploymentGroup", new ServerDeploymentGroupProps
            {
                Application = codeDeployApp,
                AutoScalingGroups = new AutoScalingGroup[] { props.ScalingGroup },
                DeploymentGroupName = deploymentGroupName,
                InstallAgent = false, // we did this already as part of EC2 instance intitialization userdata
                Role = new Role(this, "WindowsWebServerFleetDeploymentRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("codedeploy.amazonaws.com"),
                    ManagedPolicies = new IManagedPolicy[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole")
                    }
                }),
                DeploymentConfig = ServerDeploymentConfig.ONE_AT_A_TIME
            });

            // Emit the name of the bucket, and the key path (folder), to which app bundles
            // ready for deployment should be uploaded
            new CfnOutput(this, "AppDeploymentBucketName", new CfnOutputProps
            {
                Value = DeploymentBucket.BucketName
            });
            new CfnOutput(this, "AppDeploymentBucketUploadKeyPath", new CfnOutputProps
            {
                Value = applicationName
            });
        }
    }
}
