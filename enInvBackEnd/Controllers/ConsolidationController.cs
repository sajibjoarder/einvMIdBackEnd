// Controllers/ConsolidationController.cs
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using enInvBackEnd.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace enInvBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]                      // ← api/consolidation
    public class ConsolidationController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly DocumentSubmissionService _submissionSvc;

        public ConsolidationController(IWebHostEnvironment env, DocumentSubmissionService submissionSvc)
        {
            _env = env;
            _submissionSvc = submissionSvc;
        }

        // POST: api/consolidation/consolidationSubmit/{company_id}
        [HttpPost("consolidationSubmit/{company_id}")]
        public async Task<IActionResult> CreateConsolidation(Guid company_id)
        {
            // 1. Locate your sample XML under <ContentRoot>/samples/Invoice-Sample.xml
            var sampleDir = Path.Combine(_env.ContentRootPath, "samples");
            var sampleFile = Path.Combine(sampleDir, "Invoice-Sample.xml");
            if (!System.IO.File.Exists(sampleFile))
                return NotFound($"Sample invoice not found at '{sampleFile}'.");

            // 2. Ensure the target invoicess directory exists
            var invoicesDir = Path.Combine(_env.ContentRootPath, "invoicess");
            Directory.CreateDirectory(invoicesDir);

            // 3. Copy to a new, unique filename
            var newFileName = $"CONSOL-INV-{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.xml";
            var destPath = Path.Combine(invoicesDir, newFileName);
            System.IO.File.Copy(sampleFile, destPath);

            // 4. Submit via your existing DocumentSubmissionService
            HttpResponseMessage resp = await _submissionSvc.SubmitXmlAsync(
                destPath,
                "142250926443",   // document reference ID, adjust if needed
                company_id
            );
            var respBody = await resp.Content.ReadAsStringAsync();

            // 5. Return 201 Created with file info and submission response
            return Created(string.Empty, new
            {
                fileName = newFileName,
                fullPath = destPath,
                submissionResponse = respBody
            });
        }
    }
}
