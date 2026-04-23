using System.Text.Json.Serialization;

namespace Kraak.Core.Models;

public class Finding
{
    [JsonInclude]
    public string RuleId { get; set; } = string.Empty;
    [JsonInclude]
    public string Title { get; set; } = string.Empty;
    [JsonInclude]
    public string Description { get; set; } = string.Empty;
    [JsonInclude]
    public string FilePath { get; set; } = string.Empty;
    [JsonInclude]
    public string? LineContent { get; set; }
    [JsonInclude]
    public Severity Severity { get; set; }
    [JsonInclude]
    public string Suggestion { get; set; } = string.Empty;
}