using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.AppSettings;

public class ConnStringRule : IRule
{
    public string RuleId => "KRK001";
    public string Title => "Connection String Exposta";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        var findings = new List<Finding>();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(fileContent);
        }
        catch
        {
            yield break;
        }

        if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings))
            yield break;

        foreach (var prop in connStrings.EnumerateObject())
        {
            var value = prop.Value.GetString() ?? string.Empty;

            if (value.Contains("Password=", StringComparison.OrdinalIgnoreCase) ||
                value.Contains("Pwd=", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = $"A connection string '{prop.Name}' contém uma senha em texto puro.",
                    FilePath = filePath,
                    LineContent = value,
                    Severity = Severity.Critical,
                    Suggestion = "Use variáveis de ambiente ou o Secret Manager do .NET para armazenar senhas. Exemplo: Server=localhost;Database=mydb;User Id=user;Password=$env:DB_PASSWORD"
                });
            }
        }

        foreach (var finding in findings)
            yield return finding;
    }
}