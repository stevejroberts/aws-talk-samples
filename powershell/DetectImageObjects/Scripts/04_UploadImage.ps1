<#
.Synopsis
    Uploads one or more image files from a folder to the bucket which will trigger
    our Lambda function to run and analyse what's in the image, applying the labels
    for elements it found as tags on the S3 object(s).

.Description
    Now that our bucket, function and role are configured we are ready to upload some
    images and see the Lambda function get triggered. It will receive the names of the
    objects that were created and for each one that has a supported image-type file
    extension (.jpg etc) it will pass the image to Amazon Rekognition. Rekognition will
    analyze the image and return a set of 'labels' for elements it found in the image.
    Rekognition will be asked to return labels for elements in which it is 70% sure or
    higher are correct (this confidence level can be configured for the Lambda function
    using an environment variable - see the script generated from the blueprint for more
    details).

    On return of the labels our Lambda function will take up to 10 of them (in descending
    confidence% level) and apply the labels as tags on the S3 object.

    The script assumes the bucket was created in the US East (Virginia) region, us-east-1,
    by default. To change this, pass the required region code to the optional Region
    parameter.

.Example
    .\04_UploadImages.ps1 -BucketName "mybucket" -ImageFileFolder C:\Demo\Images

.Example
    .\04_UploadImages.ps1 -BucketName "mybucket" -ImageFileFolder C:\Demo\Images -Region ap-southeast-1
#>

[CmdletBinding()]
Param (
    [Parameter(Mandatory=$true, HelpMessage="The name of the bucket to upload to")]
    [string]$BucketName,

    [Parameter(Mandatory=$true, ValueFromPipeline=$true, HelpMessage="Folder path containing content to upload")]
    [string]$ImageFileFolder,

    # The region in which the bucket exists
    [Parameter()]
    [string]$Region = "us-east-1"
)

###############################################################################
# Import the required AWS module
###############################################################################

Import-Module AWSPowerShell.NetCore

###############################################################################
# Enumerate the contents of the folder (not recursively) and upload each file
# found to the S3 bucket.
###############################################################################

if (Test-Path -Path $ImageFileFolder) {
    $items = Get-ChildItem -Path $ImageFileFolder

    foreach ($i in $items) {
        Write-Host "Uploading $i to bucket $BucketName"
        Write-S3Object -BucketName $BucketName -File $i -Region $Region
    }

    Write-Host "Content uploaded"
} else {
    Write-Error "Folder $ImageFileFolder does not exist!"
}
