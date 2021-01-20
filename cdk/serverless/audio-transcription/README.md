# Serverless Audio Transcription Sample

This sample shows how to use two .NET Core-based Lambda functions to coordinate audio transcription using AWS Transcribe.

As the CDK does not contain helpers to build and package .NET Core Lambda functions, use the build-and-deploy.ps1 script in the CDK folder to build and package the functions, and create the CDK application stack.

The supplied audio snippet can be used to test. Simply upload it to the input bucket created by the sample, and remember to acknowledge the email subscription message from SNS so that you get a notification email, with a link to the transcribed audio, when transcription is completed.

