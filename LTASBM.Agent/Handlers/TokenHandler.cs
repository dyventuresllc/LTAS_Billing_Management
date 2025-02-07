using Relativity.API;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LTASBM.Agent.Handlers
{
    public class TokenHandler
    {
        private readonly HttpClient _httpClient;
        private readonly IAPILog _logger;               

        public class TokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("scope")]
            public string Scope { get; set; }
        }

        public TokenHandler(IAPILog logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string instanceUrl)
        {
            try
            {
                var tokenUrl = $"{instanceUrl}/Identity/connect/token";
                _logger.LogInformation($"Requesting new token from {tokenUrl}");

                var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "scope", "SystemUserInfo" },
                { "grant_type", "client_credentials" }
            };

                var content = new FormUrlEncodedContent(tokenRequest);
                var response = await _httpClient.PostAsync(tokenUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Failed to get token. Status: {response.StatusCode}, Error: {errorContent}");
                    return string.Empty;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

                if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                {
                    _logger.LogError("Received empty access token");
                    return string.Empty;
                }

                _logger.LogInformation("Successfully obtained access token");
                return tokenResponse.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access token");
                return string.Empty;
            }
        }

    }
}
