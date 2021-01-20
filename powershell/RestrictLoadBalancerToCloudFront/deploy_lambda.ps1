<#
.Synopsis
    Deploys the Lambda function that will be used to update the global and regional
    security groups for our deployed sample application so that only traffic coming
    from AWS public IP ranges can transit to the load balancer. Attempts to access
    the load balancer directly will be rejected.
#>
$ErrorActionPreference = "Stop"

$stackName = "PSLambdaPublicIPDemo"
$stack = Get-CFNStack -StackName $stackName

Publish-AWSPowerShellLambda -Name "UpdatePublicIPs" `
                            -ScriptPath ./LambdaFunction.ps1 `
                            -IAMRoleArn ($stack.Outputs | Where-Object -FilterScript { $_.OutputKey -eq "LambdaIAMRole" }).OutputValue `
                            -Timeout 75 `
                            -EnvironmentVariable @{
                                GlobalSecurityGroupId = ($stack.Outputs | Where-Object -FilterScript { $_.OutputKey -eq "GlobalSGId" }).OutputValue
                                RegionalSecurityGroupId = ($stack.Outputs | Where-Object -FilterScript { $_.OutputKey -eq "RegionalSGId" }).OutputValue
                                PortToAllow = 80
                            }
