using Newtonsoft.Json;

namespace MikeCodesDotNET.Models.Storage;

public class CloudEvent<T> where T : class
{
    [JsonProperty("specversion")]
    public string SpecVersion { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("source")]
    public string Source { get; set; }

    [JsonProperty("subject")]
    public string Subject { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("time")]
    public string Time { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}