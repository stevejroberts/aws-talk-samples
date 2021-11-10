# Fargate Containers Sample

This sample deploys a .NET 6 Blazor Server application (generated from Visual Studio with no changes) to a load-balanced cluster on Fargate in your account's default VPC for a region, using the high-level ECS Patterns CDK library. 

__Note__: The stack is specialized using environment variables, set by the CLI. When running synth, or deploy, commands be sure to specify the --profile option, for example

```powershell
cdk synth --profile credential-profile-name-here
cdk deploy --profile credential-profile-name-here
```

Alternatively, modify Program.cs to set your account number and/or desired region, as shown in the code comments (you can also mix the two approaches, for example using envvar for account, and fixed region).
