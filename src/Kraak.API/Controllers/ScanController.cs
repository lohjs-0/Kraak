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
        ["KRK009"] = "Esta string tem alta entropia e pode ser um secret. Mova para variáveis de ambiente se for uma chave ou senha.",
        ["KRK010"] = "Remova 'privileged: true' e use 'cap_add' para conceder apenas as capabilities estritamente necessárias.",
        ["KRK011"] = "Defina um usuário não-privilegiado no Dockerfile com 'USER appuser' ou use 'user: 1000:1000' no compose.",
        ["KRK012"] = "Avalie se a capability é realmente necessária e remova ou substitua pelo menor privilégio possível.",
        ["KRK013"] = "Remova 'network_mode: host' e use redes bridge nomeadas. Exponha apenas as portas necessárias via 'ports'.",
        ["KRK014"] = "Remova a exposição da porta ao host e acesse o serviço via rede interna do Docker.",
    };

    private static Scanner BuildScanner()
    {
        var scanner = new Scanner();

        // AppSettings
        scanner.RegisterRule(new ConnStringRule());
        scanner.RegisterRule(new AllowedHostsRule());
        scanner.RegisterRule(new SecretsRule());
        scanner.RegisterRule(new HttpsRule());
        scanner.RegisterRule(new DebugModeRule());
        scanner.RegisterRule(new EnvSecretsRule());
        scanner.RegisterRule(new EntropyRule());

        // DotEnv
        scanner.RegisterRule(new EnvGitignoreRule());

        // Docker
        scanner.RegisterRule(new DockerRule());
        scanner.RegisterRule(new PrivilegedRule());
        scanner.RegisterRule(new RunAsRootRule());
        scanner.RegisterRule(new CapAddRule());
        scanner.RegisterRule(new HostNetworkRule());
        scanner.RegisterRule(new PortExposureRule());

        return scanner;
    }

    [HttpPost]
    public IActionResult Scan([FromBody] ScanRequest[] requests)
    {
        if (requests.Length == 0)
            return BadRequest("Nenhum arquivo enviado.");

        var tempFiles = requests.Select(r =>
        {
            var path = Path.Combine(Path.GetTempPath(), r.FileName);
            System.IO.File.WriteAllText(path, r.Content);
            return (FilePath: path, Content: r.Content);
        }).ToList();

        try
        {
            var scanner = BuildScanner();
            var findings = scanner.ScanAll(tempFiles).ToList();
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
        finally
        {
            foreach (var (path, _) in tempFiles)
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
        }
    }

    private static int CalculateScore(List<Finding> findings)
    {
        if (findings.Count == 0) return 100;

        var penalty = findings.Sum(f => f.Severity switch
        {
            Severity.Critical => 25,
            Severity.Warning  => 10,
            Severity.Info     => 2,
            _                 => 0
        });

        return Math.Max(0, 100 - penalty);
    }
}

public record ScanRequest(string FileName, string Content);
