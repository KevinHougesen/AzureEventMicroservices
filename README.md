# Event-Driven Microservice Integration with Azure

## Project Overview

This project demonstrates an event-driven integration solution using microservices architecture in C# with Azure tools. The solution comprises three microservices: Authentication Service, User Service, and Mail Service. The system leverages Cosmos DB for storage, Azure Event Grid for event handling, Azure Functions for serverless computing, and Azure Communication Services for sending emails.

![EDAMSAZ](https://github.com/KevinHougesen/AzureEventMicroservices/assets/83435086/fd650b8f-df75-4ac9-ac0c-569b71d36027)

### Microservices Breakdown

- **Authentication Service**: Handles user registration and authentication.

  - **Triggers**: HTTP triggers for registration and email verification.
  - **Database**: Stores confidential user data in Cosmos DB.
  - **Event Publishing**: Publishes `VerifyMail` and `VerifiedMail` events.

- **User Service**: Manages user profile information.

  - **Event Subscription**: Subscribes to `VerifiedMail` topic and `UserCreatedEvent` event to store non-confidential user data.

- **Mail Service**: Sends verification and welcome emails.
  - **Event Subscription**: Subscribes to `VerifyMail` topic and `MailVerifyEvent` event to send a verification mail.
  - **Event Subscription**: Subscribes to `VerifiedMail` topic and `WelcomeMailEvent` event to send a verification mail.

### Workflow

1. **User Registration**

   - User registers via `AuthRegister` HTTP trigger in Authentication Service.
   - Confidential data (e.g., ID, Username, Email, PasswordHash) is stored in the Auth Cosmos DB.
   - `VerifyMail` is published.

2. **Email Verification**

   - Mail Service's `VerificationMailTrigger` sends a verification email with a link.
   - User clicks the link, triggering `VerifyEmail` HTTP trigger in Authentication Service.
   - Upon verification, `VerifiedMail` event is published.

3. **User Data Handling**
   - Mail Service's `WelcomeMailTrigger` sends a welcome email.
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
   1. Add the`VerifyMail` topic and add `VerifyMailEvent` as event subscriber. Then add the Azure Function `VerificationMailTrigger` as it's subscriber.
   2. Add the `VerifiedMail` topic and add `WelcomeMailEvent` and `UserCreatedEvent` as event subscribers. Then add the Azure Functions `WelcomeMailTrigger` and `UserCreatedTrigger` as it's subscribers.
3. **Azure Functions**: Deploy functions for each service.
   1. `AuthService`
   2. `MailService`
   3. `UserService`
4. **Azure Communication Services**: Set up a Communication Services resource for sending emails.
   1. It is possible to use e-mail or sms with Azure Communication Services. This project uses Azures e-mail service, as it is free.

### Configuration

1. Clone the repository:
   ```sh
   git clone https://github.com/KevinHougesen/AzureEventMicroservices.git
   cd AzureEventMicroservices
   ```
