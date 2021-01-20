<#
his script builds and deploys the two Lambda functions in this demo to AWS Lambda
using the AWS extensions for the dotnet CLI.

To run this script you need to have installed:
1. the AWSLambdaPSCore module (to build the deployment package)
2. the Amazon.Lambda.Tools global tools package for the dotnet CLI. To install the
tools package run the command 'dotnet tool install -g Amazon.Lambda.Tools' in
a command shell.

The script takes three parameters:
- (mandatory) the user email to which completion messages should be sent as audio files
  are uploaded to S3.
- text to append to the name of the S3 bucket used to provide input to the process. If not
  specified 'input' is assumed.
- text to append to the name of the S3 bucket to contain the transcribed output. If not
  specified 'output' is assumed.

During deployment you will be asked to provide the name of an S3 bucket to
which the deployment bundle can be uploaded. This bucket must exist in the
same region as the Lambda function (us-west-2 is selected by default in the
aws-lambda-tools-defaults.json file).
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]$UserEmail,

    [Parameter()]
    [string]$InputBucketNameSuffix = 'input',

    [Parameter()]
    [string]$OutputBucketNameSuffix = 'output'
)

New-AWSPowerShellLambdaPackage -ScriptPath ./StartTranscriptionJob.ps1 -OutputPackage ./build/StartTranscriptionJob.zip
New-AWSPowerShellLambdaPackage -ScriptPath ./NotifyTranscriptionComplete.ps1 -OutputPackage ./build/NotifyTranscriptionComplete.zip

$templateParameters = "UserEmail=$UserEmail;InputBucketNameSuffix=$InputBucketNameSuffix;OutputBucketNameSuffix=$OutputBucketNameSuffix"
dotnet lambda deploy-serverless --template-parameters $templateParameters
