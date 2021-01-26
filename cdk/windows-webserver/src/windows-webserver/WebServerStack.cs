using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;

namespace WindowsWebServer
{
    public class WebServerStack : Stack
    {
        // The Auto Scaling group needs to be referenced by our CodeDeploy infrastructure,
        // so is exposed as a property we can reference. If we deploy the optional third stack,
        // a Lambda function that triggers CodeDeploy when a new app bundle is uploaded to S3,
        // then we also need the bucket name

        public AutoScalingGroup ScalingGroup { get; private set; }

        internal WebServerStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Create a new vpc, with one public and one private subnet per AZ, to host the fleet
            var vpc = new Vpc(this, "WindowsWebServerFleetVpc", new VpcProps
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

            // Define an auto scaling group for our instances, which will be placed into
            // private subnets by default
            ScalingGroup = new AutoScalingGroup(this, "WindowsWebServerFleetASG", new AutoScalingGroupProps
            {
                Vpc = vpc,
                InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                // since we don't hold any data on the instance, and install what we need on startup,
                // we just always launch the latest version
                MachineImage = MachineImage.LatestWindows(WindowsVersion.WINDOWS_SERVER_2019_ENGLISH_CORE_BASE),
                MinCapacity = 1,
                MaxCapacity = 4,
                AllowAllOutbound = true,
                Role = new Role(this, "WebServerInstanceRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                    ManagedPolicies = new IManagedPolicy[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole"),
                        ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess")
                    },
                }),
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
            var loadBalancer = new ApplicationLoadBalancer(this, "WindowsWebServerFleetALB", new ApplicationLoadBalancerProps
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
            new CfnOutput(this, "WindowsWebServerFleetUrl", new CfnOutputProps
            {
                Value = loadBalancer.LoadBalancerDnsName
            });
        }
    }
}
