# FraudAssessment Service

A .NET 8 service responsible for receiving fraud-assessment requests,
validating them, preventing duplicate submissions, tracking assessment
lifecycle, and dispatching assessment-processing messages through
RabbitMQ using the Outbox Pattern.

> **Current implementation status:** The service can create and persist
> assessments, publish processing messages reliably through an Outbox,
> consume those messages, atomically claim an assessment for processing,
> and identify the downstream service based on the assessment type. The
> actual HTTP calls to Payment Fraud / ATO and the final Risk Decision
> integration are not implemented yet.

------------------------------------------------------------------------

## 1. Project Purpose

`FraudAssessment` is the orchestration entry point of the
fraud-detection platform.

It does **not** perform fraud analysis and does **not** make the final
risk decision.

Its responsibilities are to:

-   Receive an assessment request from a client.
-   Validate the request and its idempotency key.
-   Create and persist an assessment.
-   Track the assessment's processing lifecycle.
-   Reliably publish a message requesting assessment processing.
-   Consume the message and claim the assessment so it is not processed
    concurrently more than once.
-   Determine which downstream service is required based on the
    assessment type.

The specialized services own their respective analysis:

  -----------------------------------------------------------------------
  Service                             Responsibility
  ----------------------------------- -----------------------------------
  FraudAssessment                     Orchestrates the assessment
                                      workflow and tracks its lifecycle

  Payment Fraud                       Analyzes payment transactions and
                                      returns fraud signals/results

  ATO Detection                       Analyzes account-takeover signals

  Risk Decision                       Combines analysis results and
                                      produces the final decision
  -----------------------------------------------------------------------

The final risk outcomes are separate from the assessment lifecycle
status:

-   `ALLOW`
-   `REVIEW`
-   `STEP_UP`
-   `BLOCK`

------------------------------------------------------------------------

## 2. Current Architecture

``` text
Client
  |
  | HTTP request
  v
FraudAssessment API
  |
  | Validate request + idempotency
  v
MongoDB Transaction
  |-- Assessment
  |-- IdempotencyRecord
  |-- OutboxMessage
  |
  v
OutboxBackgroundService
  |
  v
OutboxPublisher
  |
  v
RabbitMQ Exchange: fraud-assessment
  |
  | Routing key: ProcessAssessment
  v
Queue: assessment-processing
  |
  v
AssessmentProcessingWorker
  |
  | Load Assessment
  | Validate status and type
  | Atomically claim Received -> Processing
  | Identify target service from Assessment.Type
  v
Payment Fraud Service OR ATO Detection Service
     (routing is currently identified/logged only;
      downstream HTTP integration is not implemented)
```

### Important architectural boundaries

-   `FraudAssessment` stores assessment metadata and lifecycle
    information.
-   It is not intended to store raw payment or login payloads.
-   Payment/login-specific data must be supplied to the relevant service
    through an agreed secure request contract or secure payload
    reference.
-   Do not put card PAN, CVV, credentials, or other highly sensitive raw
    data into RabbitMQ messages or Outbox payloads.
-   RabbitMQ transports work; it does not itself increase the processing
    capacity of downstream services.

------------------------------------------------------------------------

## 3. Technology Stack

-   .NET 8
-   ASP.NET Core Web API
-   Clean Architecture
-   CQRS with MediatR
-   MongoDB
-   RabbitMQ
-   RabbitMQ.Client 7.x
-   Outbox Pattern
-   Dependency Injection
-   Hosted Background Services
-   `System.Text.Json`
-   xUnit integration tests

Other application libraries used in the service include
FluentValidation, AutoMapper, and the project's response-handling
components where applicable.

------------------------------------------------------------------------

## 4. Solution Structure

The solution is organized into layers/projects:

``` text
FraudAssessment/
├── FraudAssessment.API/
│   └── Controllers, API configuration, middleware, startup
│
├── FraudAssessment.Application/
│   ├── Features/
│   │   └── Assessments/
│   │       ├── Commands/
│   │       ├── Queries/
│   │       └── DTOs/
│   ├── Exceptions/
│   ├── Interface/
│   └── Models/
│
├── FraudAssessment.Domain/
│   ├── Entities/
│   ├── Enums/
│   └── DomainEvents/
│
├── FraudAssessment.Infrastructure/
│   ├── Messaging/
│   │   └── RabbitMq/
│   ├── Persistence/
│   │   └── MongoDB/
│   └── DependencyInjection/
│
└── FraudAssessment.IntegrationTests/
    └── Persistence/
```

