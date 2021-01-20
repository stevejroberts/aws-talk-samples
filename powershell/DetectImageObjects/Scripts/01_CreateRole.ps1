<#
.Synopsis
    Creates a role with necessary policies for the DetectLabels demo. If the role
    exists, no work is performed.

.Description
    Lambda functions run under the scope of an Identity and Acces Management (IAM)
    Role.

    The role provides temporary AWS credentials to the code in your function to
    enable it to call other AWS services and also controls what APIs and AWS
    resources your function has access to.

    The role will be created by calling IAM APIs in the US East (Virginia) region,
    us-east-1, by default. To change this, pass the required region code to the
    optional Region parameter.

.Example
    .\01_CreateRole.ps1 -RoleName "MyFunctionRole"
.Example
    .\01_CreateRole.ps1 -RoleName "MyFunctionRole" -Region ap-southeast-1
#>
[CmdletBinding()]
Param (
    # The name of the role to create
    [Parameter(Mandatory=$true)]
    [string]$RoleName,

    # The region in which to make the calls to IAM. Calling IAM in us-east-1
    # creates resources that can be used in all public AWS regions, eg us-west-2 etc.
    [Parameter()]
    [string]$Region = "us-east-1"
)

###############################################################################
# Install and import the required AWS module, if required
###############################################################################

Write-Host "Checking required modules are installed"

$awsPsToolsModuleName = "AWSPowerShell.NetCore"
$awsPsToolsModule = Get-Module -ListAvailable -Name $awsPsToolsModuleName
if (!($awsPsToolsModule)) {
    Install-Module $awsPsToolsModuleName -Force -Verbose -AllowClobber
}

Import-Module $awsPsToolsModuleName

###############################################################################
# Create a new role for the function; this scopes what APIs and resources the
# function can access
###############################################################################

try {
    Write-Host "Checking for existing role $RoleName"

    # IAM throws an exception which gets surfaced if role does not exist, the
    # try/catch suppresses the exception output when it does not
    $role = Get-IAMRole -RoleName $RoleName -Region $Region
}
catch {
}

if (!($role)) {

    Write-Host "Role does not exist, creating and attaching policies"

    # the first policy to attach allows the Lambda function, when running under the
    # scope of the role, to obtain temporary AWS credentials on our behalf
    $assumeRolePolicy = @"
{
    "Version": "2012-10-17",
    "Statement": {
        "Effect": "Allow",
        "Principal": {
            "Service": "lambda.amazonaws.com"
        },
        "Action": "sts:AssumeRole"
    }
}
"@
    $role = New-IAMRole -RoleName $RoleName -Description "Demo role" -AssumeRolePolicyDocument $assumeRolePolicy -Region $Region

    # The ARNs (Amazon Resource Name) of AWS-provided 'managed policies' that we will
    # use to scope what APIs our function has access to
    # This policy permits access to Rekognition's read-only APIs
    Register-IAMRolePolicy -RoleName $role.RoleName -PolicyArn "arn:aws:iam::aws:policy/AmazonRekognitionReadOnlyAccess" -Region $Region
    # This policy provides Lambda with Put and Get access to S3 and full access to CloudWatch Logs
    Register-IAMRolePolicy -RoleName $role.RoleName -PolicyArn "arn:aws:iam::aws:policy/AWSLambdaExecute" -Region $Region

    # Add an inline policy permitting the Lambda to call the PutObjectTagging API on S3. This
    # API is not included in the general Lambda Execute managed policy we attached above
    $putObjectTaggingPolicy = @"
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": "s3:PutObjectTagging",
            "Resource": "*"
        }
    ]
}
"@
    Write-IAMRolePolicy -RoleName $RoleName -PolicyName "PutObjectTagging" -PolicyDocument $putObjectTaggingPolicy -Region $Region

    Write-Host "IAM role $RoleName created and configured for the function"

    # refresh the role object so it includes the latest data for output
    $role = Get-IAMRole -RoleName $RoleName -Region $Region
}
else {
    Write-Warning "Role $RoleName already exists, no changes made"
}

Write-Output $role
