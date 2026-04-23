using Microsoft.AspNetCore.Mvc;
using Kraak.Core;
using Kraak.Core.Models;
using Kraak.Core.Rules.AppSettings;
using Kraak.Core.Rules.DotEnv;
using Kraak.Core.Rules.Docker;

namespace Kraak.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanController : ControllerBase
{
    private static readonly Dictionary<string, string> _suggestions = new()
    {
        ["KRK001"] = "Use variáveis de ambiente ou o Secret Manager do .NET. Exemplo: Password=$env:DB_PASSWORD",
        ["KRK002"] = "Substitua '*' pelo domínio real. Exemplo: \"AllowedHosts\": \"meusite.com\"",
        ["KRK003"] = "Mova a chave para uma variável de ambiente. Nunca commite credenciais no código.",
        ["KRK004"] = "Crie um .gitignore e adicione '.env' nele. Exemplo: echo '.env' >> .gitignore",
        ["KRK005"] = "Habilite HTTPS em produção. Em Startup.cs use app.UseHttpsRedirection().",
        ["KRK006"] = "Use 'Warning' ou 'Error' como nível de log em produção. Nunca suba 'Development' para produção.",
        ["KRK007"] = "Use variáveis de ambiente do servidor ou um cofre como AWS Secrets Manager ou Azure Key Vault.",
        ["KRK008"] = "Use variáveis de ambiente do host ou Docker Secrets. Nunca coloque senhas direto no docker-compose.yml.",
    };

    [HttpPost]
    public IActionResult Scan([FromBody] ScanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Conteúdo não pode ser vazio.");

        var tempPath = Path.Combine(Path.GetTempPath(), request.FileName);
        System.IO.File.WriteAllText(tempPath, request.Content);

        var scanner = new Scanner();
        scanner.RegisterRule(new ConnStringRule());
        scanner.RegisterRule(new AllowedHostsRule());
        scanner.RegisterRule(new SecretsRule());
        scanner.RegisterRule(new EnvGitignoreRule());
        scanner.RegisterRule(new HttpsRule());
        scanner.RegisterRule(new DebugModeRule());
        scanner.RegisterRule(new EnvSecretsRule());
        scanner.RegisterRule(new DockerRule());

        var findings = scanner.Scan(tempPath).ToList();
        System.IO.File.Delete(tempPath);

        var score = CalculateScore(findings);

        var result = new
        {
            score,
            findings = findings.Select(f => new
            {
                f.RuleId,
                f.Title,
                f.Description,
                f.FilePath,
                f.LineContent,
                f.Severity,
                Suggestion = _suggestions.TryGetValue(f.RuleId, out var s) ? s : ""
            })
        };

        return Ok(result);
    }

    private static int CalculateScore(List<Finding> findings)
    {
        if (findings.Count == 0) return 100;
        var penalty = 0;
        foreach (var f in findings)
        {
            penalty += f.Severity switch
            {
                Severity.Critical => 25,
                Severity.Warning => 10,
                Severity.Info => 2,
                _ => 0
            };
        }
        return Math.Max(0, 100 - penalty);
    }
}

public record ScanRequest(string FileName, string Content);