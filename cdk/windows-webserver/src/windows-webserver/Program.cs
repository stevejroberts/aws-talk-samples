using Amazon.CDK;

namespace WindowsWebServer
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            var webServerStack = new WindowsWebServerStack(app, "WindowsWebServerFleet");

            new WindowsWebServerDeploymentStack(app, "WindowsWebServerFleetDeployment", new WindowsWebServerDeploymentStackProps
            {
                ScalingGroup = webServerStack.ScalingGroup
            });

            app.Synth();
        }
    }
}
