using Amazon.CDK;

namespace StaticWebsite
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new App(null);
            new StaticWebsiteStack(app, "HelloWorldStack", new StackProps());
            app.Synth();
        }
    }
}
