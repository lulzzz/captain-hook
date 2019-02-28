# captain-hook

[![Build Status](https://eshopworld.visualstudio.com/Github%20build/_apis/build/status/captain-hook?branchName=master)](https://eshopworld.visualstudio.com/Github%20build/_build/latest?definitionId=382?branchName=master)

Generic message dispatcher for webhooks using the Actor framework in Service Fabric

![](docs/images/hook.gif)

## Using EDA

To use the EDA flow, a number of steps need to be setup in the BigBrother Client before you should emit domain events. Additionally, after a domain event is sent from your application this will get routed to a webhook endpoint. The route and destination for this endpoint need to be configured.

1. Create Domain Models which are inherited from DomainEvent Type.
2. Setup Messager client and inject into BigBrother Instance (Using BigBrother.PublishEventsToTopics()). This is used for sending the messages to the ServiceBus Namespace and Topic.
3. Setup Azure Data Explorer (Kusto) to be able to assert on domain events in integration tests (BigBrother.UseKusto())
4. Sent Domain Events to BigBrother via the existing Publish method. (BigBrother.Publish<T>())

### EDA Pre Setup

1. Register Config with Captain Hook
  1. Domain Event Name (full namespace)
  2. Hook URI
  1. Any Auth required for this endpoint
  2. Callback with response
  1. Auth required for the callback endpoint.
  

Per authorisation for internal services, OAuth2 scopes are used to control access to the webhook endpoints. Captain Hook much be allowed to consume this scope. For example, assume a scope of "servicea.webhook.api.all" which has been created by the development team. The scope has been assinged to their controller for authroisation of the endpoints. And the scope has been assigned to Captain Hook in the STS. Further Captain Hook must request this scope when aquiring it's bearer token. The token is then used in all subsequent calls to each internal service.

## Things to note

1. Integrations tests should be asserted on Azure Data Explorer Events rather than flows in which require a synchronous response from report endpoints. Given the async flow, the data will arrive eventually but perhaps not as quickly as an integration test might need.

1. We ensure that message processing are guaranteed. This is important to note that this guarantee exists only after the message has been published to the topic. As such when you "Publish" to the topic this is a synchronous call in BigBrother such that if an exception is thrown while trying to connect to the ServiceBus Topic we will rethrow that exception back up to the caller. Here you will need to handle this, with perhaps, a retry mechanim but at least logging it to Application Insights.

## Future Work
* HA for EDA.
* Domain Event Versioning.
* Tenant Namespacing.
