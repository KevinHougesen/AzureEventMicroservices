using Newtonsoft.Json;
namespace Data.Models;
public class IdModel
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
}