<#
.Description
    Lambda function to update security group(s) to permit transit from public
    AWS IP ranges. The ranges are determined from data published by AWS and
    retrievable using the Get-AWSPublicIpAddressRange cmdlet in the AWS Tools
    for PowerShell.

    This Lambda updates the two security groups for global and regional IPs
    used in our sample application.
#>

#Requires -Modules @{ModuleName='AWSPowerShell.NetCore';ModuleVersion='3.3.390.0'}

Import-Module -Name AWSPowerShell.NetCore

function Update-SecurityGroupIPRange
{
    param(
        [Parameter(Mandatory=$true)]
        [String]
        $SecurityGroupId,

        [Parameter(Mandatory=$true)]
        [String]
        [ValidateSet("GLOBAL", "REGIONAL")]
        $Filter
    )

    $cloudFrontIPs = Get-AWSPublicIpAddressRange -ServiceKey CLOUDFRONT

    $desiredIPs = @()
    if ($Filter -eq "GLOBAL") {
        $desiredIPs = ($cloudFrontIPs | Where-Object -FilterScript { $_.Region -eq "GLOBAL" }).IpPrefix
    } else {
        $desiredIPs = ($cloudFrontIPs | Where-Object -FilterScript { $_.Region -ne "GLOBAL" }).IpPrefix
    }

    $securityGroup = Get-EC2SecurityGroup -GroupId $SecurityGroupId
    $currentIps = $securityGroup.IpPermissions.Ipv4Ranges.CidrIp
    $currentIps += $securityGroup.IpPermissions.Ipv6Ranges.CidrIpv6

    # If there are no ingress rules on the security group for comparing, just build an array
    # with instructions to add everything
    $changesToMake = @()
    if ($null -eq $currentIps) {
        $changesToMake = @()
        $desiredIPs | ForEach-Object -Process { $changesToMake += @{ InputObject = $_; SideIndicator = "=>" } }
    } else {
        $changesToMake = Compare-Object -ReferenceObject $currentIps -DifferenceObject $desiredIPs
    }

    $ipsToAdd = @()
    $ipsToRemove = @()

    $changesToMake | ForEach-Object -Process {
        $currentChange = $_

        $newIp = New-Object Amazon.EC2.Model.IpPermission
        $newIp.IpProtocol = "tcp"
        $newIp.FromPort = $env:PortToAllow
        $newIp.ToPort = $env:PortToAllow

        if ($currentChange.InputObject -like "*:*") {
            $ipRange = New-Object -TypeName Amazon.EC2.Model.Ipv6Range
            $ipRange.CidrIpv6 = $currentChange.InputObject
            $newIp.Ipv6Ranges.Add($ipRange)
        } else {
            $ipRange = New-Object -TypeName Amazon.EC2.Model.IpRange
            $ipRange.CidrIp = $currentChange.InputObject
            $newIp.Ipv4Ranges.Add($ipRange)
        }

        if ($currentChange.SideIndicator -eq "=>") {
            $ipsToAdd += $newIp
        }
        if ($currentChange.SideIndicator -eq "<=") {
            $ipsToRemove += $newIp
        }
    }

    if ($ipsToRemove.length -gt 0) {
        Revoke-EC2SecurityGroupIngress -GroupId $SecurityGroupId -IpPermission $ipsToRemove
    }
    if ($ipsToAdd.length -gt 0) {
        Grant-EC2SecurityGroupIngress -GroupId $SecurityGroupId -IpPermission $ipsToAdd
    }

}

# Use two security groups to keep the rule count below 50 on each security group
Update-SecurityGroupIPRange -SecurityGroupId $env:GlobalSecurityGroupId -Filter GLOBAL
Update-SecurityGroupIPRange -SecurityGroupId $env:RegionalSecurityGroupId -Filter REGIONAL
