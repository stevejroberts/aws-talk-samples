<#
.Synopsis
    Starts our log transform workflow on receipt of a new log file in a bucket.

.Description
    This Lambda can be configured to run when a new object is created in an
    S3 bucket. The bucket must be configured as an event source for the Lambda.
    This shows how a Lambda function can in turn execute other Lambda functions or
    Step Functions, they do not need to only respond to events.

    An alternative approach is to use CloudWatch Event Rules to monitor CloudTrail
    notifications when an object is created in a bucket. The event rule then starts
    the state machine when the rule triggers.

    This setup is documented at
    https://docs.aws.amazon.com/step-functions/latest/dg/tutorial-cloudwatch-events-s3.html.
#>

#Requires -Modules @{ModuleName='AWSPowerShell.NetCore';ModuleVersion='3.3.390.0'}

foreach ($record in $LambdaInput.Records) {

    $state = New-Object PSObject –Property @{
        "bucketName"=$record.s3.bucket.name
        "key"=$record.s3.object.key
    }

    Start-SFNExecution -StateMachineArn $env:STATE_MACHINE_ARN -Input (ConvertTo-Json $state)
}
