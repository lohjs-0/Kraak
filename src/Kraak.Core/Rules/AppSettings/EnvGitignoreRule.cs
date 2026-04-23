using Kraak.Core.Models;

namespace Kraak.Core.Rules.DotEnv;

public class EnvGitignoreRule : IRule
{
    public string RuleId => "KRK004";
    public string Title => "Arquivo .env não protegido pelo .gitignore";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
    {
        var directory = Path.GetDirectoryName(filePath) ?? ".";
        var gitignorePath = Path.Combine(directory, ".gitignore");

        if (!File.Exists(gitignorePath))
        {
            yield return new Finding
            {
                RuleId = RuleId,
                Title = Title,
                Description = "Nenhum arquivo .gitignore encontrado. O arquivo .env pode ser commitado acidentalmente.",
                FilePath = filePath,
                LineContent = "Crie um .gitignore com '.env' listado.",
                Severity = Severity.Warning,
                Suggestion = "Crie um arquivo .gitignore na raiz do projeto e adicione '.env' nele."
            };
            yield break;
        }

        var gitignoreContent = File.ReadAllText(gitignorePath);
        var lines = gitignoreContent.Split('\n')
            .Select(l => l.Trim())
            .ToList();

        var isProtected = lines.Any(l =>
            l == ".env" ||
            l == ".env*" ||
            l == "*.env" ||
            l == ".env.local" ||
            l == ".env.production"
        );

        if (!isProtected)
        {
            yield return new Finding
            {
                RuleId = RuleId,
                Title = Title,
                Description = "O .gitignore existe mas não protege o arquivo .env.",
                FilePath = gitignorePath,
                LineContent = "Adicione '.env' ao seu .gitignore.",
                Severity = Severity.Critical,
                Suggestion = "Adicione '.env' ao seu .gitignore. Exemplo: echo '.env' >> .gitignore"
            };
        }
    }
}