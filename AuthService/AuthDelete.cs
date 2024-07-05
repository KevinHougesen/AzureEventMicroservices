using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Data.Models;
using Microsoft.Extensions.Configuration;
using Azure;

namespace Auth.Http
{
    public class AuthDelete
    {
        private readonly ILogger<AuthDelete> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config; // IConfiguration for accessing settings

        public AuthDelete(ILogger<AuthDelete> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function("AuthDelete")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "users/{userId}")] HttpRequest req,
            string userId)
        {
            _logger.LogInformation($"C# HTTP trigger function processed a delete request for user id: {userId}");

            // Fetch existing user from Cosmos DB
            var container = _cosmosClient.GetContainer(_config["USER_DATABASE"], _config["USER_CONTAINER"]);
            ItemResponse<AuthModel> response = await container.ReadItemAsync<AuthModel>(userId, PartitionKey.None);
            var existingUser = response.Resource;



            if (existingUser == null)
            {
                return new NotFoundObjectResult($"User with id {userId} not found.");
            }

            var UserId = JsonConvert.DeserializeObject<IdModel>(userId);

            // Delete user from Cosmos DB
            await container.DeleteItemAsync<AuthModel>(userId, PartitionKey.None);

            // Publish UserDeleted event to Azure Event Grid
            await PublishUserDeletedEvent(UserId);

            // Optionally, send response or additional actions
            return new OkObjectResult($"User with id {userId} deleted successfully.");
        }

        private async Task PublishUserDeletedEvent(IdModel UserId)
        {
            var credential = new AzureKeyCredential(_config["EventGridKey"]);
            var eventGridClient = new EventGridPublisherClient(
                new Uri(_config["EventGridTopicEndpoint"]), credential);

            var userDeletedEvent = new EventGridEvent(
                subject: $"users/{UserId}",
                eventType: "User.Deleted",
                dataVersion: "1.0",
                data: UserId);

            await eventGridClient.SendEventAsync(userDeletedEvent);
        }
    }
}