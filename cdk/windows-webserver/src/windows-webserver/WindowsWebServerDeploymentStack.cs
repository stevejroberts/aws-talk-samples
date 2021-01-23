using Amazon.CDK;
using Amazon.CDK.AWS.AutoScaling;
using Amazon.CDK.AWS.CodeDeploy;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;

namespace WindowsWebServer
{
    public class WindowsWebServerDeploymentStackProps : StackProps
    {
        public AutoScalingGroup ScalingGroup { get;set; }
    }

    public class WindowsWebServerDeploymentStack : Stack
    {
        // Sets up a stack containing CodeDeploy resources which will be used to deploy pre-built web server bundles
        // to our web server fleet. The bundles will be uploaded to the bucket associated with the web server
        // fleet stack, and CodeDeploy then instructed to start a deployment (this sample is not using a full CI/CD
        // setup, but could easily be adapted to do so)
        internal WindowsWebServerDeploymentStack(Construct scope, string id, WindowsWebServerDeploymentStackProps props = null)
            : base(scope, id, props)
        {
            var cdApp = new ServerApplication(this, "webServerFleetDeploymentApplication", new ServerApplicationProps
            {
                ApplicationName = "WindowsWebServerFleet"
            });

            var cdGroup = new ServerDeploymentGroup(this, "webServerFleetDeploymentGroup", new ServerDeploymentGroupProps
            {
                Application = cdApp,
                AutoScalingGroups = new AutoScalingGroup[] { props.ScalingGroup },
                DeploymentGroupName = "WindowsWebServerFleetDeploymentGroup",
                InstallAgent = false, // we did this already as part of EC2 instance intitialization userdata
                Role = new Role(this, "WindowsWebServerFleetDeploymentRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("codedeploy.amazonaws.com"),
                    ManagedPolicies = new IManagedPolicy[] {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSCodeDeployRole")
                    }
                }),
                DeploymentConfig = ServerDeploymentConfig.ONE_AT_A_TIME
            });

        }
    }
}
