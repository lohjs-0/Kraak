using System.Text.Json;
using Kraak.Core.Models;

namespace Kraak.Core.Rules.AppSettings;

public class DebugModeRule : IRule
{
    public string RuleId => "KRK006";
    public string Title => "Debug Ativo em Produção";

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

        // Verifica Logging.LogLevel.Default como "Debug" ou "Trace"
        if (doc.RootElement.TryGetProperty("Logging", out var logging) &&
            logging.TryGetProperty("LogLevel", out var logLevel) &&
            logLevel.TryGetProperty("Default", out var defaultLevel))
        {
            var level = defaultLevel.GetString() ?? string.Empty;
            if (level is "Debug" or "Trace")
            {
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = $"O nível de log está configurado como '{level}'. Em produção isso pode expor informações sensíveis nos logs.",
                    FilePath = filePath,
                    LineContent = $"\"Default\": \"{level}\"",
                    Severity = Severity.Warning,
                    Suggestion = "Em produção use 'Warning' ou 'Error' como nível de log padrão para evitar exposição de dados sensíveis."
                };
            }
        }

        // Verifica se o ambiente está explicitamente como Development
        if (doc.RootElement.TryGetProperty("Environment", out var env))
        {
            var value = env.GetString() ?? string.Empty;
            if (value.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = "O ambiente está configurado como 'Development'. Nunca suba essa configuração para produção.",
                    FilePath = filePath,
                    LineContent = "\"Environment\": \"Development\"",
                    Severity = Severity.Critical,
                    Suggestion = "Nunca defina Environment como 'Development' em produção. Use variáveis de ambiente do servidor para isso."
                };
            }
        }
    }
}