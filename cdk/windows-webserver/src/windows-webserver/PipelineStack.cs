using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.CodeDeploy;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.CloudTrail;

namespace WindowsWebServer
{
/*
'WindowsWebServerFleetStack' depends on 'WindowsWebServerFleetDeploymentStack'

WindowsWebServerFleetStack -> WindowsWebServerFleetDeploymentStack/WindowsWebServerPipeline/ArtifactsBucket/Resource.Arn,
WindowsWebServerFleetStack -> WindowsWebServerFleetDeploymentStack/WindowsWebServerPipeline/ArtifactsBucketEncryptionKey/Resource.Arn

Adding this dependency

WindowsWebServerFleetDeploymentStack -> WindowsWebServerFleetStack/WindowsWebServerFleetASG/ASG.Ref

would create a cyclic reference.
*/
    public class PipelineStackProps : StackProps
    {
        public AutoScalingGroup ScalingGroup { get; set; }
        public Bucket DeploymentBucket { get; set; }
        public string DeploymentApplicationName { get; set; }
        public ServerDeploymentGroup DeploymentGroup { get; set; }
    }

    // Sets up a stack containing CodeDeploy resources which will be used to deploy pre-built web server bundles
    // to our web server fleet. A CodePipeline will be used to monitor the S3 bucket and trigger the deployment.
    public class PipelineStack : Stack
    {
        const string AppBundleNameContextKey = "AppBundleName";

        internal PipelineStack(Construct scope, string id, PipelineStackProps props = null)
            : base(scope, id, props)
        {
            var trail = new Trail(this, "WindowsWebServerTrail");
            trail.AddS3EventSelector(
                new []
                {
                    new S3EventSelector
                    {
                        Bucket = props.DeploymentBucket,
                        ObjectPrefix = props.DeploymentApplicationName
                    }
                },
                new AddEventSelectorOptions
                {
                    ReadWriteType = ReadWriteType.WRITE_ONLY
                }
            );

            var sourceOutput = new Artifact_("SourceBundle");
            var appBundleName = (string)this.Node.TryGetContext(AppBundleNameContextKey);

            var pipeline = new Pipeline(this, "WindowsWebServerPipeline", new PipelineProps
            {
                Stages = new []
                {
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "BundleUpload",
                        Actions = new []
                        {
                            new S3SourceAction(new S3SourceActionProps
                            {
                                ActionName = "BundleUpload",
                                RunOrder = 1,
                                Bucket = props.DeploymentBucket,
                                BucketKey = appBundleName,
                                Trigger = S3Trigger.EVENTS,
                                Output = sourceOutput
                            })
                        }
                    },
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "BundleDeployment",
                        Actions = new []
                        {
                            new CodeDeployServerDeployAction(new CodeDeployServerDeployActionProps
                            {
                                ActionName = "DeployViaCodeDeploy",
                                RunOrder = 2,
                                DeploymentGroup = props.DeploymentGroup,
                                Input = sourceOutput
                            })
                        }
                    }
                }
            });

            new CfnOutput(this, "AppDeploymentBundleName", new CfnOutputProps
            {
                Value = appBundleName
            });
        }
    }
}
