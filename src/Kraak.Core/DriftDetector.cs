using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core;

public class DriftDetector
{
    public static IEnumerable<Finding> Compare(
        string oldPath, string oldContent,
        string newPath, string newContent)
    {
        var snapshot = BuildSnapshot(oldPath, oldContent);
        var current = BuildSnapshot(newPath, newContent);

        foreach (var key in current.Keys.Except(snapshot.Keys))
        {
            var isSensitive = IsSensitiveKey(key);
            yield return new Finding
            {
                RuleId = "KRK015",
                Title = "Drift: Chave Nova Detectada",
                Description = $"A chave '{key}' não existia na versão antiga.",
                FilePath = newPath,
                LineContent = $"{key}: {TruncateValue(current[key])}",
                Severity = isSensitive ? Severity.Critical : Severity.Warning,
                Suggestion = isSensitive
                    ? $"'{key}' parece sensível e foi adicionada. Verifique se não é um secret exposto."
                    : $"A chave '{key}' foi adicionada na versão nova."
            };
        }

        foreach (var key in snapshot.Keys.Except(current.Keys))
        {
            yield return new Finding
            {
                RuleId = "KRK016",
                Title = "Drift: Chave Removida",
                Description = $"A chave '{key}' existia na versão antiga e foi removida.",
                FilePath = newPath,
                LineContent = $"{key}: {TruncateValue(snapshot[key])}",
                Severity = Severity.Warning,
                Suggestion = $"A chave '{key}' foi removida. Verifique se foi intencional."
            };
        }

        foreach (var key in current.Keys.Intersect(snapshot.Keys))
        {
            if (current[key] == snapshot[key]) continue;
            var isSensitive = IsSensitiveKey(key);
            yield return new Finding
            {
                RuleId = "KRK017",
                Title = "Drift: Valor Alterado",
                Description = $"O valor de '{key}' foi alterado.",
                FilePath = newPath,
                LineContent = $"{key}: {TruncateValue(snapshot[key])} → {TruncateValue(current[key])}",
                Severity = isSensitive ? Severity.Critical : Severity.Info,
                Suggestion = isSensitive
                    ? $"'{key}' parece sensível e seu valor mudou. Verifique se é legítimo."
                    : $"O valor de '{key}' mudou entre as versões."
            };
        }
    }

    private static Dictionary<string, string> BuildSnapshot(string filePath, string content)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".json" => ParseJson(content),
            ".yml" or ".yaml" => ParseYaml(content),
            _ => ParseEnv(content)
        };
    }

    private static Dictionary<string, string> ParseJson(string content)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var doc = JsonDocument.Parse(content);
            FlattenJson(doc.RootElement, "", result);
        }
        catch { }
        return result;
    }

    private static void FlattenJson(JsonElement element, string prefix, Dictionary<string, string> result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                FlattenJson(prop.Value, key, result);
            }
        }
        else
        {
            result[prefix] = element.ToString();
        }
    }

    private static Dictionary<string, string> ParseYaml(string content)
    {
        var result = new Dictionary<string, string>();
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
            var idx = trimmed.IndexOf(':');
            if (idx < 0) continue;
            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                result[key] = value;
        }
        return result;
    }

    private static Dictionary<string, string> ParseEnv(string content)
    {
        var result = new Dictionary<string, string>();
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
            var idx = trimmed.IndexOf('=');
            if (idx < 0) continue;
            result[trimmed[..idx].Trim()] = trimmed[(idx + 1)..].Trim();
        }
        return result;
    }

    private static bool IsSensitiveKey(string key) =>
        new[] { "password", "secret", "token", "key", "api", "credential", "pwd", "auth" }
            .Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static string TruncateValue(string value) =>
        value.Length > 20 ? value[..20] + "..." : value;
}
