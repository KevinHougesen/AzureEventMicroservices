using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Data.Models;
using Azure;
using Azure.Messaging.EventGrid;

namespace Nyt.UserFunction
{
    public class VerifyEmail
    {
        private readonly ILogger<VerifyEmail> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;

        public VerifyEmail(ILogger<VerifyEmail> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function("VerifyEmail")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "verify-email")] HttpRequest req)
        {
            // Read query parameters
            string token = req.Query["token"];
            string userId = req.Query["userId"];
            string userName = req.Query["username"];
            string userEmail = req.Query["email"];
            string location = req.Query["location"];
            string occupation = req.Query["occupation"];

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Missing required query parameters.");
                return new BadRequestObjectResult("Missing required query parameters.");
            }

            _logger.LogInformation($"Received email verification request for user ID: {userId}");

            var container = _cosmosClient.GetContainer(_config["AUTH_DATABASE"], _config["AUTH_CONTAINER"]);

            try
            {
                var response = await container.ReadItemAsync<AuthModel>(userId, new PartitionKey(userId));
                var auth = response.Resource;

                // Compare tokens, handle any encoding differences
                if (Uri.EscapeDataString(auth.EmailVerificationToken) == Uri.EscapeDataString(token))
                {
                    auth.EmailVerifiedAt = DateTime.UtcNow;
                    auth.EmailVerificationToken = null; // Clear the token after verification

                    await container.ReplaceItemAsync(auth, auth.Id, new PartitionKey(userId));

                    _logger.LogInformation($"User {auth.Username} verified successfully.");

                    // Prepare mail event data
                    var mailEvent = new EventModel
                    {
                        User = new UserModel
                        {
                            Id = userId,
                            Username = userName,
                            Email = userEmail,
                            Location = location,
                            Occupation = occupation
                        }
                    };

                    await PublishMailVerifiedEvent(mailEvent);

                    return new OkObjectResult("Email verified successfully.");
                }
                else
                {
                    _logger.LogWarning($"Invalid token for user ID: {userId}");
                    return new BadRequestObjectResult("Invalid token.");
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"User ID: {userId} not found.");
                return new NotFoundObjectResult("User not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task PublishMailVerifiedEvent(EventModel data)
        {
            var credential = new AzureKeyCredential(_config["VERIFIED_MAIL_EVENT_GRID_KEY"]);
            var eventGridClient = new EventGridPublisherClient(
                new Uri(_config["VERIFIED_MAIL_EVENT_GRID_TOPIC_ENDPOINT"]), credential);

            var mailVerifiedEvent = new EventGridEvent(
                subject: $"users/{data.User.Id}",
                eventType: "Mail.Verified",
                dataVersion: "1.0",
                data: data);

            await eventGridClient.SendEventAsync(mailVerifiedEvent);
        }
    }
}
