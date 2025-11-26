using System.Text.Json;
using Microsoft.Extensions.Configuration; // Necessari per llegir el JSON

namespace Worker.Helpers
{
    public static class CveHelper
    {
        private static readonly string? _apiKey;
        private static readonly TimeSpan _minInterval;
        private static readonly HttpClient _client = new HttpClient();

        // Memòria cau simple
        private static readonly Dictionary<string, string> _descriptionCache = new();

        // Variable per controlar l'última petició (Rate Limiting)
        private static DateTime _lastRequestTime = DateTime.MinValue;

        // -------------------------------------------------------------
        // CONSTRUCTOR ESTÀTIC: S'executa una sola vegada automàticament
        // -------------------------------------------------------------
        static CveHelper()
        {
            // Construïm la configuració manualment per llegir Secrets.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) // Busca a la carpeta de l'executable
                .AddJsonFile("Secrets.json", optional: true);

            var config = builder.Build();

            _apiKey = config["NistApiKey"];

            // Si tenim clau, esperem 0.6s. Si no, 6.0s
            _minInterval = string.IsNullOrEmpty(_apiKey)
                ? TimeSpan.FromSeconds(6)
                : TimeSpan.FromMilliseconds(600);
        }

        public static async Task<string> GetCveDescriptionAsync(string cveId)
        {
            if (_descriptionCache.TryGetValue(cveId, out var cachedDescription))
            {
                return cachedDescription;
            }

            // --- RATE LIMITING INTEL·LIGENT ---
            var timeSinceLast = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLast < _minInterval)
            {
                await Task.Delay(_minInterval - timeSinceLast);
            }
            // ----------------------------------

            try
            {
                _client.DefaultRequestHeaders.UserAgent.ParseAdd("MyVulnerabilityScanner/1.0");

                // AFEGIM LA CLAU SI EXISTEIX
                if (!string.IsNullOrEmpty(_apiKey) && !_client.DefaultRequestHeaders.Contains("apiKey"))
                {
                    _client.DefaultRequestHeaders.Add("apiKey", _apiKey);
                }

                var url = $"https://services.nvd.nist.gov/rest/json/cves/2.0?cveId={cveId}";

                // Actualitzem l'hora de l'última petició JUST abans de fer-la
                _lastRequestTime = DateTime.UtcNow;

                var response = await _client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    // Gestió bàsica d'errors de l'API (403, 429, 503)
                    if ((int)response.StatusCode == 403 || (int)response.StatusCode == 429)
                        return "Rate limit exceeded or Invalid Key. Pausing.";

                    return $"Error retrieving description: {response.StatusCode}";
                }

                var jsonString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("vulnerabilities", out var vulns) && vulns.GetArrayLength() > 0)
                {
                    var cveNode = vulns[0].GetProperty("cve");
                    if (cveNode.TryGetProperty("descriptions", out var descriptions))
                    {
                        foreach (var desc in descriptions.EnumerateArray())
                        {
                            if (desc.GetProperty("lang").GetString() == "en")
                            {
                                var text = desc.GetProperty("value").GetString();
                                _descriptionCache[cveId] = text;
                                return text;
                            }
                        }
                    }
                }

                return "No description found in NVD database.";
            }
            catch (Exception ex)
            {
                return $"Exception fetching CVE: {ex.Message}";
            }
        }
    }
}