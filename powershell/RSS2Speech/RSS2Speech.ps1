#==============================================================================
# Query parameters to customize this function:
#  feedUrl (required)   The url of the RSS feed to obtain blog posts from.
#  maxPosts             The maximum number of posts to convert to speech. Defaults
#                       to 1 if not specified.
#  voiceId              Speech conversion voice. Defaults to 'Nicole' if not
#                       specified.
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
#Requires -Modules @{ModuleName='AWS.Tools.Polly';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.S3';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.SimpleNotificationService';ModuleVersion='4.0.6.0'}
#Requires -Modules @{ModuleName='AWS.Tools.SimpleSystemsManagement';ModuleVersion='4.0.6.0'}

$feedUrl = $LambdaInput.queryStringParameters.feedUrl
if (!($feedUrl)) {
    throw "Expected 'feedUrl' query parameter"
}

$maxPosts = $LambdaInput.queryStringParameters.maxPosts
if (!($maxPosts)) {
    $maxPosts = 1
}
$voiceId = $LambdaInput.queryStringParameters.voiceId
if (!($voiceId)) {
    $voiceId = 'Nicole'
}

# Recover the details of the bucket and SNS topic we need to use from Systems Manager's
# Parameter Store. These are set up in the serverles.template file that we will use to
# deploy the function.
# Note that alternatively we could pass these values as environment variables to the function,
# again in the serverless.template file.
$outputBucket = (Get-SSMParameterValue -Name '/RSS2Speech/OutputBucket').Parameters[0].Value
$notificationTopic = (Get-SSMParameterValue -Name '/RSS2Speech/NotificationTopicArn').Parameters[0].Value

$feedUrl = "$feedUrl`?fmt=xml"
Write-Host "Processing feed $feedUrl to return latest $maxPosts item(s) into bucket $outputBucket using voice $voiceId"
$doc = Invoke-RestMethod -Uri $feedUrl

$blogsOutput = 0
$tempPath = [System.IO.Path]::GetTempPath()

$responseBody = ""

while ($blogsOutput -lt $maxPosts) {
    $text = $doc[$blogsOutput].description
    $speech = Get-POLSpeech -VoiceId $voiceId -Text $text -OutputFormat mp3

    $tempFilename = [System.IO.Path]::GetRandomFileName()
    $tempFile = [System.IO.Path]::Combine($tempPath, $tempFilename)

    $fs = [System.IO.FileStream]::new($tempFile, [System.IO.FileMode]::CreateNew)
    $speech.AudioStream.CopyTo($fs)
    $fs.Close()

    $title = $doc[$blogsOutput].title
    $key = "$title.mp3"
    Write-S3Object -BucketName $outputBucket -Key $key -File $tempFile -ContentType "audio/mpeg"

    $presignedUrlArgs = @{
        BucketName = $outputBucket
        Key = $key
        Expire = ([DateTime]::Now).AddDays(7)
        Verb = 'GET'
        Protocol = 'HTTPS'
        ResponseHeaderOverrides_ContentType = 'audio/mpeg'
    }
    $mp3Url = Get-S3PreSignedURL @presignedUrlArgs

    $publishArgs = @{
        TopicArn = $notificationTopic
        Subject = "New Blog Post Available!"
        Message = "Blog $title is now available at $mp3Url"
    }
    Publish-SNSMessage @publishArgs

    $responseBody += "$title - $mp3Url`n"

    $blogsOutput += 1
    if ($doc.Length -le $blogsOutput) {
        break;
    }
}

# Output response to Api Gateway
@{
    'statusCode' = 200;
    'body' = $responseBody;
    'headers' = @{'Content-Type' = 'text/plain'}
}
