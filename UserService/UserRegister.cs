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

namespace User.Http
{
    public class UserRegister
    {
        private readonly ILogger<UserRegister> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;
        private readonly TokenService _tokenService;

        public UserRegister(ILogger<UserRegister> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
            _tokenService = new TokenService(config);
        }

        [Function("UserRegister")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Query["token"];
            string userId = req.Query["userId"];
            string userEmail = req.Query["email"];
            string password = req.Query["password"];

            EventModel mailEvent = new();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult("Request body is empty.");
            }

            AuthModel auth;
            UserModel user;

            try
            {
                auth = JsonConvert.DeserializeObject<AuthModel>(requestBody);
                mail = JsonConvert.DeserializeObject<MailModel>(requestBody);
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            // Hash the password
            auth.PasswordHash = AuthUtils.HashPassword(password);

            try
            {
                // Publish UserCreated event to Azure Event Grid
                await PublishMailVerifiedEvent(mailEvent);
                await PublishMailVerifyEvent(mailEvent, auth.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Event Grid error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            // Generate access token
            var accessToken = _tokenService.GenerateAccessToken(auth.Id, auth.Email);

            return new OkObjectResult(new { AccessToken = accessToken});
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

        private async Task PublishMailVerifyEvent(EventModel data, string id)
        {
            var credential = new AzureKeyCredential(_config["VERIFY_MAIL_EVENT_GRID_KEY"]);
            var eventGridClient = new EventGridPublisherClient(
                new Uri(_config["VERIFY_MAIL_EVENT_GRID_TOPIC_ENDPOINT"]), credential);

            var mailVerifyEvent = new EventGridEvent(
                subject: $"Users/{auth.Id}",
                eventType: "Mail.Verify",
                dataVersion: "1.0",
                data: data);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }
    }
}