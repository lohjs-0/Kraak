using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.AppSettings;

public class AllowedHostsRule : IRule
{
    public string RuleId => "KRK002";
    public string Title => "AllowedHosts Inseguro";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(fileContent);
        }
        catch
        {
            yield break;
        }

        if (!doc.RootElement.TryGetProperty("AllowedHosts", out var allowedHosts))
            yield break;

        var value = allowedHosts.GetString() ?? string.Empty;

        if (value.Trim() == "*")
        {
            yield return new Finding
            {
                RuleId = RuleId,
                Title = Title,
                Description = "AllowedHosts está configurado como '*', permitindo requisições de qualquer host. Isso pode facilitar ataques de DNS Rebinding.",
                FilePath = filePath,
                LineContent = $"\"AllowedHosts\": \"{value}\"",
                Severity = Severity.Warning,
                Suggestion = "Substitua '*' pelo domínio real da sua aplicação. Exemplo: \"AllowedHosts\": \"meusite.com;api.meusite.com\""
            };
        }
    }
}