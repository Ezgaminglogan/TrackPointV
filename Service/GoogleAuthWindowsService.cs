using Microsoft.Web.WebView2.Core;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;

namespace TrackPointV.Service
{
    public class GoogleAuthWindowsService
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string[] _scopes;
        private string _codeVerifier;
        
        public GoogleAuthWindowsService(string clientId, string clientSecret = null)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _scopes = new[] { "openid", "profile", "email" };
        }

        private string GenerateCodeVerifier()
        {
            var bytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public (string authorizationUrl, string state) GetAuthorizationUrl()
        {
            _codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(_codeVerifier);
            var state = Guid.NewGuid().ToString("N");

            var queryParams = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "redirect_uri", "http://localhost" },
                { "response_type", "code" },
                { "scope", string.Join(" ", _scopes) },
                { "state", state },
                { "code_challenge", codeChallenge },
                { "code_challenge_method", "S256" },
                { "prompt", "select_account" }
            };

            var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            return ($"https://accounts.google.com/o/oauth2/v2/auth?{queryString}", state);
        }

        public async Task<(string idToken, string accessToken)> ExchangeCodeForTokensAsync(string code)
        {
            using var httpClient = new HttpClient();
            var tokenParams = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "redirect_uri", "http://localhost" },
                { "code", code },
                { "code_verifier", _codeVerifier },
                { "grant_type", "authorization_code" }
            };
            
            // Add client_secret if available
            if (!string.IsNullOrEmpty(_clientSecret))
            {
                tokenParams.Add("client_secret", _clientSecret);
            }
            
            var content = new FormUrlEncodedContent(tokenParams);

            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Token exchange failed: {error}");
            }

            var tokenResponse = await JsonSerializer.DeserializeAsync<JsonElement>(
                await response.Content.ReadAsStreamAsync());

            return (
                tokenResponse.GetProperty("id_token").GetString(),
                tokenResponse.GetProperty("access_token").GetString()
            );
        }

        public GoogleUser ParseIdToken(string idToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(idToken);

            return new GoogleUser
            {
                Id = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                Email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                Name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value,
                GivenName = token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
                FamilyName = token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value,
                Picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value
            };
        }
    }

    public class GoogleUser
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Picture { get; set; }
    }
}
