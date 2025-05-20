//// Services/DocumentSubmissionService.cs
//using System;
//using System.IO;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Security.Cryptography;
//using System.Text;
//using System.Text.Json;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http.HttpResults;
//using Microsoft.Extensions.Configuration;

//namespace enInvBackEnd.Services
//{

//    public sealed class DocumentSubmissionService
//    {
//        private readonly HttpClient _http;
//        private string _endpoint=  "https://preprod-api.myinvois.hasil.gov.my/api/v1.0/documentsubmissions/";
//        private string _bearerToken = string.Empty;

//        public DocumentSubmissionService(HttpClient http, IConfiguration cfg)
//        {
//            _http = http;

//        }


//        public async Task<HttpResponseMessage> SubmitXmlAsync(string xmlFilePath,string codeNumber,Guid companyId)
//        {
//            var toakenManeger = new LhdnTokenManager();
//            _bearerToken = (await toakenManeger.GetOrCreateTokenAsync(companyId)).Token;



//            //return (new HttpResponseMessage());

//            if (!File.Exists(xmlFilePath))
//                throw new FileNotFoundException("Invoice XML not found.", xmlFilePath);

//            /* ---- base-64 the raw bytes ---- */
//            byte[] rawBytes = await File.ReadAllBytesAsync(xmlFilePath);
//            string base64Body = Convert.ToBase64String(rawBytes);

//            /* ---- SHA-256 of the base-64 string ---- */
//            using SHA256 sha = SHA256.Create();
//            string sha256 = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(base64Body))).Replace("-", "");

//            /* ---- build payload ---- */
//            var payload = new
//            {
//                documents = new[]
//                {
//                    new
//                    {
//                        format       = "XML",
//                        document     = base64Body,
//                        documentHash = sha256,
//                        codeNumber   = codeNumber
//                    }
//                }
//            };

//            string json = JsonSerializer.Serialize(payload);

//            /* ---- craft HTTP request ---- */
//            using var content = new StringContent(json, Encoding.UTF8, "application/json");

//            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
//            {
//                Content = content
//            };
//            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);

//            /* ---- send & return ---- */
//            return await _http.SendAsync(request);
//        }
//    }
//}
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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

        public async Task<HttpResponseMessage> SubmitXmlAsync(string xmlFilePath, string codeNumber, Guid companyId)
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
                        codeNumber = codeNumber
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

            // Send request and return response
            return await _http.SendAsync(request);
        }
    }
}

