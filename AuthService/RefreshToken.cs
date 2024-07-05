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
    public class AuthRefreshToken
    {
        private readonly ILogger<AuthRefreshToken> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;
        private readonly TokenService _tokenService;

        public AuthRefreshToken(ILogger<AuthRefreshToken> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
            _tokenService = new TokenService(config);
        }

        [Function("AuthRefreshToken")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "refresh-token")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a refresh token request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var refreshTokenRequest = JsonConvert.DeserializeObject<RefreshTokenRequest>(requestBody);

            var container = _cosmosClient.GetContainer(_config["AUTH_DATABASE"], _config["AUTH_CONTAINER"]);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.RefreshToken = @RefreshToken")
                .WithParameter("@RefreshToken", refreshTokenRequest.RefreshToken);
            var iterator = container.GetItemQueryIterator<AuthModel>(query);
            var user = (await iterator.ReadNextAsync()).FirstOrDefault();

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            {
                return new UnauthorizedResult();
            }

            // Generate new tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Update user with new refresh token
            user.RefreshToken = newRefreshToken.Token;
            user.RefreshTokenExpiry = newRefreshToken.Expires;
            await container.ReplaceItemAsync(user, user.Id, PartitionKey.None);

            return new OkObjectResult(new { AccessToken = accessToken, RefreshToken = user.RefreshToken });
        }

    }


}
