# Windows Web Server Sample

This sample constructs a fleet of IIS-based web servers, fronted by a load balancer, with the EC2 instances configured at launch with the required software using a UserData script. Pre-built web applications, as zipped WebDeploy bundles, can be installed to the web servers using CodePipeline actions. The actions are configured to trigger automatically when the application bundle is uploaded, with specific key, to the deployment bucket.

The included app, bundled into a zip file with the supporting CodeDeploy scripts, is simply an ASP.NET sample generated from the templates included with Visual Studio and packaged as a web deploy bundle. To upload and deploy the sample app via the CodePipeline, run the following command which uses cmdlets from the AWS Tools for PowerShell. The bucket name can be found in your output from the deployment of the `WindowsWebServerFleetStack` stack.

```powershell
write-s3object DEPLOYMENT_BUCKET_NAME -file .\sampleapp\SampleWebApplication.webdeploy.zip -key WebApplicationBundle.zip
```

To verify the deployment, navigate to http://URL OF THE LOAD BALANCER/sample. You should see the homepage of the simple ASP.NET application.

To remove resources, run the `cdk destroy` command one per stack. You will also need to remove the deployment storage bucket, which will require you to first delete all objects it contains. If you have the AWS Tools for PowerShell installed, you can perform this final step with the following command.

```powershell
Remove-S3Bucket -BucketName DEPLOYMENT_BUCKET_NAME -DeleteAllContent
```
