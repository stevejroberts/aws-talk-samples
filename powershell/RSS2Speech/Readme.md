# RSS2Speech Demo

This demo illustrates how [AWS Lambda](https://aws.amazon.com/lambda) functions can be fronted by a web API, hosted in Amazon API Gateway. The Lambda function takes the url of a RSS feed and an optional count of the number of posts to convert, and uses [Amazon Polly](https://aws.amazon.com/polly) to convert the text of the posts to audio files. The audio files are output to an [Amazon S3](https://aws.amazon.com/s3) bucket and a notification that the file is ready sent to an [Amazon Simple Notification Service (SNS)](https://aws.amazon.com/sns) topic to which an email has been subscribed. The notification includes a presigned url to the S3 object representing the audio file.

The Lambda function is contained in the [RSS2Speech.ps1](./RSS2Speech.ps1) script file. The inputs to the function are the feed url, and optional post count and voice, conveyed in the query parameters supplied to the http endpoint exposing the API. For example 'http://api-url/?feed=feedUrl&maxPosts=2&voiceId=Nicole'.

The function invokes Amazon Polly's SynthesizeSpeech API using the [Get-POLSpeech](https://docs.aws.amazon.com/powershell/latest/reference/index.html?page=Get-POLSpeech.html&tocid=Get-POLSpeech) cmdlet. The output object from this call contains a stream holding the audio. This is first saved to a local temporary file, then uploaded to the Amazon S3 bucket that was created by the [serverless.template](./serverless.template) file.

Once the audio file has been uploaded to the bucket the function creates a presigned url to the object and includes it in a notification to the SNS topic. The topic was also created by the serverless template, and the user's email address automatically subscribed to the topic. The user's email address is supplied as the parameter *UserEmail* to the template during deployment.

The S3 bucket and SNS topic are created as resources in the serverless.template file and the name and SNS topic ARN posted to [AWS Systems Manager's](https://aws.amazon.com/systems-manager/) Parameter Store. The function recovers the details of the resources are recovered from parameter store at runtime using cmdlets from the AWS Tools for PowerShell. The [Get-SSMParameterValue](https://docs.aws.amazon.com/powershell/latest/reference/index.html?page=Get-SSMParameterValue.html&tocid=Get-SSMParameterValue) cmdlet is used to recover the values of the two parameters.

To build and deploy the demo, run the [build_and_deploy.ps1](./build_and_deploy.ps1) script. This script takes one mandatory parameter, the user email to be subscribed to the SNS topic. After deployment, this email will contain a new inbox message from SNS asking for confirmation for the subscription. Once confirmed, and the Lambda function is run, emails with links to the converted audio will arrive in the inbox.

To invoke the Lambda function, first navigate to the API in the API Gateway console and run a test (or craft the url using a browser) using the query parameters shown earlier. An example payload that is sent from API Gateway to the Lambda function can be found in the [apipayload_example.json](./apipayload_example.json) file.

Example query parameters to append to the API: *feedUrl=http://feeds.feedburner.com/AmazonWebServicesBlog&maxPosts=1&voiceId=Kendra*

See [this document](https://docs.aws.amazon.com/polly/latest/dg/voicelist.html) for other voices that may be used with Amazon Polly.
