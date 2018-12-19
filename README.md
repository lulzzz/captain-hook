# captain-hook

![](docs/images/hook.gif)

Generic message dispatcher for webhooks using the Actor framework in Service Fabric



## Using EDA

1. Create Domain Models which are inherited from DomainEvent
2. Setup Messager client and inject into BigBrother Instance (Use Method)
3. Sent Domain Events to BigBrother on the publish method

### EDA Pre Setup

1. Register Config with Captain Hook
  1. Domain Event Name (full namespace)
  2. Hook URI
  1. Any Auth required for this endpoint
  2. Callback with response
  1. Auth required for the callback endpoint.
1. If hitting an internal service in the hook or callback, both the scope and aud for captain hook (EDA) must be configured/granted on the application side. This involves adding "eda.api.all" scope to the policy around the webhook endpoint. And adding "tooling.eda.api" as an allowed Aud in the IdentityServer setup in Startup.cs.
