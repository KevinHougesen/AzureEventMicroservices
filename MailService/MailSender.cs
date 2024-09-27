using System.Security.Cryptography;
using Azure;
using Azure.Messaging.EventGrid;
using Data.Models;

namespace NytHybrid.Sender;

private readonly CosmosClient _cosmosClient;

public interface IMailSender
{
    Task PublishMailVerifyEvent(EventModel data);
    Task PublishMailVerifiedEvent(EventModel data);
    Task PublishUserRegisteredEvent(EventModel data);
}

public class MailSender : IMailSender
{
    public MailSender(CosmosClient cosmosClient)
    {
        _cosmosClient = cosmosClient;
    }
    public async Task PublishMailVerifyEvent(EventModel data)
        {


            var credential = new AzureKeyCredential("Cs9vej30MuJ2ul3EsNh5rG7e3DBdNQt0ZD1sf9WdSSTqmPaxvWSgJQQJ99AIACfhMk5XJ3w3AAABAZEGyDkP");
            var eventGridClient = new EventGridPublisherClient(
                new Uri("https://verifymail.swedencentral-1.eventgrid.azure.net/api/events"), credential);

            var mailVerifyEvent = new EventGridEvent(
                subject: $"mails/{data.Mail}",
                eventType: "Mail.Verify",
                dataVersion: "1.0",
                data: data);

            // Save user to Cosmos DB
            var container = _cosmosClient.GetContainer(_config["MAIL_DATABASE"], _config["MAIL_CONTAINER"]);
            await container.CreateItemAsync(data.Mail);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }

        public async Task PublishMailVerifiedEvent(EventModel data)
        {
            var credential = new AzureKeyCredential("Cs9vej30MuJ2ul3EsNh5rG7e3DBdNQt0ZD1sf9WdSSTqmPaxvWSgJQQJ99AIACfhMk5XJ3w3AAABAZEGyDkP");
            var eventGridClient = new EventGridPublisherClient(
                new Uri("https://verifymail.swedencentral-1.eventgrid.azure.net/api/events"), credential);

            var mailVerifyEvent = new EventGridEvent(
                subject: $"mails/{data.Mail}",
                eventType: "User.Registered",
                dataVersion: "1.0",
                data: data.Mail);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }

        public async Task PublishUserRegisteredEvent(EventModel data)
        {
            var credential = new AzureKeyCredential("Cs9vej30MuJ2ul3EsNh5rG7e3DBdNQt0ZD1sf9WdSSTqmPaxvWSgJQQJ99AIACfhMk5XJ3w3AAABAZEGyDkP");
            var eventGridClient = new EventGridPublisherClient(
                new Uri("https://verifymail.swedencentral-1.eventgrid.azure.net/api/events"), credential);

            var mailVerifyEvent = new EventGridEvent(
                subject: $"user/{data.User.Id}",
                eventType: "User.Registered",
                dataVersion: "1.0",
                data: data.User);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }
}