Exact folders may evolve as the implementation grows.

### Layer responsibilities

**Domain** - Contains core entities, enums, and domain behavior. - Does
not depend on MongoDB, RabbitMQ, or ASP.NET Core.

**Application** - Contains use cases, commands, queries, DTOs, and
abstractions. - Defines interfaces required by the application.

**Infrastructure** - Implements MongoDB persistence and RabbitMQ
messaging. - Contains the Outbox publisher and assessment-processing
worker. - Implements infrastructure-specific repositories.

**API** - Exposes HTTP endpoints. - Configures middleware and dependency
injection. - Initializes required infrastructure components during
startup.

------------------------------------------------------------------------

## 5. Assessment Lifecycle

The assessment lifecycle is represented separately from the fraud
decision.

Current lifecycle statuses:

  -----------------------------------------------------------------------
  Status                              Meaning
  ----------------------------------- -----------------------------------
  `Received`                          The request was accepted and the
                                      assessment was created

  `Processing`                        A worker successfully claimed the
                                      assessment and processing has
                                      started

  `Completed`                         Processing completed and a final
                                      decision was associated with the
                                      assessment

  `Failed`                            Processing failed
  -----------------------------------------------------------------------

The `Assessment` entity contains fields including:

-   `Id`
-   `ClientId`
-   `AccountId`
-   `Type`
-   `Status`
-   `ProcessingStartedAt`
-   `DecisionId`
-   `CreatedAt`
-   `CompletedAt`

The entity controls lifecycle transitions through methods such as:

-   `Create(...)`
-   `StartProcessing()`
-   `Complete(decisionId)`
-   `Fail(...)`

The assessment status `Completed` does not mean `ALLOW`. The final
outcome is a separate risk-decision value.

------------------------------------------------------------------------

## 6. Assessment Types

The worker currently distinguishes assessment types using
`Assessment.Type`.

  Assessment type     Downstream service
  ------------------- -----------------------
  `Payment`           Payment Fraud Service
  `AccountTakeover`   ATO Detection Service

The type is assigned when the assessment is created by the relevant API
use case. The worker reads the persisted assessment type and uses it to
identify the target service.

At the current stage, the worker logs the selected service. It does not
yet send an HTTP request to that service.

------------------------------------------------------------------------

## 7. API Endpoints

The implemented API routes include:

  ----------------------------------------------------------------------------------
  Method                  Route                              Purpose
  ----------------------- ---------------------------------- -----------------------
  `POST`                  `/api/v1/Assessments`              Create a general
                                                             assessment

  `POST`                  `/api/v1/Assessments/Payment`      Create a payment
                                                             assessment

  `GET`                   `/api/v1/Assessments/{clientId}`   Retrieve assessments
                                                             for a client

  `GET`                   `/api/v1/Assessments/Id/{id}`      Retrieve an assessment
                                                             by ID
  ----------------------------------------------------------------------------------

The exact request and response contracts are defined by the
corresponding API request models and DTOs.

### Idempotency

Creation requests use an idempotency key supplied by the client,
typically through the request contract/header handling implemented by
the API.

The service computes a request hash internally. The hash represents the
semantically relevant request data, rather than trusting a
client-provided hash.

For payment requests, the hash includes the payment amount and currency.
Currency normalization uses trimming and uppercase conversion; decimal
formatting uses invariant culture.

The `IdempotencyRecord` contains:

-   `ClientId`
-   `Key`
-   `RequestHash`
-   `AssessmentId`
-   `CreatedAt`
-   `ExpiresAt`

A unique MongoDB index on `(ClientId, Key)` prevents duplicate
idempotency records for the same client/key pair.

If two concurrent requests race to create the same idempotency record,
duplicate-key handling should recover the existing assessment rather
than return an unhandled server error.

------------------------------------------------------------------------

## 8. MongoDB Persistence

MongoDB is used to persist assessment data and messaging records.

Collections used by the service:

-   `Assessments`
-   `IdempotencyRecords`
-   `Outbox`

### Atomic creation

The service saves the following records in one MongoDB transaction:

1.  Assessment
2.  Idempotency record
3.  Outbox message

