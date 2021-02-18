# Object Detection Sample

This sample shows how to use Amazon Rekognition to detect objects (referred to as Labels) in image files, and apply those labels as tags to the S3 object that triggered the invocation of the function.

## Demo Steps

1. Run .\build-and-deploy.ps1
    1. Optional – add –MinConfidence {number}, defaults to 70
1. While deployment runs, show the two sample image files
1. Upload one of the images
1. How do we know it’s working? Let’s check invocation
    1. Query Lambda cmdlets – point out the Invoke cmdlets, also Get-LMFunctionList, Get-LMFunction, and Get-LMFunctionConfiguration (or use (Get-LMFunction).Configuration)
    1. However, there’s nothing to get invocation details so…
    1. Lambda function invocations get recorded in CloudWatch Logs (as well as function output via Write-Host), so let’s look there
    1. Query CloudWatch cmdlets – Get-CWLLogGroup, Get-CWLLogStream, Get-CWLLogEvent (NOTE: make sure to escape the $ in the stream name), should see output from the function
1. Check object tags – Get-S3ObjectTagSet
