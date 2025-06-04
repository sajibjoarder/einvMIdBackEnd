using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

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

            await using var context = new EninvContext();

            // ♦ 1.  Grab the row and map to DTO
            var doc = await context.Invoices
                                       .AsNoTracking()
                                       .Where(i => i.InvoiceId == invoiceId)
                                       .FirstOrDefaultAsync();

            if (doc == null)
                return NotFound($"No invoice found with InvoiceId: {invoiceId}");

            if (string.IsNullOrWhiteSpace(doc.Path) || !System.IO.File.Exists(doc.Path))
                return NotFound("Invoice XML file not found at the specified path.");

            // ♦ 2.  Convert the XML to a JObject (or keep as string)
            var xmlContent = await System.IO.File.ReadAllTextAsync(doc.Path);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            string xmlJson = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented, true);

            // ♦ 3.  Shape the outgoing object
            var response = new
            {
                doc = new 
                {
                    Id = doc.Id,
                    TimeSubmitted = doc.TimeSummitted,   // keep original casing if you like
                    Type = doc.Type,
                    Status = doc.Ststus,
                    ResponseDetails = doc.RespososeDetails,
                    ResponseCode = doc.ResposeCode,
                    CompanyId = doc.CompanyId,
                    InvoiceId = doc.InvoiceId,
                    SubmissionId = doc.SubmissionId,
                    DocId = doc.DocId
                },
                Xml = xmlJson
            };

            return Ok(response);       // ASP.NET Core serialises to JSON automatically
        }



        [HttpGet("getInvs")]
        public async Task<IActionResult> SearchInvoicesONly(
        [FromQuery] string? invoiceId,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] string? status,
        [FromQuery] string? type)
        {
            using (var _context = new EninvContext())
            {
                var query = _context.Invoices.AsQueryable();

                if (!string.IsNullOrWhiteSpace(invoiceId))
                    query = query.Where(i => i.InvoiceId == invoiceId);

                if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromDt))
                    query = query.Where(i => i.TimeSummitted >= fromDt);

                if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toDt))
                {
                    toDt = toDt.AddDays(1);
                    query = query.Where(i => i.TimeSummitted <= toDt);

                }
                  

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(i => i.Ststus == status);

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(i => i.Type == type);

                var invoices = await query.ToListAsync();
            
                return Ok(invoices);
            }
        }




        [HttpGet("search")]
        public async Task<IActionResult> SearchInvoices(
            [FromQuery] string? invoiceId,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? status,
            [FromQuery] string? type)
        {
            using (var _context = new EninvContext())
            {
                var query = _context.Invoices.AsQueryable();

                if (!string.IsNullOrWhiteSpace(invoiceId))
                    query = query.Where(i => i.InvoiceId == invoiceId);

                if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromDt))
                    query = query.Where(i => i.TimeSummitted >= fromDt);

                if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toDt))
                {
                    toDt = toDt.AddDays(1);
                    query = query.Where(i => i.TimeSummitted <= toDt);
                }
                    

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(i => i.Ststus == status);

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(i => i.Type == type);

                var invoices = await query.ToListAsync();
                var results = new List<object>();

                foreach (var invoice in invoices)
                {
                    var absolutePath = Path.GetFullPath(invoice.Path);
                    if (!string.IsNullOrWhiteSpace(absolutePath) && System.IO.File.Exists(absolutePath))
                    {
                        try
                        {
                            var xmlContent = await System.IO.File.ReadAllTextAsync(absolutePath);
                            var xmlDoc = new XmlDocument();
                            xmlDoc.LoadXml(xmlContent);
                            string json = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented, true);

                            var deserializedObject = JsonConvert.DeserializeObject(json);
                            if (deserializedObject != null)
                            {
                                results.Add(deserializedObject);
                            }
                        }
                        catch
                        {
                            // Optionally log or handle file read errors per invoice
                        }
                    } 
                }

                return Ok(results);
            }
        }
    }
}
