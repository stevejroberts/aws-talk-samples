using Amazon.CDK;

var app = new App(null);
new S3WebsiteSampleStack(app, "WebsiteStack", new StackProps());
app.Synth();
