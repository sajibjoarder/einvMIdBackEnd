using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using enInvBackEnd.DataContext;
using enInvBackEnd.DataModels;
using System;
using System.Threading.Tasks;

namespace enInvBackEnd.Controllers
{
    [ApiController]
    [Route("api/cancel_doc")]
    public class Cancel_docController : ControllerBase
    {
        private const string LhdnApiBase = "https://preprod-api.myinvois.hasil.gov.my/";

        [HttpPost("{docId:guid}")]
        public async Task<IActionResult> CancelDocument(Guid docId)
        {
            using var dbcontext = new EninvContext();
            var invoice = await dbcontext.Invoices.FindAsync(docId);
            if (invoice == null)
                return NotFound(new { message = "Invoice not found" });
            if (string.Equals(invoice.Ststus, "cancelled", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Invoice already cancelled" });
            if (string.IsNullOrWhiteSpace(invoice.SubmissionId))
                return BadRequest(new { message = "No SubmissionId found for this invoice." });
            if (!invoice.CompanyId.HasValue)
                return BadRequest(new { message = "No CompanyId found for this invoice." });

            // Get LHDN access token (from your LhdnTokenManager)
            var tokenManager = new LhdnTokenManager();
            var tokenRecord = await tokenManager.GetOrCreateTokenAsync(invoice.CompanyId.Value);
            string accessToken = tokenRecord.Token;

            var payload = new
            {
                status = "cancelled",
                reason = "Cancellation requested by user"
            };
            string jsonPayload = JsonSerializer.Serialize(payload);

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(LhdnApiBase);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var putRequest = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/v1.0/documents/state/{invoice.DocId}/state"
            )
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            var response = await httpClient.SendAsync(putRequest);
            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                invoice.Ststus = "cancelled";
                await dbcontext.SaveChangesAsync();
                return Ok(JsonDocument.Parse(responseBody).RootElement);
            }
            else
            {
                return StatusCode((int)response.StatusCode, new { error = responseBody });
            }
        }
    }
}
