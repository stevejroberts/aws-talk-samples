# Serverless Media Ingester

This repository contains a demo application written in C# that uses [AWS Lambda](https://docs.aws.amazon.com/lambda/) and [AWS Step Functions](https://docs.aws.amazon.com/step-functions/) to implement a serverless media ingestion service. Serverless means your code runs without you needing to provision or manage servers and you pay only for the compute time that your code consumes — there’s no charge when your code isn’t running. In addition to demonstrating a serverless application the demo also shows how to use the [AWS SDK for .NET](https://docs.aws.amazon.com/sdk-for-net/) to make calls from within the application to various AWS services. The demo code also illustrates how the various Lambda functions within the application can configure themselves at runtime using configuration data held in [AWS Systems Manager](https://docs.aws.amazon.com/systems-manager/) Parameter Store, enabling the configuration to be changed dynamically without needing to redeploy the application.

## Overview

The serverless application accepts as input a number of different types of media - images, videos, audio (mp3) and text files - which are uploaded to an [Amazon S3](https://docs.aws.amazon.com/s3/) bucket. The bucket is configured to trigger a Lambda function when files are upload or updated. The Lambda functon in turn starts a workflow defined as a state machine in Step Functions to process the file in some way.

For image and video files the step functions perform AI processing using [Amazon Rekognition](https://docs.aws.amazon.com/rekognition/). Amazon Rekognition is a service that makes it easy to add image and video analysis to your applications - you provide an image or video to the Rekognition API, and the service can identify objects, people, text, scenes, and activities. It can detect any inappropriate content as well.

Audio files are converted to text using [Amazon Transcribe](https://docs.aws.amazon.com/transcribe/). Text files are converted to audio using [Amazon Polly](https://docs.aws.amazon.com/polly/). Amazon Transcribe provides transcription services for your audio files. It uses advanced machine learning technologies to recognize spoken words and transcribe them into text. Amazon Polly is a Text-to-Speech (TTS) cloud service that converts text into lifelike speech. You can use Amazon Polly to develop applications that increase engagement and accessibility. Amazon Polly supports multiple languages and includes a variety of lifelike voices, so you can build speech-enabled applications that work in multiple locations and use the ideal voice for your customers.

## Services Used

As noted in the overview above the demo makes use of a number of AWS services, both to host the application in addition to providing the various processes performed on the uploaded files:

* Amazon DynamoDB
* Amazon Polly
* Amazon Rekognition
* Amazon S3
* Amazon Simple Notification Service (SNS)
* Amazon Transcribe
* AWS CloudFormation
* AWS Lambda
* AWS Step Functions
* AWS Systems Manager

## Getting Started

To make getting started with the demo application easy, an AWS CloudFormation template is provided that you can use with the deployment features in the [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/), or from the 'dotnet' CLI, to create the necessary resources and permissions.

### Code Overview

The code in the repository contains a number of important files you may wish to inspect to gain an understanding of how the application is both hosted and works.

TBD

### Deploying with the AWS Toolkit for Visual Studio

TBD

### Deploying with the dotnet CLI

TBD

## Trying the Application

To see the demo application in action, you must first create the necessary 'folder', or key path, in the Amazon S3 bucket created by the template and then upload one or more files to that path in the bucket. This will trigger a Lambda function to run and in turn start the Step Functions workflow to process the file.

TBD

## Cleaning Up

To delete the resources used by this demo application, begin by deleting the CloudFormation stack that created the serverless application resources. Log into the AWS Management Console, then enter **CloudFormation** in the field reading *Find a service by name or feature*. Select the returned entry to go to the CloudFormation dashboard.

The dashboard lists your running stacks. Select the stack that represents the ingester application you started and under *Actions* select **Delete stack**. You will be prompted for confirmation before the stack and the associated resources are deleted.

**Important:** Deleting the stack removes the serverless functions, Step Functions state machine, DynamoDB table and all associated IAM roles and policies. It **does not** delete the Amazon S3 bucket that was used to hold the upload and processed content. You will be charged for this bucket and the content it contains unless you delete it.

Amazon S3 buckets may only be deleted once they contain no objects. Although you can delete the objects one by one, and subsequently the bucket, using the AWS Management Console you may find it simpler to use either the [AWS Toolkit for Visual Studio](https://aws.amazon.com/visualstudio/) or the [AWS Tools for PowerShell](https://aws.amazon.com/powershell/) as they contain features enabling you to delete a bucket in one step regardless of whether it contains content.

> **Note: DO NOT** follow these steps on buckets containing content you wish to keep!

To delete the bucket using the AWS Toolkit for Visual Studio:

1. Open the AWS Explorer from the *View > AWS Explorer* menu item.
1. Ensure the credential profile and region at the top of the explorer match those used to deploy the demo application.
1. Expand the *Amazon S3* node in the explorer's tree.
1. Locate the bucket that was created by the stack - it will have a name similar to *mediaingester-ingestbucket-* followed by some random characters.
1. Right click on the bucket entry in the tree and select **Delete** from the context menu.
1. In the confirmation dialog that is displayed, check the option to delete all objects in the bucket.
1. Click **OK** to proceed. The bucket, and all objects it contains, will now be deleted.

To delete the bucket using the AWS Tools for PowerShell:

1. Start a Windows PowerShell or PowerShell 6 (aka PowerShell Core) command prompt.
1. Import the AWS module:
    1. If using Windows PowerShell, run `Import-Module AWSPowerShell`.
    1. If using PowerShell 6 on Windows, macOS or Linux run `Import-Module AWSPowerShell.NetCore`.
1. If the credential profile you used when deploying the stack in the *Getting Started* section above was not named 'default', run `Set-AWSCredential *profilename*` to load and set the correct credentials in the shell, substituting the profile name as appropriate.
1. Run the command `Get-S3Bucket` to list your available buckets.
1. In the output locate the bucket that was created by the stack - it will have a name similar to *mediaingester-ingestbucket-* followed by some random characters.
1. Run the command `Remove-S3Bucket *bucketname* -DeleteBucketContent -Region *region*`, substituting *bucketname* and *region* as appropriate. You will need to know the AWS region (us-east-1, us-west-2 etc) the bucket was created in. The defaults in the files contained in this repository assume us-west-2.
1. You will be asked to confirm the command. When you do so, the bucket and all it's content will be deleted.

Once the stack and Amazon S3 bucket have been deleted, all resources created by this demo application will have been removed from your account.


