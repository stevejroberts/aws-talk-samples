<#
.Synopsis
    Creates a new Lambda function using the DetectLabels blueprint and
    deploys it to AWS.

.Description
    The create uses the New-AWSPowerShellLambda cmdlet in the AWS Lambda
    Tools for Powershell module (AWSlambdaPSCore) to create a new script
    from the 'DetectLabels' sample blueprint. The script is then deployed
    (unchanged) to Lambda using the Publish-AWSPowerShellLambda cmdlet
    from the same module.

    The new Lambda function will run under the scope of the role created
    using the 01_CreateRole.ps1 script. The name of that role must be
    supplied to the RoleName parameter.

    The deployment will be created in the US East (Virginia) region, us-east-1,
    by default. To change this, pass the required region code to the optional
    Region parameter.

.Example
    .\02_DeployLambdaFunction -FunctionName "MyFunction" -RoleName "MyFunctionRole"
.Example
    .\02_DeployLambdaFunction -FunctionName "MyFunction" -RoleName "MyFunctionRole" -Region ap-southeast-1
#>

[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true)]
    [string]$FunctionName,

    [Parameter(Mandatory=$true)]
    [string]$RoleName,

    # The region to deploy the Lambda function to. Note that the next script,
    # 03_ConfigureBucketAndTrigger, creates a bucket - the bucket and Lambda
    # function must exist in the same AWS region.
    [Parameter()]
    [string]$Region = "us-east-1"
)

###############################################################################
# Install the Lambda PowerShell tools module if required, and import it
# and the AWS Tools for PowerShell module ready for use
###############################################################################

$awsPsLambdaToolsModuleName = "AWSLambdaPSCore"
$awsPsToolsModuleName = "AWSPowerShell.NetCore"

$awsPsLambdaToolsModule = Get-Module -ListAvailable -Name $awsPsLambdaToolsModuleName
if (!($awsPsLambdaToolsModule)) {
    Install-Module $awsPsLambdaToolsModuleName -Force -Verbose -AllowClobber
}

Import-Module $awsPsLambdaToolsModuleName
Import-Module $awsPsToolsModuleName

###############################################################################
# Create the sample Lambda function from the blueprint
###############################################################################

# We place the generated script into a subfolder at the current location
$cwd = (Get-Location).Path
$srcFolder = Join-Path $cwd "DetectLabelsDemo"
if (Test-Path $srcFolder) {
    Write-Host "Cleaning demo source path $srcFolder"
    Remove-Item $srcFolder -Recurse -Force
}

# NOTE: To see what blueprints are available, you can use the
# Get-AWSPowerShellLambdaTemplate cmdlet
New-AWSPowerShellLambda -Template "DetectLabels" -Directory $srcFolder

###############################################################################
# Retrieve the ARN of the role the function will be scoped by; we need this
# during deployment
###############################################################################

$role = Get-IAMRole -RoleName $roleName -Region $Region

###############################################################################
# Deploy the function!
###############################################################################

# Form up the path to the generated script file we are going to deploy
$scriptPath = Join-Path $srcFolder "DetectLabels.ps1"

Publish-AWSPowerShellLambda -Name $FunctionName -ScriptPath $scriptPath -Region $Region -IAMRoleArn $role.Arn

Write-Host "Lambda function deployed"
