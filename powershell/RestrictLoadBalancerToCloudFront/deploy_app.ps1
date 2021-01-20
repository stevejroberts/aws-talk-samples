<#
.Synopsis
    Deploys a sample Elastic Beanstalk application into a Load Balanced environment
    fronted by a CDN
#>
$ErrorActionPreference = "Stop"

$stackName = "PSLambdaPublicIPDemo"
$stacktest = $null
try { $stacktest = Get-CFNStack -StackName $stackName } catch {}

if ($null -eq $stacktest) {
    New-CFNStack -StackName $stackName `
                 -TemplateBody (Get-Content ./infrastructure.yaml -Raw) `
                 -Capability CAPABILITY_IAM
} else {
    try {
        Update-CFNStack -StackName $stackName `
                        -TemplateBody (Get-Content ./infrastructure.yaml -Raw) `
                        -Capability CAPABILITY_IAM
    } catch {
        if ($_.Exception.Message -notlike "*No updates are to be performed.*") {
            throw $_
        }
    }
}
Wait-CFNStack -StackName $stackName -Timeout 3600
$stack = Get-CFNStack -StackName $stackName

$stack.Outputs
