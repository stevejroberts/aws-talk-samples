#Requires -Modules @{ModuleName='AWSPowerShell.NetCore';ModuleVersion='3.3.390.0'}

<#
    .SYNOPSIS
    Determines the type of log file we have been triggered to process.
#>
function Test-EventLogType {

    param (
        $LambdaInput,
        $LambdaContext
    )

    $ext = Split-Path -Extension -Path $LambdaInput.key
    if ($ext) {
        $ext = $ext.ToLower()
    }

    Write-Host "Evaluating transformer for log file with extension $ext"
    $logType = ''
    switch ($ext) {
        '.xml'
        {
            $logType = 'xml'
        }
        '.json'
        {
            $logType = 'json'
        }
        '.csv'
        {
            $logType = 'csv'
        }
        '.txt'
        {
            $logType = 'csv'
        }
        default
        {
            Write-Host 'Unknown log file type extension'
        }
    }

    $LambdaInput | Add-Member -MemberType 'NoteProperty' -Name 'LogType' -Value $logType -Force
    $LambdaInput
}

<#
    .SYNOPSIS
    Transforms a log file containing data in unheadered csv format.
#>
function Invoke-CsvLogTransform {

    param (
        $LambdaInput,
        $LambdaContext
    )

    $tempFile = [System.IO.Path]::GetTempFileName()
    Write-Host "Downloading log content from $LambdaInput.key to $tempFile"
    Read-S3Object -BucketName $LambdaInput.bucketName -Key $LambdaInput.key -File $tempFile

    $logStreamName = $LambdaInput.key + "-" + (Get-Date -Format FileDateTimeUniversal)
    Write-Host "Creating new log stream $logStreamName in log group $env:LOG_GROUP_NAME"
    New-CWLLogStream -LogGroupName $env:LOG_GROUP_NAME -LogStreamName $logStreamName -Force

    # read and parse the csv log content into the output stream
    $logContent = Import-Csv -Path $tempFile -Encoding utf8

    # using an arraylist for better perf (doesn't realloc on every addition) giving lower
    # runtime cost, helpful if we encounter a large logfile
    $events = [System.Collections.ArrayList]::new()
    foreach ($logEntry in $logContent) {
        Write-Host "Creating log event in log stream $logStreamName"

        $events.Add([Amazon.CloudWatchLogs.Model.InputLogEvent]@{
            Message = "EventTime=$($logEntry.EventTime), Status=$($logEntry.Status), Message=$($logEntry.Message)"
            Timestamp = [DateTime]::UtcNow
        })
    }

    Write-CWLLogEvent -LogGroupName $env:LOG_GROUP_NAME -LogStreamName $logStreamName -LogEvent $events

    $LambdaInput
}

<#
    .SYNOPSIS
    Transforms a log file in xml format.
#>
function Invoke-XmlLogTransform {

    param (
        $LambdaInput,
        $LambdaContext
    )

    $tempFile = [System.IO.Path]::GetTempFileName()
    Write-Host "Downloading log content from $LambdaInput.key to $tempFile"
    Read-S3Object -BucketName $LambdaInput.bucketName -Key $LambdaInput.key -File $tempFile

    $logStreamName = $LambdaInput.key + "-" + (Get-Date -Format FileDateTimeUniversal)
    Write-Host "Creating new log stream $logStreamName in log group $env:LOG_GROUP_NAME"
    New-CWLLogStream -LogGroupName $env:LOG_GROUP_NAME -LogStreamName $logStreamName -Force

    # read and parse the csv log content into the output stream
    [xml]$logContent = Get-Content -Path $tempFile -Raw -Encoding utf8

    # using an arraylist for better perf (doesn't realloc on every addition) giving lower
    # runtime cost, helpful if we encounter a large logfile
    $events = [System.Collections.ArrayList]::new()
    foreach ($logEntry in $logContent.LogData.LogEntry) {
        Write-Host "Creating log event in log stream $logStreamName"

        $eventTime = $logEntry.EventTime
        $status = $logEntry.Status
        $msg = $logEntry.Message

        $events.Add([Amazon.CloudWatchLogs.Model.InputLogEvent]@{
            Message = "EventTime=$eventTime, Status=$status, Message=$msg"
            Timestamp = [DateTime]::UtcNow
        })
    }

    Write-CWLLogEvent -LogGroupName $env:LOG_GROUP_NAME -LogStreamName $logStreamName -LogEvent $events

    $LambdaInput
}

<#
    .SYNOPSIS
    Transforms a log file in json format.
#>
function Invoke-JsonLogTransform {

    param (
        $LambdaInput,
        $LambdaContext
    )

    $tempFile = [System.IO.Path]::GetTempFileName()
    Write-Host "Downloading log content from $LambdaInput.key to $tempFile"
    Read-S3Object -BucketName $LambdaInput.bucketName -Key $LambdaInput.key -File $tempFile

    $logStreamName = $LambdaInput.key + "-" + (Get-Date -Format FileDateTimeUniversal)
    Write-Host "Creating new log stream $logStreamName in log group $env:LOG_GROUP_NAME"
    New-CWLLogStream -LogGroupName $env:LOG_GROUP_NAME -LogStreamName $logStreamName -Force

    # read and parse the csv log content into the output stream
    $jsonText = Get-Content -Path $tempFile -Raw -Encoding utf8
    $json = ConvertFrom-Json -InputObject $jsonText

    # using an arraylist for better perf (doesn't realloc on every addition) giving lower
    # runtime cost, helpful if we encounter a large logfile
    $events = [System.Collections.ArrayList]::new()
    foreach ($logEntry in $json.LogEntries) {
        Write-Host "Creating log event in log stream $logStreamName"

        $events.Add([Amazon.CloudWatchLogs.Model.InputLogEvent]@{
            Message = "EventTime=$($logEntry.LoggedAt), Status=$($logEntry.Status), Message=$($logEntry.Message)"
            Timestamp = [DateTime]::UtcNow
        })
    }

    Write-CWLLogEvent -LogGroupName $env:LOG_GROUP_NAME -LogStreamName $logStreamName -LogEvent $events

    $LambdaInput
}
