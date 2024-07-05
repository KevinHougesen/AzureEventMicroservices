using Newtonsoft.Json;
using Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Auth.Http
{
    public class AuthLogin
    {
        private readonly ILogger<AuthLogin> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;
        private readonly TokenService _tokenService;

        public AuthLogin(ILogger<AuthLogin> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
            _tokenService = new TokenService(config);
        }

        [Function("AuthLogin")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "login")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a login request.");

            string requestBody;
            using (var reader = new StreamReader(req.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrEmpty(requestBody))
            {
                return new BadRequestObjectResult("Request body is empty.");
            }

            LoginRequest loginRequest;
            try
            {
                loginRequest = JsonConvert.DeserializeObject<LoginRequest>(requestBody);
            }
            catch (JsonException)
            {
                return new BadRequestObjectResult("Invalid JSON format.");
            }

            if (string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return new BadRequestObjectResult("Email and password are required.");
            }

            var container = _cosmosClient.GetContainer(_config["AUTH_DATABASE"], _config["AUTH_CONTAINER"]);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.Email = @Email")
                .WithParameter("@Email", loginRequest.Email);
            var iterator = container.GetItemQueryIterator<AuthModel>(query);
            var users = await iterator.ReadNextAsync();
            var user = users.FirstOrDefault();

            if (user == null || !AuthUtils.VerifyPassword(loginRequest.Password, user.PasswordHash))
            {
                return new UnauthorizedResult();
            }

            // Generate tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Update user with new refresh token
            user.RefreshToken = refreshToken.Token;
            user.RefreshTokenExpiry = refreshToken.Expires;

            try
            {
                await container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Id));
            }
            catch (CosmosException ex)
            {
                _logger.LogError($"Cosmos DB error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new OkObjectResult(new { AccessToken = accessToken, RefreshToken = user.RefreshToken });
        }
    }
}