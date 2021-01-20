using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.CodeDeploy;

namespace CICDPipeline
{
    /// <summary>
    /// Application resources stack constructs the VPC, App load balancer and Auto scaling
    /// group that will contain the deployed application.
    /// </summary>
    public class CICDPipelineStack : Stack
    {
        internal CICDPipelineStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            #region Resources for the fleet hosting the application

            var vpc = new Vpc(this, "appVpc", new VpcProps
            {
                MaxAzs = 2
            });

            var userData = UserData.ForLinux();
            userData.AddCommands(new string[]
            {
                "sudo yum install -y httpd",
                "sudo systemctl start httpd",
                "sudo systemctl enable httpd"
            });

            var scalingGroup = new AutoScalingGroup(this, "appASG", new AutoScalingGroupProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                MachineImage = MachineImage.LatestAmazonLinux(new AmazonLinuxImageProps
                {
                    Generation = AmazonLinuxGeneration.AMAZON_LINUX_2
                }),
                MinCapacity = 1,
                MaxCapacity = 4,
                AllowAllOutbound = true,
                UserData = userData
            });

            var alb = new ApplicationLoadBalancer(this, "appLB", new ApplicationLoadBalancerProps
            {
                Vpc = vpc,
                InternetFacing = true
            });

            var albListener = alb.AddListener("Port80Listener", new BaseApplicationListenerProps
            {
                Port = 80
            });

            albListener.AddTargets("Port80ListenerTargets", new AddApplicationTargetsProps
            {
                Port = 80,
                Targets = new [] { scalingGroup }
            });

            albListener.Connections.AllowDefaultPortFromAnyIpv4("Open access to port 80");

            scalingGroup.ScaleOnRequestCount("ScaleOnModestLoad", new RequestCountScalingProps
            {
                TargetRequestsPerMinute = 120
            });

            // the url to the load balancer fronting the application fleet
            var appUrl = new CfnOutput(this, "alb-dns", new CfnOutputProps
            {
                ExportName = "AppUrl",
                Value = alb.LoadBalancerDnsName
            });

            #endregion

            #region CI/CD resources

            var _sourceOutput = new Artifact_("Source");
            var _buildOutput = new Artifact_("Build");

            var build = new PipelineProject(this, "CodeBuild", new PipelineProjectProps
            {
                // relative path to sample app's file (single html page for now) - this is
                // relative to the repo root, btw
                BuildSpec = BuildSpec.FromSourceFilename("cdk/cicd-pipeline/SimplePage/buildspec.yml"),
                Environment = new BuildEnvironment
                {
                    BuildImage = LinuxBuildImage.AMAZON_LINUX_2_2
                },
            });

            var appDeployment = new ServerApplication(this, "appDeployment");
            // we will use CodeDeploy's default one-at-a-time deployment mode as we are
            // not specifying a deployment config
            var deploymentGroup = new ServerDeploymentGroup(this, "appDeploymentGroup", new ServerDeploymentGroupProps
            {
                Application = appDeployment,
                InstallAgent = true,
                AutoRollback = new AutoRollbackConfig
                {
                    FailedDeployment = true
                },
                AutoScalingGroups = new [] { scalingGroup }
            });

            var pipeline = new Pipeline(this, "sampleappPipeline", new PipelineProps
            {
                // fully qualified names as there's also a StageProps type in Amazon.CDK.StageProps
                Stages = new Amazon.CDK.AWS.CodePipeline.StageProps[]
                {
                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Source",
                        Actions = new IAction[]
                        {
                            new GitHubSourceAction(new GitHubSourceActionProps
                            {
                                ActionName = "GitHubSource",
                                Repo = this.Node.TryGetContext("repo-name").ToString(),
                                Owner = this.Node.TryGetContext("repo-owner").ToString(),
                                Branch = this.Node.TryGetContext("repo-branch").ToString(),
                                OauthToken = SecretValue.SecretsManager("github-token"),
                                Output = _sourceOutput
                            })
                        }
                    },

                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Build",
                        Actions = new IAction[]
                        {
                            new CodeBuildAction(new CodeBuildActionProps
                            {
                                ActionName = "Build-app",
                                Project = build,
                                Input = _sourceOutput,
                                Outputs = new Artifact_[] { _buildOutput },
                                RunOrder = 1
                            })
                        }
                    },

                    new Amazon.CDK.AWS.CodePipeline.StageProps
                    {
                        StageName = "Deploy",
                        Actions = new IAction[]
                        {
                            new CodeDeployServerDeployAction(new CodeDeployServerDeployActionProps
                            {
                                ActionName = "Deploy-app",
                                Input = _buildOutput,
                                RunOrder = 2,
                                DeploymentGroup = deploymentGroup
                            })
                        }
                    }
                }
            });

            #endregion
        }
    }
}
