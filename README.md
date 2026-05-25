# Overview

This gateway allows a merchant to send a payment request to an acquiring bank and get a response back. It has authorization and authentication of the payment API using JWT tokens via the auth endpoint. It also checks idempotency via a key provided in the request header.

Payments are also stored in the gateway along with the process status and can be retrieved via the payments get endpoint.

```mermaid
sequenceDiagram
    Merchant-)PaymentGateway: /login
    PaymentGateway-)Auth: Verify login
    Auth-)PaymentGateway: JWT token (1 hour)
    PaymentGateway-)Merchant: JWT
    Merchant-)PaymentGateway: /api/payments PaymentRequest
    alt NotAuthorized
        PaymentGateway-)Merchant: 401
    end
    alt NotAuthenticated
        PaymentGateway-)Merchant: 403
    end
    alt IdepotancyAlreadyCompleted
        PaymentGateway-)Merchant: 200 Cached response
    end
    alt IdepotancyStillProcessing
        PaymentGateway-)Merchant: 409 Conflict
    end
    alt IdepontancyKeyMissing
        PaymentGateway-)Merchant: 400 Bad request
    end

    PaymentGateway-)Validator: PaymentRequest
    Validator-)PaymentGateway: ValidationResult
    
    alt Passed validation
        PaymentGateway-)AcquiringBank: PaymentRequest
        AcquiringBank-)PaymentGateway: PaymentResponse
    end

    PaymentGateway-)PaymentRepository: Save

    alt DeclinedStatus
        PaymentGateway-)Merchant: 400 Declined status PaymentResponse
    end

    alt RejectedStatus
        PaymentGateway-)Merchant: 400 Rejected status PaymentResponse
    end

    alt RejectedStatus
        PaymentGateway-)Merchant: 400 Failed validation - Rejected status PaymentResponse
    end

    PaymentGateway-)Merchant: Authorized status PaymentResponse
```


# Assumptions

* PCI-sensitive data is not stored
* An acquiring bank simulator is used
* Every request has a new idempotency new if any details were changed/update
* The idempotency key does not expire
* The expiry year is always a 2 digit format
* Http requests is all that is required
* Any repositories do not need to concurrency support and are in memory
* Saving data to repositories never errors
* No observability platform needed to be setup and logging to the console is acceptable
* The gateway does not need to be containerized or scaled
* There is only one acquiring bank

# Architecture

I wanted to keep the organisation simple in its current state so folders were used rather than following any project structures such as clean/onion architecture which I think would be overkill right now. If the project evolved any further then it would be worth doing.

## Logs

Source generated logging was used to help with performance and save configuration setup when unit testing etc. The logging static class can be found in the Loggers folder.

## HttpClient

I created a custom httpclient to encapsulate any logic that the acquiring bank requires while not leaking that implementation to the caller. You can find this in the HttpClients folder.

## Idempotency

I wanted to isolate the idempotency logic into something that was reusable. I had thought about using middleware but I went for using resource filters and attributes as I wanted to apply idempotency to just the endpoints I wanted rather than trying to do any kind of filtering in the middleware.

## Validation

I wanted to capture the validation logic as a way to validate the domain rules set out in the requirements. I used the fluent validation library and created a validator that I could run as soon as a request is received.

## Error handling

The majority of error handling logic is around the acquiring bank. As that is a remote service I wanted to make sure the logic was resilient and could safely handle any errors and log any problems. I added “AddStandardResilienceHandler” to the custom httpclient to add a sensible basic resilience strategy. See https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience?tabs=dotnet-cli#standard-resilience-handler-defaults

## Repositories

For the demo these are just wrapper classes around a list object. They don't handle concurrency and are registered in the DI as a singleton so their values don't get reset with new requests etc. I wanted to make it easy in the future to swap out the in memory repo for a real implementation later so I created a general interface for the repo but also created smaller generic interfaces to explicitly set if the repo could read and write which would make it easier to extend and limit implementations by functionality etc later if needed. 

## Observability

I used the open telemetry library and set it up to broadcast metrics and tracing ids. For logging I used serilog as it provides a nice structure to the logs and makes it easier to search later in centralised loggers. I didn't integrate this with open telemetry as you normally do this via a sink provided by the logger you're using. I have left a comment with how I would set this up.

