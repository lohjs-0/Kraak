using Microsoft.AspNetCore.Mvc;
using Kraak.Core;
using Kraak.Core.Models;

namespace Kraak.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DriftController : ControllerBase
{
    [HttpPost]
    public IActionResult Compare([FromBody] DriftRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OldContent) || string.IsNullOrWhiteSpace(request.NewContent))
            return BadRequest("Conteúdo não pode ser vazio.");

        var oldPath = Path.Combine(Path.GetTempPath(), $"old_{request.FileName}");
        var newPath = Path.Combine(Path.GetTempPath(), $"new_{request.FileName}");

        System.IO.File.WriteAllText(oldPath, request.OldContent);
        System.IO.File.WriteAllText(newPath, request.NewContent);

        try
        {
            var findings = DriftDetector.Compare(
                oldPath, request.OldContent,
                newPath, request.NewContent
            ).Select(f => new
            {
                f.RuleId,
                f.Title,
                f.Description,
                f.FilePath,
                f.LineContent,
                f.Severity,
                f.Suggestion
            });

            return Ok(findings);
        }
        finally
        {
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            if (System.IO.File.Exists(newPath)) System.IO.File.Delete(newPath);
        }
    }
}

public record DriftRequest(string FileName, string OldContent, string NewContent);
