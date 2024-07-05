using System;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Data.Models;
using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Mvc;

namespace Mail.Trigger
{
    public class UserDeletedTrigger
    {
        private readonly ILogger<UserDeletedTrigger> _logger;
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;

        public UserDeletedTrigger(ILogger<UserDeletedTrigger> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function(nameof(UserDeletedTrigger))]
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

                    // Fetch existing user from Cosmos DB
                    var container = _cosmosClient.GetContainer(_config["USER_DATABASE"], _config["USER_CONTAINER"]);
                    ItemResponse<UserModel> response = await container.ReadItemAsync<UserModel>(user.Id, PartitionKey.None);
                    var existingUser = response.Resource;

                    // Delete user from Cosmos DB
                    await container.DeleteItemAsync<UserModel>(user.Id, PartitionKey.None);

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
