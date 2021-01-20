using Amazon.CDK;

namespace FargateSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new App(null);

            new FargateSampleStack(app, "FargateSampleStack", new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
                }
            });

            app.Synth();
        }
    }
}
