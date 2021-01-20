<#
.Synopsis
    Creates an Amazon S3 bucket and configures it to be an event source for
    a Lambda function.

.Description
    Once a Lambda function has been deployed it can be run manually by using
    the Invoke-LMFunction or Invoke-LMFunctionAsync cmdlets.

    The more usual invocation mechanism however is to be triggered by an event.
    In this case we want new objects created in a specific Amazon S3 bucket to
    trigger an event that invokes our Lambda function.

    To allow this we must first create a bucket to hold our objects. We then
    configure our Lambda function to allow S3 to be able to invoke our function
    (this is a separate concern to the role we created earlier). The final step
    is to configure the bucket so that all object creation actions cause a
    notification that triggers our Lambda function to run and receive details
    of the new object(s).

    The bucket will be created in the US East (Virginia) region, us-east-1,
    by default. To change this, pass the required region code to the optional
    Region parameter.

.Example
    .\ConfigureBucketAndTrigger.ps1 -BucketName "mybucket" -FunctionName "MyFunction"
.Example
    .\ConfigureBucketAndTrigger.ps1 -BucketName "mybucket" -FunctionName "MyFunction" -Region ap-southeast-1
#>

[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true, HelpMessage="The name of the bucket to be created")]
    [string]$BucketName,

    [Parameter(Mandatory=$true, HelpMessage="The name of the function to be invoked when objects are created in the bucket")]
    [string]$FunctionName,

    # The region in which to create the bucket. It must be in the same region
    # as the Lambda function that will be invoked.
    [Parameter()]
    [string]$Region
)

###############################################################################
# Import the required AWS module
###############################################################################

Import-Module AWSPowerShell.NetCore

###############################################################################
# If a region was not specified, try and assume one from the shell. If that
# fails, fallback to good ol' us-east-1.
###############################################################################

if (!($Region)) {
    $shellRegion = Get-DefaultAWSRegion
    if ($shellRegion) {
        $Region = $shellRegion.Region
        Write-Host "Assuming use of region $Region based on current shell default"
    } else {
        Write-Host 'No region specified or default set in shell, falling back to use us-east-1'
        $Region = 'us-east-1'
    }
}

###############################################################################
# Create the bucket and configure function permissions to allow S3 to invoke
# our Lambda function
###############################################################################

$bucket = Get-S3Bucket -BucketName $BucketName -Region $Region
if (!($bucket)) {
    Write-Host "Creating bucket $BucketName"
    New-S3Bucket -BucketName $BucketName -Region $Region

    Write-Host 'Configuring bucket permissions to allow Lambda invocation'
    $rand = $functionName + '_' + (Get-Random)
    $permissionsSplat = @{
        'FunctionName'=$FunctionName
        'Action'='lambda:InvokeFunction'
        'Principal'='s3.amazonaws.com'
        'SourceArn'="arn:aws:s3:::$BucketName"
        'StatementId'="$rand"
    }
    Add-LMPermission @permissionsSplat -Region $Region

    # we need the ARN of the function to use in the notification setup call
    $lambdaConfig = Get-LMFunctionConfiguration -FunctionName $FunctionName -Region $Region
    Write-Host "Configuring object creation in bucket $BucketName to invoke function $FunctionName"
    $notificationConfig = @{
        'FunctionArn'=$lambdaConfig.FunctionArn
        'Events'='s3:ObjectCreated:*'
    }

    Write-S3BucketNotification -BucketName $BucketName -LambdaFunctionConfiguration $notificationConfig -Region $Region

    Write-Host "Bucket $BucketName configured for use"
} else {
    Write-Warning "Bucket $BucketName exists, assuming already configured"
}

Write-Host "Bucket $BucketName and Lambda function $FunctionName are now ready to receive data!"
