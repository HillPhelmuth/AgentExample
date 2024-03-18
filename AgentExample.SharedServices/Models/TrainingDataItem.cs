using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentExample.SharedServices.Models
{
    public class TrainingDataItem
    {
        [JsonPropertyName("messages")]
        public List<TrainingMessage> Messages { get; set; } = [];
        public string ToJson() => JsonSerializer.Serialize(this);
    }

    public class TrainingMessage(string role, string content)
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = role;

        [JsonPropertyName("content")]
        public string Content { get; set; } = content;
    }
}
