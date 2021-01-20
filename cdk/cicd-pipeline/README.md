# CI-CD Deployment Pipeline Sample

This sample shows how to use CDK Pipelines to stand up resources for both a CI/CD pipeline and an auto scaled fleet of Amazon Linux 2 images, behind an application load balancer, to receive the deployment.

To deploy the sample, run the command

```bash
cdk deploy --profile YOUR-CREDENTIAL-PROFILE -c repo-owner=YOUR-GITHUB-ID -c repo-name=YOUR-REPO-NAME -c repo-branch=YOUR-REPO-BRANCH
```

The deployment will occur to your configured default region.

The sample uses a static web page as the app, in the SimplePage folder.

Once deployed, you can access the deployed app (a static web page) using the link output as 'AppUrl'. To trigger the pipeline, update the sample page and push to GitHub. You'll see the pipeline trigger and the update be deployed to the fleet.

To delete the sample resources, run the command

```bash
cdk destroy --profile YOUR-CREDENTIAL-PROFILE -c repo-owner=YOUR-GITHUB-ID -c repo-name=YOUR-REPO-NAME -c repo-branch=YOUR-REPO-BRANCH
```

For more details on CDK Pipelines, see this [blog post](https://aws.amazon.com/blogs/developer/cdk-pipelines-continuous-delivery-for-aws-cdk-applications/).
