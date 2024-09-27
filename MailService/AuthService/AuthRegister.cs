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

namespace Auth.Http
{
    public class AuthRegister
    {
        private readonly ILogger<AuthRegister> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;
        private readonly TokenService _tokenService;

        public AuthRegister(ILogger<AuthRegister> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
            _tokenService = new TokenService(config);
        }

        [Function("AuthRegister")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string email = req.Query["Email"];

            EventModel mailEvent = new();

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult("Request body is empty.");
            }

            AuthModel auth;
            MailModel mail;
            UserModel user;

            try
            {
                var mail = mail.Email = email;
                auth.mail = mail;
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            // Generate unique ID for the user
            auth.Id = Guid.NewGuid().ToString();

            // Generate email verification token
            auth.EmailVerificationToken = GenerateEmailVerificationToken();
            mail.EmailVerificationToken = auth.EmailVerificationToken;

            EventModel.data.mailEvent.Mail = mail;

            try
            {
                // Publish UserCreated event to Azure Event Grid
                await PublishMailVerifyEvent(mailEvent, auth.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Event Grid error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new OkObjectResult();
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