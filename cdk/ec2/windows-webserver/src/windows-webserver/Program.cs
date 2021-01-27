using Amazon.CDK;

namespace WindowsWebServer
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            // Stands up the hosting infrastructure, a set of EC2 instances in a VPC
            // controlled by an auto scaling group, behind a load balancer.
            var webServerStack = new WindowsWebServerStack(app, "WindowsWebServerFleetStack");

            app.Synth();
        }
    }
}
