# S3 Event Trigger Sample

This sample shows how easy it is to wire up resources to each other using events. In this example, an S3 bucket is configured to trigger an message to be posted to an SQS queue whenever an object is created (or updated) in the bucket.

In declarative templates, this is somewhat more involved. You'll see how a auto-generated Lambda function is used by the CDK to handle the wire-up on your behalf.
