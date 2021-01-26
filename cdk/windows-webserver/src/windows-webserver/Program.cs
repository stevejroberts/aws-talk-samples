using Amazon.CDK;

/*
'WindowsWebServerFleetDeploymentStack' depends on 'WindowsWebServerFleetPipelineStack'
(WindowsWebServerFleetDeploymentStack -> WindowsWebServerFleetPipelineStack/WindowsWebServerPipeline/Resource.Ref,
WindowsWebServerFleetDeploymentStack -> WindowsWebServerFleetPipelineStack/WindowsWebServerPipeline/EventsRole/Resource.Arn).

Adding this dependency (WindowsWebServerFleetPipelineStack -> WindowsWebServerFleetDeploymentStack/AppDeploymentBucket/Resource.Arn) would create a cyclic reference.
*/

namespace WindowsWebServer
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // Stands up the hosting infrastructure, a set of EC2 instances in a VPC
            // controlled by an auto scaling group, behind a load balancer.
            var webServerStack = new WebServerStack(app, "WindowsWebServerFleetStack");

            // Stands up CodeDeploy resources that will be used to deploy webdeploy-based
            // app bundles to the fleet.
            var deploymentStack
                = new DeploymentStack(app, "WindowsWebServerFleetDeploymentStack", new DeploymentStackProps
            {
                ScalingGroup = webServerStack.ScalingGroup
            });

            var pipelineStack = new PipelineStack(app, "WindowsWebServerFleetPipelineStack", new PipelineStackProps
            {
                DeploymentApplicationName = deploymentStack.DeploymentApplicationName,
                DeploymentBucket = deploymentStack.DeploymentBucket,
                DeploymentGroup = deploymentStack.DeploymentGroup,
                ScalingGroup = webServerStack.ScalingGroup
            });

            app.Synth();
        }
    }
}
