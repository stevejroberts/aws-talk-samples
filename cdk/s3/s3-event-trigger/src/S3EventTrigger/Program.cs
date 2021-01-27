using Amazon.CDK;
using System;

namespace S3EventTrigger
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new App(null);
            new S3EventTriggerStack(app, "S3-to-SQS-EventTriggerStack", new StackProps());
            app.Synth();
        }
    }
}
