using Kraak.Core.Models;

namespace Kraak.Core.Rules.Docker;

public class RunAsRootRule : IRule
{
    public string RuleId => "KRK011";
    public string Title => "Container Rodando como Root";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        if (!IsDockerCompose(filePath)) yield break;

        var lines = fileContent.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (!trimmed.StartsWith("user:", StringComparison.OrdinalIgnoreCase)) continue;

            var value = trimmed["user:".Length..].Trim().Trim('"');
            if (value is "root" or "0" or "0:0")
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = $"Container configurado com usuário '{value}' (root). Isso aumenta o risco em caso de escape do container.",
                    FilePath = filePath,
                    LineContent = $"Linha {i + 1}: {lines[i].TrimEnd()}",
                    Severity = Severity.Critical,
                    Suggestion = "Defina um usuário não-privilegiado no Dockerfile com 'USER appuser' ou use 'user: 1000:1000' no compose."
                };
        }
    }

    private static bool IsDockerCompose(string filePath)
    {
        var name = Path.GetFileName(filePath).ToLower();
        return name.Contains("docker-compose") || name.Contains("compose");
    }
}