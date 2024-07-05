# Event-Driven Microservice Integration with Azure

## Project Overview

This project demonstrates an event-driven integration solution using microservices architecture in C# with Azure tools. The solution comprises three microservices: Authentication Service, User Service, and Mail Service. The system leverages Cosmos DB for storage, Azure Event Grid for event handling, Azure Functions for serverless computing, and Azure Communication Services for sending emails.

![EDAMS](https://github.com/KevinHougesen/NytAzureFunctions/assets/83435086/3738984e-6271-4e85-a8b9-7043fda8ec3d)

### Microservices Breakdown

- **Authentication Service**: Handles user registration and authentication.

  - **Triggers**: HTTP triggers for registration and email verification.
  - **Database**: Stores confidential user data in Cosmos DB.
  - **Event Publishing**: Publishes `UserCreated` and `UserRegistered` events.

- **User Service**: Manages user profile information.

  - **Event Subscription**: Subscribes to `UserRegistered` event to store non-confidential user data.
  - **Event Subscription**: Subscribes to `UserUpdated` event to update non-confidential user data.
  - **Event Subscription**: Subscribes to `UserDeleted` event to delete user data.

- **Mail Service**: Sends verification and welcome emails.
  - **Event Subscription**: Subscribes to `UserCreated` event to send a verification mail.
  - **Event Subscription**: Subscribes to `UserRegistered` event to send a welcome mail.

### Workflow

1. **User Registration**

   - User registers via `AuthRegister` HTTP trigger in Authentication Service.
   - Confidential data (e.g., ID, Username, Email, PasswordHash) is stored in the Auth Cosmos DB.
   - `UserCreated` event is published.

2. **Email Verification**

   - Mail Service's `UserCreatedMailTrigger` sends a verification email with a link.
   - User clicks the link, triggering `VerifyMail` HTTP trigger in Authentication Service.
   - Upon verification, `UserRegistered` event is published.

3. **User Data Handling**
   - Mail Service's `UserRegisteredMailTrigger` sends a welcome email.
   - User Service's `UserCreatedTrigger` stores non-confidential data in the User Cosmos DB.

## Setup Instructions

### Prerequisites

- .NET SDK
- Azure Account
- Visual Studio or VS Code
- Azure Functions Core Tools

### Services and Tools

1. **Cosmos DB**: Set up a Cosmos DB account and create two databases and two containers:
   1. `Auth` database and `UserCredentials` container for Authentication Service.
   2. `Users` database and `ProfileData` container for User Service.
2. **Azure Event Grid**: Configure the Event Grid topics to publish and subscribe to events:
   1. `UserCreated` topic and add `UserCreatedMailEvent` as event subscriber. Then add the Azure Function `UserCreatedMailTrigger` as it's subscriber.
   2. `UserRegistered` topic and add `UserRegisteredMailEvent` and `UserRegisteredUserEvent` as event subscribers. Then add the Azure Functions `UserRegisteredMailTrigger` and `UserRegisteredUserTrigger` as it's subscribers.
   3. `UserUpdated` topic and add `UserUpdatedUserEvent` as event subscriber. Then add the Azure Function `UserUpdatedUserTrigger` as it's subscriber.
   4. `UserDeleted` topic and add `UserDeletedEvent` as event subscriber. Then add the Azure Function `UserDeletedTrigger` as it's subscriber.
3. **Azure Functions**: Deploy functions for each service.
   1. `AuthFunctions`
   2. `MailFunctions`
   3. `UserFunctions`
4. **Azure Communication Services**: Set up a Communication Services resource for sending emails.
   1. It is possible to use e-mail or sms with Azure Communication Services. This project uses Azures e-mail service, as it is free.

### Configuration

1. Clone the repository:
   ```sh
   git clone https://github.com/KevinHougesen/AzureEventMicroservices.git
   cd AzureEventMicroservices
   ```
