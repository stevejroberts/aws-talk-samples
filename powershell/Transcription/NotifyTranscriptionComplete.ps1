#==============================================================================
# This Lambda function is triggered by creation of a new object in an Amazon S3
# bucket which is configured as an event source for this function. The object
# in question is the output file from Amazon Transcribe's StartTranscriptionJob
# api containing the transcribed audio. A url to the object is obtained and a
# notification published to an SNS topic which in turn triggers an email to
# a sbscribed user that the job has completed.
#
# Environment parameters to customize this function:
#   ParameterNameRoot       The root key in Parameter Store for all settings
#=============================================================================

# When executing in Lambda the following variables will be predefined.
#   $LambdaInput        A PSObject that contains the Lambda function input data.
#   $LambdaContext      An Amazon.Lambda.Core.ILambdaContext object that contains
#                       information about the currently running Lambda environment.
#
# The last item in the PowerShell pipeline will be returned as the result of the
# Lambda function.

# Note: we're using the modular release of the AWS Tools for PowerShell, which
# has one module per AWS service plus a common shared module. The Lambda tooling
# doesn't currently follow the dependency chain so we have to be explicit and add
# the common module.
#Requires -Modules @{ModuleName='AWS.Tools.Common';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.S3';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.SimpleNotificationService';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.SimpleSystemsManagement';ModuleVersion='4.0.6.0'}

$parameterNameRoot = $env:ParameterNameRoot

$notificationTopic = (Get-SSMParameterValue -Name "$parameterNameRoot/NotificationTopicArn").Parameters[0].Value

foreach ($record in $LambdaInput.Records) {

    $bucket = $record.s3.bucket.name
    $key = $record.s3.object.key

    # if the object was Amazon Transcribe probing write permissions, skip it
    if ($key -eq '.write_access_check_file.temp') {
        Write-Host "Skipping processing of Amazon Transcribe write-access test file"
        continue
    }

    Write-Host "Processing event for: bucket = $bucket, key = $key"

    $presignedUrlArgs = @{
        BucketName = $bucket
        Key = $key
        Expire = ([DateTime]::Now).AddDays(7)
        Verb = 'GET'
        Protocol = 'HTTPS'
        ResponseHeaderOverrides_ContentType = 'text/json'
    }
    $s3uri = Get-S3PreSignedURL @presignedUrlArgs

    Write-Host "Sending completion notification to topic $notificationTopic"
    $publishArgs = @{
        TopicArn = $notificationTopic
        Subject = "Transcription completed"
        Message = "Transcription file $key is now available at $s3uri"
    }
    Publish-SNSMessage @publishArgs
}