Metrics were handled by custom middleware to save the implementation details leaking into the controller which would have got messy quickly.

## Authentication/Authorization 

I used the built in ASP.net authentication libraries and set up a JWT scheme that checks the user's login details against its in memory store and if that user has the correct claims to access the endpoint its calling once authenticated.

# API documentation 

POST /api/payments
```
curl --request POST \
  --url https://localhost:7092/api/payments \
  --header 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW5AdGVzdC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkB0ZXN0LmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik1lcmNoYW50IiwiZXhwIjoxNzc5NjkxNDEyLCJpc3MiOiJEZW1vQXBpIiwiYXVkIjoiRGVtb0FwaSJ9.ZV30sl2OJ6QgqX1kQDCKx5kC1jCU_s3igUr7YlLVuws' \
  --header 'Content-Type: application/json' \
  --header 'Idempotency-Key: 1234' \
  --header 'User-Agent: insomnia/12.6.0' \
  --data '{
    "cardNumber": "2222405343248877",
    "expiryMonth": 5,
    "expiryYear": 27,
    "currency": "USD",
    "amount": 1000,
    "cvv": "123"
}'
```

```
{
    "id": "06df7fe6-5317-44d1-a3e7-774e1b89b9a7",
    "status": "Authorized",
    "cardNumberLastFour": "8877",
    "expiryMonth": 5,
    "expiryYear": 27,
    "currency": "USD",
    "amount": 1000
}
```

GET /api/payments/{id}
```
curl --request GET \
  --url https://localhost:7092/api/payments/06df7fe6-5317-44d1-a3e7-774e1b89b9a7 \
  --header 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW5AdGVzdC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkB0ZXN0LmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik1lcmNoYW50IiwiZXhwIjoxNzc5NjkxNDEyLCJpc3MiOiJEZW1vQXBpIiwiYXVkIjoiRGVtb0FwaSJ9.ZV30sl2OJ6QgqX1kQDCKx5kC1jCU_s3igUr7YlLVuws' \
  --header 'User-Agent: insomnia/12.6.0'
```
```
{
    "id": "06df7fe6-5317-44d1-a3e7-774e1b89b9a7",
    "status": "Authorized",
    "cardNumberLastFour": "8877",
    "expiryMonth": 5,
    "expiryYear": 27,
    "currency": "USD",
    "amount": 1000
}
```

POST /api/auth/login

```
curl --request POST \
  --url https://localhost:7092/api/auth/login \
  --header 'Content-Type: application/json' \
  --header 'User-Agent: insomnia/12.6.0' \
  --data '{
    "email": "admin@test.com",
    "password": "password123"
}'
```
```
{
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW5AdGVzdC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZG1pbkB0ZXN0LmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6Ik1lcmNoYW50IiwiZXhwIjoxNzc5NjkxNDEyLCJpc3MiOiJEZW1vQXBpIiwiYXVkIjoiRGVtb0FwaSJ9.ZV30sl2OJ6QgqX1kQDCKx5kC1jCU_s3igUr7YlLVuws",
    "role": "Merchant"
}
```

# Trade-offs and future improvements 

Idempotency requests and responses are currently stored in memory for simplicity. In a production environment, this would be replaced with a distributed cache such as Redis to ensure idempotency survives application restarts and works correctly across multiple gateway instances.

Communication with downstream services currently uses synchronous HTTP requests. In a distributed architecture, this could be evolved to use asynchronous event-driven messaging via a message broker to improve scalability, resiliency, and decoupling between services.

Observability could be improved by introducing a dedicated monitoring platform with structured logging, distributed tracing, and metrics dashboards to provide better operational insight into payment flows and failures.

Repository implementations currently use in-memory storage to keep the demo lightweight. In a production system, these would be backed by a persistent datastore. Additional considerations such as concurrency control, transactional consistency, and distributed workflow management would also need to be addressed. For more complex distributed payment workflows, patterns such as Saga orchestration could be introduced to manage compensating actions and rollback behaviour.

