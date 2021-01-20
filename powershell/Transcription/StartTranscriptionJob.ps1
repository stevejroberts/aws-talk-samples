#==============================================================================
# This Lambda function to schedule a transcription of an audio or video to text
# is triggered by creation of a new object in an Amazon S3 bucket which is
# configured as an event source for this function. The function passes details
# of the object to Amazon Transcribe to perform the conversion, which is an
# async job, before exiting. The transcription output from the job is written
# to another S3 bucket. This in turn will trigger another Lambda function to
# run to notify the user that the transcription job has completed, with a link
# to the output file.
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
#Requires -Modules @{ModuleName='AWS.Tools.TranscribeService';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.SimpleSystemsManagement';ModuleVersion='4.0.6.0'}

$allowedExtensions = "mp3","mp4","flac","wav"

$parameterNameRoot = $env:ParameterNameRoot

$outputBucket = (Get-SSMParameterValue -Name "$parameterNameRoot/OutputBucket").Parameters[0].Value

foreach ($record in $LambdaInput.Records) {

    $bucket = $record.s3.bucket.name
    $key = $record.s3.object.key

    Write-Host "Processing event for: bucket = $bucket, key = $key"

    $ext = [System.IO.Path]::GetExtension($key).Trim('.').ToLower()

    if ($allowedExtensions -notcontains $ext) {
        ThrowError "Invalid object extension $ext, only $allowedExtensions are supported"
    }

    $s3uri = "https://$bucket`.s3.$($env:AWS_REGION).amazonaws.com/$key"
    Write-Host "Starting transcription on $s3uri"

    # job name has to be unique; note that spaces in key name come through as +,
    # which is not a valid character
    $jobName = $key.Replace('+','') + [DateTime]::UtcNow.Ticks
    $args = @{
        Media_MediaFileUri = $s3uri
        TranscriptionJobName = $jobName
        MediaFormat = $ext
        LanguageCode = 'en-US'
        OutputBucketName = $outputBucket
    }
    Write-Host "Starting transcription job $jobName"
    Start-TRSTranscriptionJob @args
}
