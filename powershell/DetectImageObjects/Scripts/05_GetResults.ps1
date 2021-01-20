<#
.Synopsis
    Enumerates the objects in the specified Amazon S3 bucket and for each object
    displays the attached tags that are the result of running our Lambda function.
    Optionally the logs from the function invocation(s) can also be displayed.

.Description
    The real output from our function is the tags that were placed on the objects in
    our S3 bucket. These tags correspond to the 'labels' that Amazon Rekognition
    returned to describe the elements it found in each image file we uploaded.

    Lambda functions capture console output from a function (in this case the Write-Host
    output in our sample) and saves the output to log streams in CloudWatch Logs that we
    can subsequently inspect. This is useful for diagnosing issues. If the ShowLogOutput
    switch is set we will obtain from the notification configuration on the bucket the
    details of the Lambda function that was invoked and use them to obtain the log output
    of the function.

    The script assumes the bucket was created in the US East (Virginia) region, us-east-1,
    by default. To change this, pass the required region code to the optional Region
    parameter.

.Example
    .\05_GetResults.ps1 -BucketName "mybucket"

    .Example
    .\05_GetResults.ps1 -BucketName "mybucket" -ShowLogOutput

.Example
    .\05_GetResults.ps1 -BucketName "mybucket" -Region ap-southeast-1
#>

[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true, HelpMessage="The bucket the content was uploaded to")]
    [string]$BucketName,

    [Parameter()]
    [switch]$ShowLogOutput,

    # The region in which the bucket was created.
    [Parameter()]
    [string]$Region = "us-east-1"
)

###############################################################################
# Import the required AWS module
###############################################################################

Import-Module AWSPowerShell.NetCore

###############################################################################
# Begin by listing the tags attached to each object in the bucket - these are
# the result of our Lambda function being invoked when the S3 objects were
# created
###############################################################################

Write-Host "Retrieving tags on objects in bucket, representing what Rekognition detected and it's confidence level"
$objectKeys = (Get-S3Object -BucketName $BucketName -Region $Region).Key

$objectKeys | ForEach-Object {
    $tagSet = $_ | Get-S3ObjectTagSet -BucketName $BucketName -Region $Region
    if ($tagSet) {
        Write-Host "Elements found in $_"
        $tagSet | ForEach-Object {
            Write-Host "    $($_.Key), Confidence $($_.Value)%"
        }
    }
}

###############################################################################
# Optionally output the logs from the function's invocation. We can recover the
# details of the function that ran from the bucket's notification settings.
###############################################################################

if ($ShowLogOutput) {
    Write-Host "Retrieving function invocation logs"

    $notificationsResponse = Get-S3BucketNotification -BucketName $BucketName -Region $Region

    # we're assuming for this demo their is only one notification configured, to
    # our Lambda function
    $lambdaNotification = $notificationsResponse.LambdaFunctionConfigurations | Select-Object -First 1

    if (!($lambdaNotification)) {
        Write-Error "Failed to find expected Lambda notification configuration on bucket $BucketName"
    }

    # now we can get the Amazon Resource Name (ARN) that identifies the Lambda function
    # allowing us to recover the details of the function
    $functionArn = $lambdaNotification.FunctionArn

    Write-Host "Retrieving details of function with ARN $functionArn"
    $lambda = Get-LMFunction -FunctionName $functionArn -Region $Region
    if (!($lambda)) {
        Write-Error "Failed to obtain the function details"
    }

    # retrieve the latest log stream for the function
    $logGroupName = "/aws/lambda/$($lambda.Configuration.FunctionName)"
    Write-Host "Retrieving latest log stream for log group $logGroupName"
    $logstream = Get-CWLLogStream -Descending $true -LogGroupName $logGroupName -Region $Region | Select-Object -First 1
    if ($logstream) {
        Write-Host "Log stream events:"
        (Get-CWLLogEvent -LogGroupName $logGroupName -LogStreamName $logstream.LogStreamName -Region $Region).Events
    } else {
        Write-Error "Failed to obtain latest logstream for log group $logGroupName"
    }
}