This prevents a situation where the assessment is committed but its
processing message is never recorded for publication.

### MongoDB transactions and replica set

MongoDB multi-document transactions require a replica set. The local
development setup uses a replica set named `rs0`.

Example local connection string:

``` text
mongodb://127.0.0.1:27017/?replicaSet=rs0
```

Configuration keys:

``` yaml
MongoDB:
  ConnectionString: "mongodb://127.0.0.1:27017/?replicaSet=rs0"
  DatabaseName: "FraudAssessment"
```

Use environment variables or secure configuration for non-local
credentials. Do not commit production secrets.

### Indexes

The indexes initialized for the current implementation are:

  ----------------------------------------------------------------------------------
  Collection              Index                              Purpose
  ----------------------- ---------------------------------- -----------------------
  Assessments             `(ClientId ASC, CreatedAt DESC)`   Efficiently retrieve a
                                                             client's assessments

  IdempotencyRecords      Unique `(ClientId ASC, Key ASC)`   Enforce idempotency

  Outbox                  `(Status ASC, OccurredAt ASC)`     Find pending messages
                                                             efficiently
  ----------------------------------------------------------------------------------

------------------------------------------------------------------------

## 9. Outbox Pattern

The Outbox Pattern is used to reliably publish assessment-processing
messages.

### Why Outbox?

Publishing directly to RabbitMQ after saving an assessment can create a
consistency problem:

-   The database save may succeed.
-   RabbitMQ publishing may fail.
-   The assessment then exists without a corresponding processing
    message.

With the Outbox Pattern, the assessment and Outbox message are saved in
the same MongoDB transaction. A background publisher later sends pending
messages to RabbitMQ.

### OutboxMessage

The Outbox entity contains:

-   `Id`
-   `AssessmentId`
-   `EventType`
-   `Payload`
-   `OccurredAt`
-   `PublishedAt`
-   `RetryCount`
-   `Status`
-   `NextRetryAt`

The Outbox status starts as `Pending`. After successful publishing, the
message is marked as published.

### Publishing flow

``` text
Create Assessment
      |
      v
Save Assessment + IdempotencyRecord + OutboxMessage
in one MongoDB transaction
      |
      v
OutboxBackgroundService
      |
      v
OutboxPublisher
      |
      v
IMessageBus
      |
      v
RabbitMQ
      |
      v
Mark OutboxMessage as Published
```

`OutboxBackgroundService` runs as a hosted background service and
periodically asks `IOutboxPublisher` to publish pending messages. The
current polling interval is five seconds.

The message bus publishes with:

-   Persistent message delivery.
-   JSON content type.
-   Event type in message properties.
-   Stable `MessageId` based on the Outbox message ID.
-   Publisher confirmations enabled.
-   Mandatory publishing enabled.

### Delivery guarantee

The Outbox/RabbitMQ flow should be treated as **at-least-once
delivery**. A publisher can successfully publish a message and crash
before marking its Outbox record as published. The message may then be
published again.

Therefore, consumers must be idempotent. A stable message ID helps
identify duplicates, but it does not automatically prevent duplicate
processing.

------------------------------------------------------------------------

## 10. RabbitMQ Topology

Current RabbitMQ topology:

  Component          Value
  ------------------ -------------------------
  Exchange           `fraud-assessment`
  Exchange type      `direct`
  Queue              `assessment-processing`
  Routing key        `ProcessAssessment`
  Durable exchange   Yes
  Durable queue      Yes

The topology initializer creates the exchange, queue, and binding.

### Local Docker Compose

Example development configuration:

``` yaml
services:
  rabbitmq:
    image: rabbitmq:4.2-management
    container_name: fraud-rabbitmq
    hostname: fraud-rabbitmq
    restart: unless-stopped
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: user
      RABBITMQ_DEFAULT_PASS: mypassword
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq

volumes:
  rabbitmq_data:
```

RabbitMQ management UI:

``` text
http://localhost:15672
```

Development credentials in the example:

``` text
Username: user
Password: mypassword
```

These are local development credentials only. Change them and use secret
management outside local development.

### Application configuration

Example:

``` yaml
RabbitMQ:
  HostName: localhost
  Port: 5672
  UserName: user
  Password: mypassword
  ExchangeName: fraud-assessment
  ExchangeType: direct
  EnableAssessmentWorker: true
```

