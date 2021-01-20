<#
.Synopsis
    Cleans up the resources used by the demo.

.Description
    The preceeding scripts created an IAM role, a Lambda function and an S3
    bucket. In addition the invocation of our Lambda function will have created
    log streams in CloudWatch Logs. This script cleans up the demo resources.

    The log streams are automatically deleted, without user interaction, from
    the Lambda function. Then we attempt to delete the Lambda function, the role
    and finally the S3 bucket. The removal of these resources requires the user
    to confirm.

    The name of the Lambda function and the role that was created to scope it are
    automatically discovered by the script by following the notification settings
    configured on the bucket. They do not need to be supplied as parameters.

    The script assumes the resources were created in the US East (Virginia) region,
    us-east-1, by default. To change this, pass the required region code to the
    optional Region parameter.

.Example
    .\06_PostDemoCleanup.ps1 -BucketName "mybucket"

.Example
    .\06_PostDemoCleanup.ps1 -BucketName "mybucket" -Region ap-southeast-1
#>

[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true, HelpMessage="The name of the bucket that was created.")]
    [string]$BucketName,

    # The region in which the resources were created
    [Parameter()]
    [string]$Region = "us-east-1"
)

###############################################################################
# Import the required AWS module
###############################################################################

Import-Module AWSPowerShell.NetCore

###############################################################################
# Recover the bucket's notification settings and use them to obtain the
# Lambda function details, which includes details of the role that was created.
###############################################################################

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

# final step is to retrieve details of the role associated with the function
$roleArn = $lambda.Configuration.Role
Write-Host "Retrieving details role with ARN $roleArn"
$role = (Get-IAMRoleList -Region $Region | Where-Object { $_.Arn -eq $roleArn })
if (!($role)) {
    Write-Error "Failed to obtain the role details"
}

###############################################################################
# Now we have the details we need, we can start resource deletion
###############################################################################

# First delete the log streams associated with the function
Write-Host "Removing CloudWatch Logs log group for function"
Remove-CWLLogGroup -LogGroupName "/aws/lambda/$($lambda.Configuration.FunctionName)"

# Now delete the function, with use permission
Write-Host "Removing Lambda function"
Remove-LMFunction -FunctionName $lambda.Configuration.FunctionName -Region $Region

# Remove the role that was associated with the now-deleted function, with user
# permission. Note that before we can fully remove the role, we need to remove
# the inline policy that we attached allowing access to S3's PutObjectTagging API
# and also detatch the two managed policies that we added allowing our function
# to execute against S3 and CloudWatch logs under an assumed role.
$inlinePolicies = Get-IAMRolePolicyList -RoleName $role.RoleName -Region $Region
$inlinePolicies | ForEach-Object { Remove-IAMRolePolicy -RoleName $role.RoleName -PolicyName $_ -Region $Region }

$managedPolicies = Get-IAMAttachedRolePolicyList -RoleName $role.RoleName -Region $Region
$managedPolicies | ForEach-Object { Unregister-IAMRolePolicy -RoleName $role.RoleName -PolicyArn $_.PolicyArn -Region $Region }

# with all policies detatched or removed we can now remove the role itself
Remove-IAMRole -RoleName $role.RoleName -Region $Region

# Remove the bucket, and content -- careful! -- with user permission
Remove-S3Bucket -BucketName $BucketName -Region $Region -DeleteBucketContent

###############################################################################
# ...and...we're done!
###############################################################################

Write-Host "Post-demo cleanup complete"
