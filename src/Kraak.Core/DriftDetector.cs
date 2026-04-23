using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core;

public class DriftDetector
{
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };

    public static void SaveSnapshot(string filePath, string fileContent)
    {
        var snapshot = BuildSnapshot(filePath, fileContent);
        var json = JsonSerializer.Serialize(snapshot, _opts);
        File.WriteAllText(GetSnapshotPath(filePath), json);
    }

    public static IEnumerable<Finding> DetectDrift(string filePath, string fileContent)
    {
        var snapshotPath = GetSnapshotPath(filePath);
        if (!File.Exists(snapshotPath)) yield break;

        var snapshotJson = File.ReadAllText(snapshotPath);
        var snapshot = JsonSerializer.Deserialize<Dictionary<string, string>>(snapshotJson);
        if (snapshot is null) yield break;

        var current = BuildSnapshot(filePath, fileContent);

        // Chaves adicionadas
        foreach (var key in current.Keys.Except(snapshot.Keys))
        {
            var isSensitive = IsSensitiveKey(key);
            yield return new Finding
            {
                RuleId = "KRK015",
                Title = "Drift: Chave Nova Detectada",
                Description = $"A chave '{key}' não existia no snapshot aprovado.",
                FilePath = filePath,
                LineContent = $"{key}: {TruncateValue(current[key])}",
                Severity = isSensitive ? Severity.Critical : Severity.Warning,
                Suggestion = isSensitive
                    ? $"A chave '{key}' parece sensível e foi adicionada sem aprovação. Verifique se não é um secret exposto."
                    : $"A chave '{key}' foi adicionada. Execute 'kraak snapshot' para aprovar se for intencional."
            };
        }

        // Chaves removidas
        foreach (var key in snapshot.Keys.Except(current.Keys))
        {
            yield return new Finding
            {
                RuleId = "KRK016",
                Title = "Drift: Chave Removida",
                Description = $"A chave '{key}' existia no snapshot aprovado e foi removida.",
                FilePath = filePath,
                LineContent = $"{key}: {TruncateValue(snapshot[key])}",
                Severity = Severity.Warning,
                Suggestion = $"A chave '{key}' foi removida. Execute 'kraak snapshot' para aprovar se for intencional."
            };
        }

        // Valores alterados
        foreach (var key in current.Keys.Intersect(snapshot.Keys))
        {
            if (current[key] == snapshot[key]) continue;

            var isSensitive = IsSensitiveKey(key);
            yield return new Finding
            {
                RuleId = "KRK017",
                Title = "Drift: Valor Alterado",
                Description = $"O valor de '{key}' foi alterado em relação ao snapshot aprovado.",
                FilePath = filePath,
                LineContent = $"{key}: {TruncateValue(snapshot[key])} → {TruncateValue(current[key])}",
                Severity = isSensitive ? Severity.Critical : Severity.Info,
                Suggestion = isSensitive
                    ? $"'{key}' parece uma chave sensível e seu valor mudou. Verifique se a alteração é legítima."
                    : $"O valor de '{key}' mudou. Execute 'kraak snapshot' para aprovar se for intencional."
            };
        }
    }

    private static Dictionary<string, string> BuildSnapshot(string filePath, string fileContent)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch
        {
            ".json" => ParseJson(fileContent),
            ".yml" or ".yaml" => ParseYaml(fileContent),
            _ => ParseEnv(fileContent)
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
            var key = trimmed[..idx].Trim();
            var value = trimmed[(idx + 1)..].Trim();
            result[key] = value;
        }
        return result;
    }

    private static bool IsSensitiveKey(string key) =>
        new[] { "password", "secret", "token", "key", "api", "credential", "pwd", "auth" }
            .Any(k => key.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static string TruncateValue(string value) =>
        value.Length > 20 ? value[..20] + "..." : value;

    private static string GetSnapshotPath(string filePath) =>
        Path.Combine(Path.GetDirectoryName(filePath) ?? ".", $".kraak.snapshot.{Path.GetFileName(filePath)}.json");
}