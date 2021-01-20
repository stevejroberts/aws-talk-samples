<#
This script builds and deploys the RSS2Speech function to AWS Lambda using the
AWS extensions for the dotnet CLI.

To run this script you need to have installed:
1. the AWSLambdaPSCore module (to build the deployment package)
2. the Amazon.Lambda.Tools global tools package for the dotnet CLI. To install the
tools package run the command 'dotnet tool install -g Amazon.Lambda.Tools' in
a command shell.

The script takes one mandatory parameter, the user email to which completion
messages should be sent as audio files are uploaded to S3.

During deployment you will be asked to provide the name of an S3 bucket to
which the deployment bundle can be uploaded. This bucket must exist in the
same region as the Lambda function (us-west-2 is selected by default in the
aws-lambda-tools-defaults.json file).
#>

[CmdletBinding()]
param (
    # The user's email that will receive notification messages when audio conversions
    # are complete
    [Parameter(Mandatory)]
    [string]$UserEmail
)
New-AWSPowerShellLambdaPackage -ScriptPath ./RSS2Speech.ps1 -OutputPackage ./build/RSS2Speech.zip

dotnet lambda deploy-serverless --template-parameters "UserEmail=$UserEmail"
