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

namespace Nyt.UserFunction
{
    public class UpdateUser
    {
        private readonly ILogger<UpdateUser> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config; // IConfiguration for accessing settings

        public UpdateUser(ILogger<UpdateUser> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function("UpdateUser")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updatedUser = JsonConvert.DeserializeObject<UserModel>(requestBody);

            // Validate user input (optional)

            // Fetch existing user from Cosmos DB
            var container = _cosmosClient.GetContainer(_config["DatabaseName"], _config["ContainerName"]);
            var response = await container.ReadItemAsync<UserModel>(updatedUser.Id, new PartitionKey(updatedUser.Id));
            var existingUser = response.Resource;

            // Update properties of existing user
            existingUser.Username = updatedUser.Username;
            existingUser.DisplayName = updatedUser.DisplayName;
            existingUser.Email = updatedUser.Email;
            existingUser.ProfilePicturePath = updatedUser.ProfilePicturePath;
            existingUser.Location = updatedUser.Location;
            existingUser.Occupation = updatedUser.Occupation;

            // Save updated user back to Cosmos DB
            await container.ReplaceItemAsync(existingUser, existingUser.Id, new PartitionKey(existingUser.Id));

            // Publish UserUpdated event to Azure Event Grid
            // await PublishUserUpdatedEvent(existingUser);

            // Optionally, send response or additional actions
            return new OkObjectResult($"User {existingUser.Username} updated successfully.");
        }

        /* Update Event (Publisher)

        private async Task PublishUserUpdatedEvent(UserModel user)
        {
            var credential = new AzureKeyCredential(_config["EventGridKey"]);
            var eventGridClient = new EventGridPublisherClient(
                new Uri(_config["EventGridTopicEndpoint"]), credential);

            var userUpdatedEvent = new EventGridEvent(
                subject: $"users/{user.Id}",
                eventType: "User.Updated",
                dataVersion: "1.0",
                data: user);

            await eventGridClient.SendEventAsync(userUpdatedEvent);
        }

        */
    }
}