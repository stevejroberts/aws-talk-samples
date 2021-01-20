# Restrict Load Balancer to CloudFront

This example contains a solution that lets you demonstrate how a security group (assigned to a load balancer)
can be restricted to only allow traffic from CloudFront IP address ranges. This can be used in scenarios
where a customer wants to ensure traffic only comes to the app from CloudFront to ensure caching rules, or
other mechanisms (WAF rules, lambda@edge) are applied.

## Deploying the demo

1. Ensure you have the AWS Tools for PowerShell cmdlets installed and that your default credentials have been
   configured. The solution will deploy to the default profile and the default region that have been set.
2. Run deploy_app.ps1. This will deploy a CloudFormation template with all the infrastructure required to run
   the demo. As this includes a CloudFront distribution it may take some time. You will know it has finished
   when you see the stack outputs get displayed.
3. Run deploy_lambda.ps1 to deploy the PowerShell-based Lambda function that will update the security groups
   to allow transit from CloudFront IP address ranges.

## Removing the demo

1. Manually delete the Lambda function called "UpdatePublicIPs"
2. Manually delete the CloudFormation stack called "PSLambdaPublicIPDemo"

There should be nothing left behind by this solution after these steps

## Running the demo

1. Go to CloudFormation in the AWS console and retrieve the URL of the application that was deployed. Attempt
   to browse to it, you will notice that there is a timeout as the default configuration of the security groups
   that has been deployed does not allow in ingress on port 80.
2. While still in CloudFormation, note down the two security group IDs that have been created for the app. Note
   to users that this could be done with one SG if you increase the default number of rules per SG above 50 since
   CloudFront has more than 60 IP ranges currently. Splitting it across two means we can do this in a default
   account configuration which is preferable.
3. Go to EC2 in the console and select "Security Groups", show that there are currently no ingress rules on the
   security groups. Talk to the fact that we should allow CloudFront IP ranges to access this, and since public
   IP ranges are subject to change (for example, if we add more CloudFront points of presence) that you will want
   to automate this.
4. Open your code editor of choice, show the code inside LambdaFunction.ps1. Notice the "Update-SecurityGroupIPRange"
   function using the following commands:
   * Get-AWSPublicIpAddressRange to look up the current IP ranges used by cloud front (line 29)
   * Get-EC2SecurityGroup to get the current details of the group (line 38)
   * The comparison of current IP ranges and desired ranges (lines 44-50)
   * The final application of desired changes using Revoke and Grant cmdlets (lines 81-86)

   Note that the logic in this function assumes that current ingress rules will always have the same protocol/port,
   and that an existing rule on the security group will break the logic here.
5. Open Lambda in the AWS console and select the "UpdatePublicIPs" function. Add a
   test event with an empty JSON object as its body (just use empty curly braces {}).

   Run the function - execution time should be around 50-53 seconds.
6. Return the EC2 in the console and back to the two security groups. They should now
   be populated with many ingress rules based on the current CloudFront ranges.
7. Lastly attempt to browse the CloudFront URL again, this time the example
   Elastic Beanstalk web application should correctly show.
8. Browse to Elastic Beanstalk in the console and select the web application. Click
   on the applications URL to open it in a new tab. Note that this request will still
   time out because you are not coming through CloudFront.

## Get help on this demo

Email Brian Farnhill (brfarn@amazon.com) if you have any issues running this demo, or
wish to contribute any code changes.
