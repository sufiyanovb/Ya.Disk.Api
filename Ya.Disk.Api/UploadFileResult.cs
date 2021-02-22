using System.Text.Json.Serialization;

namespace Ya.Disk.Api
{
    public class UploadFileResult : BaseResult
    {
        [JsonPropertyName("operation_id")]
        public string OperationId { get; set; }
    }
}
