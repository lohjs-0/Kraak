using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.AppSettings;

public class HttpsRule : IRule
{
    public string RuleId => "KRK005";
    public string Title => "HTTPS Desabilitado";

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

        if (!doc.RootElement.TryGetProperty("HttpsRedirection", out var httpsRedirection))
            yield break;

        if (httpsRedirection.TryGetProperty("Enabled", out var enabled) &&
            enabled.ValueKind == JsonValueKind.False)
        {
            yield return new Finding
            {
                RuleId = RuleId,
                Title = Title,
                Description = "O redirecionamento HTTPS está desabilitado. Isso expõe a aplicação a ataques de interceptação (Man-in-the-Middle).",
                FilePath = filePath,
                LineContent = "\"HttpsRedirection\": { \"Enabled\": false }",
                Severity = Severity.Critical,
                Suggestion = "Configure HttpsRedirection como true em produção. Em Startup.cs use app.UseHttpsRedirection()."
            };
        }
    }
}