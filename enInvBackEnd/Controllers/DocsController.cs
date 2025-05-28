using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml;
using Newtonsoft.Json;
using System.IO;

namespace enInvBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocsController : ControllerBase
    {
        [HttpGet("single")]
        public async Task<IActionResult> GetInvoiceByInvoiceId([FromQuery] string invoiceId)
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
                return BadRequest("invoiceId query parameter is required");

            using (var _context = new EninvContext())
            {
                var doc = await _context.Invoices.Where(i => i.InvoiceId == invoiceId).FirstOrDefaultAsync();
                if (doc == null)
                {
                    return NotFound($"No invoice found with InvoiceId: {invoiceId}");
                }

                if (string.IsNullOrWhiteSpace(doc.Path) || !System.IO.File.Exists(doc.Path))
                {
                    return NotFound("Invoice XML file not found at the specified path.");
                }

                try
                {
                    var xmlContent = await System.IO.File.ReadAllTextAsync(doc.Path);
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);
                    string json = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented, true);
                    return Content(json, "application/json");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error processing XML file: {ex.Message}");
                }
            }
        }
    }
}
