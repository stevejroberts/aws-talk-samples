# Transcription Demo

This demo illustrates using a Lambda function to start an asynchronous job (media transcription to text), and the use of another Lambda function to pass a notification to the user that the transcription was complete. Both Lambda functions are triggered by an object being created in S3 buckets. One bucket is used for the input audio file, the second is used by [Amazon Transcribe](https://aws.amazon.com/transcribe/) to write the output text file. The name of the bucket to which the transcribed text should be output, together with the [Amazon Simple Notification Service (SNS)](https://aws.amazon.com/sns) topic ARN from which an email notification will be sent on completion, are recovered from [AWS Systems Manager's](https://aws.amazon.com/systems-manager/) parameter store, at runtime. These parameters are created by the [serverless.template](./serverless.template) file during deployment.

The serverless.template takes three parameters:

1. InputBucketNameSuffix - this text will be appended to the name of the bucket that will contain the uploaded audio files and which triggers the [StartTranscriptionJob.ps1](./StartTranscriptionJob.ps1) Lambda function when an object is created in the bucketTo, or an existing object updated.
1. OutputBucketNameSuffix - this text will be appended to the name of the bucket created to contain the output from Amazon Transcribe. Note that the service does not accept a key path to control placement of the output file and will write the file, plus a test marker to validate the service can write to the bucket, at the bucket's root. You can see the [NotifyTranscriptionComplete.sp1](./NotifyTranscriptionComplete.ps1) Lambda function account for and ignore this marker file.
1. UserEmail - this is the email of the user who will receive notifications that a transcription job has completed. The serverless template file will automatically subscribe this email to the SNS topic. The user should check their inbox after deployment to confirm the subscription.

To deploy the demo run the [build_and_deploy.ps1](./build_and_deploy.ps1) script. It takes three parameters identical to those above. Only the user's email has no default value and is mandatory.

A sample audio clip [sample_audio.mp3](./sample_audio.mp3) is included. To test the Lambda function upload this file to the S3 input bucket.

## Demo Steps

1. Run .\build-and-deploy.ps1
    1. Provide email address
1. Provide name of S3 bucket for build upload when asked
1. REMEMBER to confirm the email subscription!
1. While deployment runs, play the audio file
1. Verify output bucket contains no objects (Get-S3Object)
1. Upload audio file to input bucket
1. Get-AWSCmdletName –service transcribe
1. Get-TRSTranscriptionJobList
    1. NOTE - Can also view transcription job in console if that’s your thing
1. On completion, email sometimes doesn’t arrive in a timely manner, so stick with PowerShell
    1. Get-S3Object on output bucket to find key
    1. Read-S3Object to download
    1. View json output
