using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.AppSettings;

public class EntropyRule : IRule
{
    public string RuleId => "KRK009";
    public string Title => "Possível Secret Detectado por Entropia";

    private static readonly string[] _ignoredKeys =
    [
        "version", "name", "description", "url", "host", "port",
        "path", "environment", "allowedhosts", "default", "server",
        "database", "provider", "timeout", "enabled", "type"
    ];

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            yield break;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(fileContent); }
        catch { yield break; }

        foreach (var finding in AnalyzeElement(doc.RootElement, filePath))
            yield return finding;
    }

    private IEnumerable<Finding> AnalyzeElement(JsonElement element, string filePath, string path = "")
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var newPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                foreach (var f in AnalyzeElement(prop.Value, filePath, newPath))
                    yield return f;
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString() ?? string.Empty;
            var key = path.Split('.').Last().ToLower();

            if (_ignoredKeys.Any(k => key.Contains(k))) yield break;
            if (value.Length < 16) yield break;
            if (value.Contains(' ') || value.Contains('/') && value.Contains(':')) yield break;

            var entropy = CalculateEntropy(value);
            if (entropy >= 4.0)
            {
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = $"O valor de '{path}' tem alta entropia ({entropy:F2} bits/char), sugerindo um secret hardcoded.",
                    FilePath = filePath,
                    LineContent = $"{path}: {(value.Length > 20 ? value[..20] + "..." : value)}",
                    Severity = Severity.Warning
                };
            }
        }
    }

    private static double CalculateEntropy(string value)
    {
        var freq = value.GroupBy(c => c).ToDictionary(g => g.Key, g => (double)g.Count() / value.Length);
        return -freq.Values.Sum(p => p * Math.Log2(p));
    }
}