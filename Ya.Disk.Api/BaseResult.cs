using System.Text.Json.Serialization;

namespace Ya.Disk.Api
{
    public class BaseResult 
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("templated")]
        public bool Templated { get; set; }
    }
}