`EnableAssessmentWorker` controls whether the assessment consumer is
registered as a hosted service.

------------------------------------------------------------------------

## 11. Assessment Processing Worker

`AssessmentProcessingWorker` is a hosted background service in
Infrastructure.

Its current responsibilities are:

1.  Connect to RabbitMQ.
2.  Consume messages from `assessment-processing`.
3.  Deserialize the `ProcessAssessmentMessage`.
4.  Retrieve the assessment from MongoDB.
5.  Verify that the assessment exists.
6.  Verify that its current status is `Received`.
7.  Verify that the message type matches the persisted assessment type.
8.  Change the entity to `Processing`.
9.  Atomically update the document only if its status is still
    `Received`.
10. Identify the downstream service from the assessment type.
11. Acknowledge the message after successful processing of the current
    worker stage.

### ProcessAssessmentMessage

The current message contains the assessment identifier and type:

``` csharp
public record ProcessAssessmentMessage(
    Guid AssessmentId,
    AssessmentType Type);
```

The message is intentionally small. It does not contain raw payment-card
data or login credentials.

### Atomic claim

The repository method `TryUpdateReceivedAssessmentAsync` uses a
conditional MongoDB update:

-   Match the assessment by ID.
-   Match only if its status is `Received`.
-   Replace/update the document with the `Processing` version.

Only one concurrent consumer can successfully match the original
`Received` state.

If another consumer already claimed the assessment, the operation
returns `false`.

### Concurrency integration test

An integration test was added to verify the atomic claim:

-   Insert an assessment.
-   Read it twice while its status is `Received`.
-   Change both in-memory copies to `Processing`.
-   Attempt both repository updates concurrently.
-   Assert exactly one update succeeds.
-   Assert the final persisted status is `Processing`.

The test passed in the local development environment.

This test validates the repository's atomic claim behavior. It does not
simulate two complete independent worker processes.

### Current routing behavior

After the assessment is successfully claimed:

-   `Payment` logs that it is routed to Payment Fraud Service.
-   `AccountTakeover` logs that it is routed to ATO Detection Service.
-   Unsupported types cause an error.

**Important:** Routing is currently identification/logging only. The
worker does not yet call either downstream service.

------------------------------------------------------------------------

## 12. Message Acknowledgement and Failure Handling

The consumer uses manual acknowledgement (`autoAck: false`) and a
prefetch count of `1`.

Current behavior:

-   Already-processed assessment: ACK and ignore the duplicate.
-   Assessment already claimed by another consumer: ACK and ignore the
    duplicate.
-   Successful claim and current routing stage: ACK.
-   Exception during message handling: log the error and NACK with
    `requeue: false`.

A production deployment should configure a Dead Letter Exchange/Dead
Letter Queue and an explicit retry strategy before relying on
`requeue: false`, because without a DLX a rejected message can be
discarded.

The later downstream health-check and service-unavailable behavior is
**not implemented yet**. The intended design discussed is to identify
the target service, check its health for up to ten seconds, and if it
remains unavailable, mark the assessment as failed with a clear reason
and acknowledge/remove the message. This behavior still needs
implementation and tests.

------------------------------------------------------------------------

## 13. Running Locally

### Prerequisites

-   .NET 8 SDK
-   MongoDB configured as a replica set (`rs0`) for transaction support
-   Docker Desktop (or another way to run RabbitMQ)
-   RabbitMQ management plugin/image

### Start RabbitMQ

From the directory containing the Docker Compose file:

``` bash
docker compose up -d
```

Check that the container is running:

``` bash
docker ps
```

Open the management UI at `http://localhost:15672`.

### Configure the application

Set the MongoDB and RabbitMQ configuration values in the appropriate
development configuration file or environment variables.

Ensure the RabbitMQ worker flag is enabled when testing message
consumption:

``` yaml
RabbitMQ:
  EnableAssessmentWorker: true
```

### Run the API

From the API project directory:

``` bash
dotnet run
```

At startup, the service initializes the MongoDB indexes and RabbitMQ
topology according to the current Program/startup configuration.

### Verify the flow

1.  Start MongoDB and RabbitMQ.
2.  Start the FraudAssessment API with the worker enabled.
3.  Submit a payment assessment using
    `POST /api/v1/Assessments/Payment`.
