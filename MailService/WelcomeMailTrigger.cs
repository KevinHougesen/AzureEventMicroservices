using System.Text;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Data.Models;
using Azure;

namespace Mail.Trigger
{
    public class WelcomeMailTrigger
    {
        private readonly ILogger<WelcomeMailTrigger> _logger;
        private readonly IConfiguration _config;

        public WelcomeMailTrigger(ILogger<WelcomeMailTrigger> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [Function(nameof(WelcomeMailTrigger))]
        public async Task RunAsync([EventGridTrigger] MailEventType eventGridEvent)
        {
            try
            {
                if (eventGridEvent.Data != null)
                {
                    _logger.LogInformation("Received CloudEvent. Type: {type}, Subject: {subject}", eventGridEvent.EventType, eventGridEvent.Subject);

                    EventModel mailEvent = new();

                    var data = eventGridEvent.Data;

                    var verificationUrl = _config["VerificationUrl"];

                    // Ensure that the verification URL was retrieved correctly
                    if (string.IsNullOrEmpty(verificationUrl))
                    {
                        _logger.LogError("VerificationUrl configuration is missing or empty.");
                        throw new Exception("VerificationUrl configuration is missing or empty.");
                    }

                    // Construct verification link
                    var verificationLink = $"https://google.com/";

                    // Send email using ACS
                    await SendEmailAsync(data, verificationLink);

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

        private async Task SendEmailAsync(EventModel data, string verificationLink)
        {
            var connectionString = _config["COMMUNICATION_SERVICES_CONNECTION_STRING"];
            var senderEmail = _config["SENDER_EMAIL"];
            var recipientEmail = data.User.Email;

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(senderEmail))
            {
                _logger.LogError("Email configuration is missing or empty.");
                throw new Exception("Email configuration is missing or empty.");
            }

            var emailClient = new EmailClient(connectionString);

            // Read the HTML template from the file
            var filePath = Path.Combine(AppContext.BaseDirectory, "WelcomeEmailTemplate.html");
            string emailTemplate;

            try
            {
                emailTemplate = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read HTML email template.");
                throw;
            }

            // Replace placeholders in the HTML template
            var emailContentHtml = emailTemplate
                .Replace("{verificationLink}", verificationLink);

            var emailContent = new EmailContent("Welcome to [Service]")
            {
                Html = emailContentHtml
            };

            var emailMessage = new EmailMessage(senderEmail, recipientEmail, emailContent);

            try
            {
                _logger.LogInformation("Sending email to user.");

                EmailSendOperation emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage);
                EmailSendResult statusMonitor = emailSendOperation.Value;

                _logger.LogInformation($"Email sent. Status = {statusMonitor.Status}, OperationId = {emailSendOperation.Id}");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Email send operation failed.");
                throw; // Rethrow exception to mark function invocation as failed
            }
        }

    }
}
