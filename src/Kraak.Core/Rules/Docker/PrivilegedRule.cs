using Kraak.Core.Models;

namespace Kraak.Core.Rules.Docker;

public class PrivilegedRule : IRule
{
    public string RuleId => "KRK010";
    public string Title => "Container Executando em Modo Privilegiado";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!IsDockerCompose(filePath)) yield break;

        var lines = fileContent.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Equals("privileged: true", StringComparison.OrdinalIgnoreCase))
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = "'privileged: true' concede acesso total ao host, eliminando o isolamento do container.",
                    FilePath = filePath,
                    LineContent = $"Linha {i + 1}: {lines[i].TrimEnd()}",
                    Severity = Severity.Critical,
                    Suggestion = "Remova 'privileged: true' e use 'cap_add' para conceder apenas as capabilities estritamente necessárias."
                };
        }
    }

    private static bool IsDockerCompose(string filePath)
    {
        var name = Path.GetFileName(filePath).ToLower();
        return name.Contains("docker-compose") || name.Contains("compose");
    }
}