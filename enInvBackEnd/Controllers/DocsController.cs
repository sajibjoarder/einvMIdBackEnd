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

        [HttpGet("single/docDetails")]
        public async Task<IActionResult> GetInvoiceByID([FromQuery] string invoiceId)
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

                return Ok(doc);
             
            }
        }


        [HttpGet("getInvs")]
        public async Task<IActionResult> SearchInvoicesONly(
        [FromQuery] string? invoiceId,
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        [FromQuery] string? status,
        [FromQuery] string? type,
        [FromQuery] string? companyId
            )
        {
            using (var _context = new EninvContext())
            {
                Guid companyGuid = Guid.Empty;
                if (string.IsNullOrWhiteSpace(companyId))
                    return BadRequest("companyId query parameter is required");
                else if (!Guid.TryParse(companyId, out companyGuid))
                    return BadRequest("companyId must be a valid GUID");


                var query = _context.Invoices.AsQueryable();

                if (!string.IsNullOrWhiteSpace(invoiceId))
                    query = query.Where(i => i.InvoiceId == invoiceId && i.CompanyId == companyGuid);

                if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromDt))
                    query = query.Where(i => i.TimeSummitted >= fromDt &&i.CompanyId == companyGuid);

                if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toDt))
                {
                    toDt = toDt.AddDays(1);
                    query = query.Where(i => i.TimeSummitted <= toDt && i.CompanyId == companyGuid);

                }
                  

                if (!string.IsNullOrWhiteSpace(status))
                    query = query.Where(i => i.Ststus == status && i.CompanyId == companyGuid);

                if (!string.IsNullOrWhiteSpace(type))
                    query = query.Where(i => i.Type == type && i.CompanyId == companyGuid);

                var invoices = await query.ToListAsync();
            
                return Ok(invoices);
            }
        }




        //[HttpGet("search")]
        //public async Task<IActionResult> SearchInvoices(
        //    [FromQuery] string? invoiceId,
        //    [FromQuery] string? fromDate,
        //    [FromQuery] string? toDate,
        //    [FromQuery] string? status,
        //    [FromQuery] string? type)
        //{
        //    using (var _context = new EninvContext())
        //    {
        //        var query = _context.Invoices.AsQueryable();

        //        if (!string.IsNullOrWhiteSpace(invoiceId))
        //            query = query.Where(i => i.InvoiceId == invoiceId);

        //        if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fromDt))
        //            query = query.Where(i => i.TimeSummitted >= fromDt);

        //        if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var toDt))
        //            query = query.Where(i => i.TimeSummitted <= toDt);

        //        if (!string.IsNullOrWhiteSpace(status))
        //            query = query.Where(i => i.Ststus == status);

        //        if (!string.IsNullOrWhiteSpace(type))
        //            query = query.Where(i => i.Type == type);

        //        var invoices = await query.ToListAsync();
        //        var results = new List<object>();

        //        foreach (var invoice in invoices)
        //        {
        //            var absolutePath = Path.GetFullPath(invoice.Path);
        //            if (!string.IsNullOrWhiteSpace(absolutePath) && System.IO.File.Exists(absolutePath))
        //            {
        //                try
        //                {
        //                    var xmlContent = await System.IO.File.ReadAllTextAsync(absolutePath);
        //                    var xmlDoc = new XmlDocument();
        //                    xmlDoc.LoadXml(xmlContent);
        //                    string json = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented, true);

        //                    var deserializedObject = JsonConvert.DeserializeObject(json);
        //                    if (deserializedObject != null)
        //                    {
        //                        results.Add(deserializedObject);
        //                    }
        //                }
        //                catch
        //                {
        //                    // Optionally log or handle file read errors per invoice
        //                }
        //            } 
        //        }

        //        return Ok(results);
        //    }
        //}
    }
}
