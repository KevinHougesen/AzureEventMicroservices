using Newtonsoft.Json;
using System.Security.Cryptography;
using Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.Messaging.EventGrid;
using Auth;

namespace User.Http
{
    public class UserRegister
    {
        private readonly ILogger<UserRegister> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;

        public UserRegister(ILogger<UserRegister> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function("UserRegister")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Query["token"];
            string userId = req.Query["userId"];
            string userEmail = req.Query["email"];

            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }


            EventModel mailEvent = new();

            AuthModel auth;
            UserModel user;

            try
            {
                auth = JsonConvert.DeserializeObject<AuthModel>(requestBody);
                user = JsonConvert.DeserializeObject<UserModel>(requestBody);
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            // Hash the password
            auth.PasswordHash = AuthUtils.HashPassword(auth.PasswordHash);
            

            try
            {
                var authContainer = _cosmosClient.GetContainer(_config["AUTH_DATABASE"], _config["AUTH_CONTAINER"]);
                await authContainer.ReplaceItemAsync(auth, auth.Id, new PartitionKey(userId));
                // Publish UserCreated event to Azure Event Grid
                //await PublishMailVerifiedEvent(mailEvent);
                var container = _cosmosClient.GetContainer(_config["USER_DATABASE"], _config["USER_CONTAINER"]);
                await container.CreateItemAsync(user);
                await PublishMailVerifyEvent(mailEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Event Grid error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new OkObjectResult("Mail verification event published successfully.");
        }

        private string GenerateEmailVerificationToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] tokenBuffer = new byte[32];
                rng.GetBytes(tokenBuffer);
                return Convert.ToBase64String(tokenBuffer);
            }
        }

        private async Task PublishMailVerifyEvent(EventModel data)
        {
            var credential = new AzureKeyCredential(_config["VERIFY_MAIL_EVENT_GRID_KEY"]);
            var eventGridClient = new EventGridPublisherClient(
                new Uri(_config["VERIFY_MAIL_EVENT_GRID_TOPIC_ENDPOINT"]), credential);

            var mailVerifyEvent = new EventGridEvent(
                subject: $"Users/{data.User.Id}",
                eventType: "Mail.Verify",
                dataVersion: "1.0",
                data: data);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }
    }
}