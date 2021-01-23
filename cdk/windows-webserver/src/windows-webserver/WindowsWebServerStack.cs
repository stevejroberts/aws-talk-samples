using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;

namespace WindowsWebServer
{
    public class WindowsWebServerStack : Stack
    {
        // The Auto Scaling group needs to be referenced by our CodeDeploy infrastructure,
        // so is exposed as a property we can reference
        public AutoScalingGroup ScalingGroup {get; private set; }

        internal WindowsWebServerStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Create a new vpc, with one public and one private subnet per AZ, to host the fleet
            var vpc = new Vpc(this, "fleetVpc", new VpcProps
            {
                Cidr = "10.0.0.0/16",
                MaxAzs = 2,
                SubnetConfiguration = new SubnetConfiguration[]
                {
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        SubnetType = SubnetType.PUBLIC,
                        Name = "PublicIngress"
                    },
                    new SubnetConfiguration
                    {
                        CidrMask = 24,
                        SubnetType = SubnetType.PRIVATE,
                        Name = "Private"
                    }
                }
            });

            // Bucket used to hold the built application bundles to be installed using the
            // CodeDeploy agent running on the instance(s). The EC2 instance role will have
            // permissions scoped to enable the agent running on the instance to reach this
            // bucket during deployments.
            var deploymentBundleBucket = new Bucket(this, "deploymentBundleBucket");

            // Define a role for the instance(s) in the fleet. This role permits access to the S3
            // bucket for the deployment region that contains the CodeDeploy agent install file,
            // and to the bucket created above where we will place our built application bundles
            // during deploymemt
            var instanceRole = new Role(this, "webServerInstanceRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole"),
                },
            });

            instanceRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Resources = new string[]
                {
                    $"{deploymentBundleBucket.BucketArn}/*",
                    $"arn:aws:s3:::aws-codedeploy-{this.Region}/*"
                },
                Actions = new string[]
                {
                    "s3:Get*",
                    "s3:List*"
                }
            }));

            // Define an auto scaling group for our instances, which will be placed into
            // private subnets by default
            ScalingGroup = new AutoScalingGroup(this, "appASG", new AutoScalingGroupProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                // since we don't hold any data on the instance, and install what we need on startup,
                // we just always launch the latest version
                MachineImage = MachineImage.LatestWindows(WindowsVersion.WINDOWS_SERVER_2019_ENGLISH_CORE_BASE),
                MinCapacity = 1,
                MaxCapacity = 4,
                AllowAllOutbound = true,
                Role = instanceRole,
                Signals = Signals.WaitForCount(1, new SignalsOptions
                {
                    Timeout = Duration.Minutes(10)
                })
            });

            // Initialize the vanilla EC2 instances with our needed software at launch time using a UserData script
            ScalingGroup.AddUserData(new string[]
            {
                // install and configure IIS with ASP.NET 4.6
                "Install-WindowsFeature -Name Web-Server,NET-Framework-45-ASPNET,NET-Framework-45-Core,NET-Framework-45-Features",
                // install WebDeploy, which I've found to be easiest using chocolatey
                "Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))",
                "choco install webdeploy -y",
                // download and install the CodeDeploy agent; note that the AddS3DownloadCommand method on UserData does
                // exactly what's below with Read-S3Object
                $"Read-S3Object -BucketName aws-codedeploy-{this.Region} -Key latest/codedeploy-agent.msi -File c:/temp/codedeploy-agent.msi",
                "c:/temp/codedeploy-agent.msi /quiet /l c:/temp/host-agent-install-log.txt",
            });

            ScalingGroup.UserData.AddSignalOnExitCommand(ScalingGroup);

            // Configure an application load balancer, listening on port 80, to front our fleet
            var loadBalancer = new ApplicationLoadBalancer(this, "webserversLB", new ApplicationLoadBalancerProps
            {
                Vpc = vpc,
                InternetFacing = true
            });

            var lbListener = loadBalancer.AddListener("Port80Listener", new BaseApplicationListenerProps
            {
                Port = 80
            });

            lbListener.AddTargets("Port80ListenerTargets", new AddApplicationTargetsProps
            {
                Port = 80,
                Targets = new [] { ScalingGroup }
            });

            lbListener.Connections.AllowDefaultPortFromAnyIpv4("Open access to port 80");

            // Can only be set after the group has been attached to a load balancer
            ScalingGroup.ScaleOnRequestCount("DemoLoad", new RequestCountScalingProps
            {
                TargetRequestsPerMinute = 10 // enough for demo purposes
            });

            // Emit the url to the load balancer fronting the application fleet
            new CfnOutput(this, "loadBalancerUrl", new CfnOutputProps
            {
                Value = loadBalancer.LoadBalancerDnsName
            });
            // Emit the name of the bucket that should be used to upload our built application
            // bundles, that we will then ask CodeDeploy to deploy to the instance(s) in the fleet
            new CfnOutput(this, "deploymentBundleBucketName", new CfnOutputProps
            {
                Value = deploymentBundleBucket.BucketName
            });
        }
    }
}
