using System;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Data.Models;
using Microsoft.Azure.Cosmos;

namespace Mail.Trigger
{
    public class UserCreatedTrigger
    {
        private readonly ILogger<UserCreatedTrigger> _logger;
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;

        public UserCreatedTrigger(ILogger<UserCreatedTrigger> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function(nameof(UserCreatedTrigger))]
        public async Task RunAsync([EventGridTrigger] MailEventType eventGridEvent)
        {
            try
            {
                if (eventGridEvent.Data != null)
                {
                    _logger.LogInformation("Received CloudEvent. Type: {type}, Subject: {subject}", eventGridEvent.EventType, eventGridEvent.Subject);

                    var dataString = JsonConvert.SerializeObject(eventGridEvent.Data);

                    var data = JsonConvert.DeserializeObject<EventModel>(dataString);
                    var user = data.User;
                    // Save user to Cosmos DB
                    var container = _cosmosClient.GetContainer(_config["USER_DATABASE"], _config["USER_CONTAINER"]);
                    await container.CreateItemAsync(user);

                }

                else
                {
                    _logger.LogError("EventGridEvent.Data is null or invalid.");
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing user creation event.");
                throw; // Rethrow exception to mark function invocation as failed
            }
        }

    }
}
