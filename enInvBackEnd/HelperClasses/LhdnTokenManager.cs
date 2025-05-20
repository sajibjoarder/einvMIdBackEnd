using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using enInvBackEnd.DataContext; // Adjust based on your project
using enInvBackEnd.DataModels;       // Assuming your model classes live here

public class LhdnTokenManager
{
    private const string TokenEndpoint = "https://preprod-api.myinvois.hasil.gov.my/connect/token";

    public async Task<LhdnToken> GetOrCreateTokenAsync(Guid companyId)
    {
        // Step 1: Check for existing, valid token
        using (var context = new EninvContext())
        {
            var now = DateTime.Now;

            var existingToken = await context.LhdnTokens
                .AsNoTracking()
                .Where(t => t.CompanyId == companyId && t.ExpieryDateTime != null && t.ExpieryDateTime > now)
                .FirstOrDefaultAsync();

           // Console.WriteLine($"Existing token: {existingToken?.ExpieryDateTime} current time: {DateTime.Now}");

            if (existingToken != null)
                return existingToken;
        }

        // Step 2: Retrieve credentials for the company
        string clientId, clientSecret;
        using (var context = new EninvContext())
        {
            var lhdnProfile = await context.LhdnProfiles.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (lhdnProfile == null)
            {
                throw new Exception("Lhdn Profile found.");
            }
          

            clientId = lhdnProfile.ClientIdLhdn ?? throw new Exception("Client ID LHDN is missing.");
            clientSecret = lhdnProfile.ClientSecretLhdn ?? throw new Exception("Client Secret LHDN is missing.");
        }

        // Step 3: Request token from LHDN
        string accessToken;
        int expiresIn;

        using (var httpClient = new HttpClient())
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "client_credentials" },
                { "scope", "InvoicingAPI" }
            });

            var response = await httpClient.PostAsync(TokenEndpoint, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        }

        // Step 4: Store the token in the database
        var newToken = new LhdnToken
        {
            TokenId = Guid.NewGuid(),
            CompanyId = companyId,
            IssueddateTime = DateTime.Now,
            ExpieryDateTime = DateTime.Now.AddSeconds(expiresIn),
            Token = accessToken
        };

        using (var context = new EninvContext())
        {
            context.LhdnTokens.Add(newToken);
            await context.SaveChangesAsync();
        }

        return newToken;
    }
}
