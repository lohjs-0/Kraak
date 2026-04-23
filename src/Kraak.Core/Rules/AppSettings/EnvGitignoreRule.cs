using Kraak.Core.Models;

namespace Kraak.Core.Rules.DotEnv;

public class EnvGitignoreRule : IRule
{
    public string RuleId => "KRK004";
    public string Title => "Arquivo .env não protegido pelo .gitignore";

    public IEnumerable<Finding> Analyze(string filePath, string fileContent)
        => [];

    public IEnumerable<Finding> AnalyzeAll(IReadOnlyList<(string FilePath, string Content)> files)
    {
        var envFiles = files
            .Where(f => Path.GetFileName(f.FilePath).StartsWith(".env", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (envFiles.Count == 0) yield break;

        var gitignoreFile = files.FirstOrDefault(f =>
            Path.GetFileName(f.FilePath).Equals(".gitignore", StringComparison.OrdinalIgnoreCase));

        // Sem .gitignore no lote
        if (gitignoreFile == default)
        {
            foreach (var env in envFiles)
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = $"Nenhum .gitignore encontrado no lote. O arquivo '{Path.GetFileName(env.FilePath)}' pode ser commitado acidentalmente.",
                    FilePath = env.FilePath,
                    LineContent = "Adicione um .gitignore com '.env' listado.",
                    Severity = Severity.Warning,
                    Suggestion = "Inclua o .gitignore na análise junto com o .env, ou crie um com '.env' listado."
                };
            yield break;
        }

        // Tem .gitignore — verifica se protege cada .env
        var gitignoreLines = gitignoreFile.Content
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith('#'))
            .ToList();

        // Verifica negações perigosas: !.env
        foreach (var line in gitignoreLines.Where(l => l.StartsWith('!')))
        {
            var negated = line[1..];
            if (negated.StartsWith(".env", StringComparison.OrdinalIgnoreCase))
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = "Negação Perigosa no .gitignore",
                    Description = $"A regra '{line}' no .gitignore força o commit de arquivos .env, anulando qualquer proteção.",
                    FilePath = gitignoreFile.FilePath,
                    LineContent = line,
                    Severity = Severity.Critical,
                    Suggestion = $"Remova a linha '{line}' do .gitignore imediatamente."
                };
        }

        var protectedPatterns = new[] { ".env", ".env*", "*.env", ".env.local", ".env.production", ".env.development" };
        var isProtected = gitignoreLines.Any(l => protectedPatterns.Contains(l));

        foreach (var env in envFiles)
        {
            var fileName = Path.GetFileName(env.FilePath);
            var isFileProtected = isProtected || gitignoreLines.Contains(fileName);

            if (!isFileProtected)
                yield return new Finding
                {
                    RuleId = RuleId,
                    Title = Title,
                    Description = $"O .gitignore não protege '{fileName}'. Ele pode ser commitado acidentalmente.",
                    FilePath = gitignoreFile.FilePath,
                    LineContent = $"'{fileName}' não encontrado no .gitignore.",
                    Severity = Severity.Critical,
                    Suggestion = $"Adicione '{fileName}' ou '.env*' ao seu .gitignore."
                };
        }
    }
}