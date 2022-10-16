using Newtonsoft.Json;

namespace CosmosDb.NGramPartialTextMatch;

public class User
{
    [JsonProperty("id")]
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    [JsonProperty("pk")]
    public string Pk { get; set; } = "PK";

    public IList<string> NGrams { get; set; } = Array.Empty<string>();
}
