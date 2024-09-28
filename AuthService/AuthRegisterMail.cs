using System.Security.Cryptography;
using System.Text.Json;
using Azure;
using Azure.Messaging.EventGrid;
using Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Company.Function
{
    public class AuthRegisterMail
    {
        private readonly ILogger<AuthRegisterMail> _logger;
        private readonly IConfiguration _config;

        public AuthRegisterMail(ILogger<AuthRegisterMail> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function("AuthRegisterMail")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string email = req.Query["Email"];

            EventModel mailEvent = new();

            if (string.IsNullOrEmpty(req.Form["Email"]))
            {
                return new BadRequestObjectResult("Request body is empty.");
            }

            AuthModel auth = new();
            MailModel mail = new();

            try
            {
                mail.Email = email;
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

            mailEvent.Mail = mail;

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
                subject: $"Mail/{data.Mail.Email}",
                eventType: "Mail.Verify",
                dataVersion: "1.0",
                data: data);

            await eventGridClient.SendEventAsync(mailVerifyEvent);
        }
    }
    
}
