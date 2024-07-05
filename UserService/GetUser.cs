using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Threading.Tasks;
using Data.Models;

namespace Nyt.UserFunction
{
    public class GetUser
    {
        private readonly ILogger<GetUser> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _config;

        public GetUser(ILogger<GetUser> logger, CosmosClient cosmosClient, IConfiguration config)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            _config = config;
        }

        [Function("GetUser")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{userId}")] HttpRequestData req,
            string userId,
            FunctionContext executionContext)
        {
            var response = req.CreateResponse();
            _logger.LogInformation($"C# HTTP trigger function processed a get request for user id: {userId}");

            try
            {
                var container = _cosmosClient.GetContainer(_config["USER_DATABASE"], _config["USER_CONTAINER"]);
                ItemResponse<UserModel> cosmosResponse = await container.ReadItemAsync<UserModel>(userId, PartitionKey.None);

                var user = cosmosResponse.Resource;
                if (user == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    await response.WriteStringAsync($"User with id {userId} not found.");
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(user));
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"User with id {userId} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user from Cosmos DB.");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("An error occurred while processing your request.");
            }

            return response;
        }
    }
}
