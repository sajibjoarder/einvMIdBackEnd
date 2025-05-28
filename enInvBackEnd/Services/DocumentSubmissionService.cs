using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using enInvBackEnd.DataModels; // Assuming your model classes live here
using enInvBackEnd.DataContext;
namespace enInvBackEnd.Services
{
    public sealed class DocumentSubmissionService
    {
        private readonly HttpClient _http;
        private readonly string _endpoint = "https://preprod-api.myinvois.hasil.gov.my/api/v1.0/documentsubmissions/";
        private string _bearerToken = string.Empty;

        public DocumentSubmissionService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
        }

        public async Task<HttpResponseMessage> SubmitXmlAsync(string xmlFilePath, string codeNumber, Guid companyId, string Doctype, string? id)
        {
            // Get token
            var tokenManager = new LhdnTokenManager();
            var tokenEntity = await tokenManager.GetOrCreateTokenAsync(companyId);
            _bearerToken = tokenEntity.Token!;

            // Check file existence
            if (!File.Exists(xmlFilePath))
                throw new FileNotFoundException("Invoice XML not found.", xmlFilePath);

            // Read file contents (as string)
            string fileContents = await File.ReadAllTextAsync(xmlFilePath);

            // Convert to base64
            string base64Body = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContents));

            // Generate SHA256 hash (of raw XML content, not base64)
            string sha256;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(fileContents));
                sha256 = BitConverter.ToString(hashBytes).Replace("-", "");
            }
            var invoiceID = Guid.NewGuid(); // Generate a new GUID for the invoice ID
            // Prepare payload
            var payload = new
            {
                documents = new[]
                {
                    new
                    {
                        format = "XML",
                        document = base64Body,
                        documentHash = sha256,
                        codeNumber = invoiceID.ToString(), // Use the generated GUID as codeNumber
                    }
                }
            };

            // Serialize JSON
            string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Setup HTTP request
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);



            try
            {
                var response = await _http.SendAsync(request);

                using (EninvContext dbcontext = new EninvContext())
                {
                    Invoice invoice = new Invoice
                    {
                        Id = invoiceID,
                        CompanyId = companyId,
                        TimeSummitted = DateTime.Now,
                        Type = Doctype,
                        Ststus = response.IsSuccessStatusCode ? "Submitted" : "Failed",
                        ResposeCode = (int)response.StatusCode,
                        Path = xmlFilePath,
                        RespososeDetails = response.IsSuccessStatusCode ? "Submission successful" : await response.Content.ReadAsStringAsync(),
                        InvoiceId = id

                    };

                    dbcontext.Invoices.Add(invoice);
                    await dbcontext.SaveChangesAsync();
                    return response;
                }
            }catch (Exception ex)
            {

                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent($"Error: {ex.Message}")
                };
            }

        




          
        }
    }
}

