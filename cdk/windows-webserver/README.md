# Windows Web Server Sample

This sample constructs a fleet of IIS-based web servers, fronted by a load balancer, with the EC2 instances configured at launch with the required software using a UserData script. Pre-built web applications, as zipped WebDeploy bundles, can be installed to the web servers CodeDeploy.

The sample uses two stacks, and also illustrates sharing of resources between the stacks. The first stack builds the web server fleet itself, inside an Auto Scaling group within a VPC, and fronted by an application load balancer permitting traffic on port 80. This stack also creates a bucket that can be used to upload the webdeploy bundles to, and configures a role permitting access to the bucket by the CodeDeploy agent running on the instances.

The second stack builds the CodeDeploy resources.

The command `cdk list` can be used to discover the names of the stacks contained in the application, which will deploy to your default region.

To deploy the stacks, run the following commands in order.

```bash
cdk deploy WindowsWebServerFleet
cdk deploy WindowsWebServerFleetDeployment
```

The included app, bundled into a zip file with the supporting CodeDeploy scripts, is simply an ASP.NET sample generated from the templates included with Visual Studio and packaged as a web deploy bundle. To upload and deploy the sample app via CodeDeploy, run the following command which uses cmdlets from the AWS Tools for PowerShell. The bucket name can be found in your output from the deployment of the `WindowsWebServerFleet` stack.

```powershell
.\New-CodeDeployment.ps1 -ApplicationName WindowsWebServerFleet -DeploymentBundle .\SampleWebApplication.bundled.zip -BucketName BUNDLE_BUCKET_NAME_HERE -DeploymentGroupName WindowsWebServerFleetDeploymentGroup -WaitForCompletion
```

To verify the deployment, navigate to http://URL OF THE LOAD BALANCER/sample. You should see the homepage of the simple ASP.NET application.

To remove resources, run the `cdk destroy` command one per stack. You will also need to remove the deployment storage bucket, which will require you to first delete all objects it contains. If you have the AWS Tools for PowerShell installed, you can perform this final step with the following command.

```powershell
Remove-S3Bucket -BucketName BUNDLE_BUCKET_NAME_HERE -DeleteAllContent
```