4.  Verify that the assessment and Outbox message are persisted.
5.  Verify that the Outbox publisher publishes the message.
6.  Verify that RabbitMQ delivers the message to
    `assessment-processing`.
7.  Verify that the worker changes the assessment from `Received` to
    `Processing`.
8.  Verify that the log identifies `Payment Fraud Service` as the
    target.

------------------------------------------------------------------------

## 14. Tests

The current integration test verifies concurrent assessment claiming
against MongoDB.

The test database is separate from the normal development database:

``` text
FraudAssessment_IntegrationTests
```

Example environment variable for a test connection string:

``` text
MONGODB_TEST_CONNECTION_STRING=mongodb://127.0.0.1:27017/?replicaSet=rs0
```

Run the tests from the solution directory:

``` bash
dotnet test
```

The integration test must run against a MongoDB replica set because the
broader service uses MongoDB transactions.

------------------------------------------------------------------------

## 15. Current Implementation Status

### Implemented

-   [x] Assessment creation use cases.
-   [x] Assessment retrieval by client and ID.
-   [x] Assessment lifecycle entity and statuses.
-   [x] Idempotency record and unique index.
-   [x] Request hash generation for idempotency.
-   [x] MongoDB persistence.
-   [x] MongoDB transaction for Assessment + IdempotencyRecord +
    OutboxMessage.
-   [x] MongoDB index initialization.
-   [x] Outbox entity and repository.
-   [x] Outbox background publisher.
-   [x] RabbitMQ message bus.
-   [x] RabbitMQ exchange, queue, and binding initialization.
-   [x] Assessment processing consumer.
-   [x] Atomic `Received` to `Processing` claim.
-   [x] Duplicate/already-claimed message handling.
-   [x] Assessment type validation.
-   [x] Payment vs ATO target-service identification/logging.
-   [x] Integration test for concurrent atomic claiming.

### Not implemented yet

-   [ ] HTTP client integration with Payment Fraud Service.
-   [ ] HTTP client integration with ATO Detection Service.
-   [ ] Downstream service health checks.
-   [ ] Ten-second health-check window and its failure handling.
-   [ ] Risk Decision API integration.
-   [ ] Persisting the final decision and completing the assessment.
-   [ ] Secure contract/source for passing payment or login analysis
    data.
-   [ ] Dead Letter Queue and explicit retry/backoff policy.
-   [ ] End-to-end tests covering the complete cross-service workflow.
-   [ ] Production observability, metrics, tracing, and operational
    alerts.

------------------------------------------------------------------------

## 16. Next Development Steps

Recommended order:

1.  Build the **Payment Fraud Service** as an independent service.
2.  Define the request and response contract between FraudAssessment and
    Payment Fraud.
3.  Add a dedicated HTTP client abstraction in FraudAssessment.
4.  Extend the worker's routing stage to send payment assessments to
    Payment Fraud.
5.  Add the downstream health-check behavior before sending the business
    request.
6.  Build the ATO Detection Service and its integration.
7.  Build the Risk Decision Service integration.
8.  Complete the assessment lifecycle by storing the final decision and
    setting `Completed`.
9.  Add retry/DLQ handling, integration tests, and operational
    monitoring.

------------------------------------------------------------------------

## 17. Design Principles

-   Keep fraud-analysis rules inside the specialized fraud services.
-   Keep final risk-decision logic inside Risk Decision.
-   Keep FraudAssessment focused on orchestration and assessment
    lifecycle.
-   Keep domain logic independent of infrastructure.
-   Use idempotency at request creation and idempotent message handling
    at consumers.
-   Use atomic database operations when multiple workers may compete for
    the same assessment.
-   Treat RabbitMQ delivery as at-least-once.
-   Never assume a missing fraud-service result means the transaction is
    safe.
-   Never return `ALLOW` when a required fraud check has not completed
    successfully.
-   Keep sensitive payment and authentication data out of logs and
    message payloads unless a specifically reviewed secure contract
    requires otherwise.

------------------------------------------------------------------------

## 18. Important Current-State Note

The current worker's successful log message means that the assessment
was claimed and its type was mapped to the intended downstream service.
It does **not** mean that Payment Fraud or ATO analysis has run, and it
does **not** mean a final risk decision has been produced.

The next major milestone is to implement and integrate the Payment Fraud
Service.
