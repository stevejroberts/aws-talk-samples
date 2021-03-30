# CurrentWeather Sample Serverless Application

This sample implements a simple API to retrieve the current and predicted weather forecast using the OpenWeather service.

## Prerequisites

To run the sample you will need to create an OpenWeather account (a free account is sufficient) and receive an API key that must be accessible to the Lambda function when it runs. The sample assumes the API key is stored, as an encrypted SecureString, in AWS Systems Manager's Parameter Store. The parameter must be created in the same region as the Lambda function is deployed to.

The name of the parameter can be supplied to the application template on deployment. This name is available to the Lambda function when it runs, and is used to fetch the value of the API key from Parameter Store prior to constructing the URI's to the OpenWeather API endpoints.

If you do not specify a parameter name, */openweather/apikeys/default* is assumed and passed to the Lambda function.

To create the parameter holding your API key, you can use the Systems Manager dashboard in the AWS Management Console, or you can use the command line as follows, with either the AWS Tools for PowerShell or the AWS CLI. Both examples assume you are going to make use of the default parameter name, as mentioned above.

```powershell
# Using the AWS Tools for PowerShell
Write-SSMParameter -Name "/openweather/apikeys/default" -Type SecureString -Value "YOUR-API-KEY-HERE"
```

```bash
# Using the AWS CLI
aws ssm put-parameter --name /openweather/apikeys/default --type SecureString --value "YOUR-API-KEY-HERE"
```

## Using the application

To use the sample application, either deploy it to AWS or run it locally. In both cases, obtain the URL of the application. For example, following a deployment, this will be output at the end of the deployment process.

In a browser, enter the URL and append a query string with the US zipcode of the location you want the current weather to be returned for, as in the example below

```
https://ddwhgclkkd.execute-api.us-west-2.amazonaws.com/?zipcode=98006
```

The sample application currently returns the API output from the OpenWeather service. Future versions will extract relevant data from this output, and return the future forecast, to make the sample more useful than simply using OpenWeather directly!

An example of the current data returned is below.

```json
{"coord":{"lon":-122.1552,"lat":47.5614},"weather":[{"id":800,"main":"Clear","description":"clear sky","icon":"01d"}],"base":"stations","main":{"temp":42.94,"feels_like":37.92,"temp_min":41,"temp_max":45,"pressure":1032,"humidity":61},"visibility":10000,"wind":{"speed":2.17,"deg":222,"gust":4.25},"clouds":{"all":1},"dt":1617124357,"sys":{"type":1,"id":5692,"country":"US","sunrise":1617112224,"sunset":1617158112},"timezone":-25200,"id":0,"name":"Bellevue","cod":200}
```

## Sample application folders

- src - Code for the application's Lambda function.
- events - Invocation events that you can use to invoke the function.
- template.yaml - A template that defines the application's AWS resources.

## Deploying the sample application

The Serverless Application Model Command Line Interface (SAM CLI) is an extension of the AWS CLI that adds functionality for building and testing Lambda applications. It uses Docker to run your functions in an Amazon Linux environment that matches Lambda. It can also emulate your application's build environment and API.

To use the SAM CLI, you need the following tools

* SAM CLI - [Install the SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html)
* .NET Core - [Install .NET Core](https://www.microsoft.com/net/download) - this sample uses .NET Core 3.1
* Docker - [Install Docker community edition](https://hub.docker.com/search/?type=edition&offering=community)

To build and deploy your application for the first time, run the following in your shell:

```bash
sam build
sam deploy --guided
```

The first command will build the source of your application. The second command will package and deploy your application to AWS, with a series of prompts:

* **Stack Name**: The name of the stack to deploy to CloudFormation. This should be unique to your account and region, and a good starting point would be something matching your project name. *Note:* command line examples in this documentation assume you used a stack name of 'CurrentWeather' during deployment.
* **AWS Region**: The AWS region you want to deploy your app to.
* **Confirm changes before deploy**: If set to yes, any change sets will be shown to you before execution for manual review. If set to no, the AWS SAM CLI will automatically deploy application changes.
* **Allow SAM CLI IAM role creation**: Many AWS SAM templates, including this example, create AWS IAM roles required for the AWS Lambda function(s) included to access AWS services. By default, these are scoped down to minimum required permissions. To deploy an AWS CloudFormation stack which creates or modified IAM roles, the `CAPABILITY_IAM` value for `capabilities` must be provided. If permission isn't provided through this prompt, to deploy this example you must explicitly pass `--capabilities CAPABILITY_IAM` to the `sam deploy` command.
* **Save arguments to samconfig.toml**: If set to yes, your choices will be saved to a configuration file inside the project, so that in the future you can just re-run `sam deploy` without parameters to deploy changes to your application.

You can find your API Gateway Endpoint URL in the output values displayed after deployment.

## Use the SAM CLI to build and test locally

Build your application with the `sam build` command.

```bash
sam build
```

The SAM CLI installs dependencies defined in `src/CurrentWeather.csproj`, creates a deployment package, and saves it in the `.aws-sam/build` folder.

Test a single function by invoking it directly with a test event. An event is a JSON document that represents the input that the function receives from the event source. Test events are included in the `events` folder in this project.

Run functions locally and invoke them with the `sam local invoke` command.

```bash
$ sam local invoke CurrentWeatherFunction --event events/event.json
```

The SAM CLI can also emulate your application's API. Use the `sam local start-api` to run the API locally on port 3000.

```bash
sam local start-api
curl http://localhost:3000/
```

The SAM CLI reads the application template to determine the API's routes and the functions that they invoke. The `Events` property on each function's definition includes the route and method for each path.

```yaml
      Events:
        RootResourcePath:
          Type: HttpApi
          Properties:
            Path: "/"
            Method: GET
```

## Fetch, tail, and filter Lambda function logs

To simplify troubleshooting, SAM CLI has a command called `sam logs`. `sam logs` lets you fetch logs generated by your deployed Lambda function from the command line. In addition to printing the logs on the terminal, this command has several nifty features to help you quickly find the bug.

`NOTE`: This command works for all AWS Lambda functions; not just the ones you deploy using SAM.

```bash
$ sam logs -n HelloWorldFunction --stack-name CurrentWeather --tail
```

You can find more information and examples about filtering Lambda function logs in the [SAM CLI Documentation](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-logging.html).

## Cleanup

To delete the sample application that you created, you can use either the AWS Tools for PowerShell or the AWS CLI. Assuming you used your project name for the stack name, you can run the following:

```powershell
# using the AWS Tools for PowerShell
Remove-CFNStack -StackName CurrentWeather
```

```bash
# using the AWS CLI
aws cloudformation delete-stack --stack-name CurrentWeather
```

## Resources

See the [AWS SAM developer guide](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/what-is-sam.html) for an introduction to SAM specification, the SAM CLI, and serverless application concepts.

Next, you can use AWS Serverless Application Repository to deploy ready to use Apps that go beyond hello world samples and learn how authors developed their applications: [AWS Serverless Application Repository main page](https://aws.amazon.com/serverless/serverlessrepo/)
