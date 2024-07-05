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

            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

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
                auth = JsonConvert.DeserializeObject<AuthModel>(requestBody);
                mail = JsonConvert.DeserializeObject<MailModel>(requestBody);
                user = JsonConvert.DeserializeObject<UserModel>(requestBody);
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            if (!AuthUtils.ValidateUserInput(auth))
            {
                return new BadRequestObjectResult("Invalid user input.");
            }

            // Generate unique ID for the user
            auth.Id = Guid.NewGuid().ToString();
            user.Id = auth.Id;

            // Hash the password
            auth.PasswordHash = AuthUtils.HashPassword(auth.PasswordHash);

            // Generate email verification token
            auth.EmailVerificationToken = GenerateEmailVerificationToken();
            mail.EmailVerificationToken = auth.EmailVerificationToken;

            // Generate refresh token
            var refreshToken = _tokenService.GenerateRefreshToken();
            auth.RefreshToken = refreshToken.Token;
            auth.RefreshTokenExpiry = refreshToken.Expires;

            try
            {
                // Save user to Cosmos DB
                var container = _cosmosClient.GetContainer(_config["AUTH_DATABASE"], _config["AUTH_CONTAINER"]);
                await container.CreateItemAsync(auth);
            }
            catch (CosmosException ex)
            {
                _logger.LogError($"Cosmos DB error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            mailEvent.Mail = mail;
            mailEvent.User = user;

            try
            {
                // Publish UserCreated event to Azure Event Grid
                await PublishMailVerifyEvent(mailEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Event Grid error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            // Generate access token
            var accessToken = _tokenService.GenerateAccessToken(auth.Id, auth.Email);

            return new OkObjectResult(new { AccessToken = accessToken, RefreshToken = auth.RefreshToken });
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
                subject: $"users/{data.User.Id}",
                eventType: "Mail.Verify",
                dataVersion: "1.0",
                data: data);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }
    }
}