# Create the package of Lambda function handlers; the output file is referenced in
# our serverless.template file for each function as a relative path to the zip file
New-AWSPowerShellLambdaPackage -ScriptPath ./StateMachineFunctions.ps1 -OutputPackage ./package/StateMachineFunctions.zip

# Deploy the pre-built package as a serverless app; this creates our state machine
# and related artifacts in the template
dotnet lambda deploy-serverless

# Retrieve the state machine arn from the serverless app's stack outputs
$outputs = (Get-CFNStack -StackName logTransformer).Outputs
$stateMachineArn = ($outputs | Where-Object -FilterScript { $_.OutputKey -eq "StateMachineArn" }).OutputValue

# Deploy the trigger Lambda that will run when a log file is uploaded
Publish-AWSPowerShellLambda -Name StartLogTransformWorkflow -ScriptPath .\StartLogTransformWorkflowLambda.ps1 -EnvironmentVariable @{STATE_MACHINE_ARN="$stateMachineArn"}

# Create and configure a bucket to trigger the StartLogTransformWorkflow Lambda when a new object
# is created.
.\ConfigureBucketAndTrigger.ps1 -BucketName YOUR-TEST-BUCKETNAME-HERE -FunctionName StartLogTransformWorkflow

# Trigger the workflow for a single log file

    Write-S3Object -BucketName YOUR-TEST-BUCKETNAME-HERE -File .\TestData\logfile.json

# Trigger the workflow multiple times

    Get-ChildItem .\TestData\logfile.* | % { Write-S3Object -BucketName YOUR-TEST-BUCKETNAME-HERE -File $_ }
