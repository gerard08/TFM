using DetectorVulnerabilitatsDatabase.Models;
using System.Text;
using System.Text.Json;

namespace Worker.Services
{
    public class LocalAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocalAiService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Canviem al model Llama 3.2
        private const string ModelName = "llama3.2:3b";

        public LocalAiService(HttpClient httpClient, ILogger<LocalAiService> logger, IServiceScopeFactory scopeFactory)
        {
            _httpClient = httpClient;
            _logger = logger;
            // Augmentem el timeout per si la màquina va lenta
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _scopeFactory = scopeFactory;
        }

        public async Task<string> GenerateDescriptionAsync(string description)
        {
            // PROMPT HÍBRID:
            // Li demanem que busqui la versió, i si no, que dedueixi una solució genèrica.
            var promptText = $@"
                Role: Security Engineer.
                Task: Provide a 1-sentence description for the following vulnerability found using nuclei.
                
                DETECTION OUTPUT:
                {description}
                
                RULES:
                1. If the text mentions a fixed version (e.g., 'fixed in 1.2.3'), reply: 'Update [PROGRAM] to version [VERSION] or later.'
                2. If NO version is mentioned, use your knowledge to deduce the standard fix
                3. Keep it under 20 words. No filler words. Be imperative.
            ";

            return await SendRequestToAi(promptText);
        }

        public async Task<string> GenerateFixAsync(string cveId, string description, string software)
        {
            // PROMPT HÍBRID:
            // Li demanem que busqui la versió, i si no, que dedueixi una solució genèrica.
            var promptText = $@"
                Role: Security Engineer.
                Task: Provide a 1-sentence remediation instruction for the following vulnerability.
                
                VULNERABILITY DATA:
                - ID: {cveId}
                - Software: {software}
                - Details: {description}
                
                RULES:
                1. If the text mentions a fixed version (e.g., 'fixed in 1.2.3'), reply: 'Update {software} to version [VERSION] or later.'
                2. If NO version is mentioned, use your knowledge to deduce the standard fix (e.g., for SQLi -> 'Sanitize inputs using parameterized queries').
                3. Keep it under 20 words. No filler words. Be imperative.
            ";

            return await SendRequestToAi(promptText);
        }

        private async Task<string> SendRequestToAi(string promptText)
        {
            var payload = new
            {
                model = ModelName,
                prompt = promptText,
                stream = false, // Volem la resposta de cop
                options = new
                {
                    temperature = 0.2 // Baixa creativitat per ser més precís
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                // Connectem amb el servei del docker-compose
                // Si ho proves des de VS (fora de docker) usa "localhost" en lloc de "ollama-service"
                string ollamaUrl = Environment.GetEnvironmentVariable("OllamaUrl") ?? "http://localhost:11434";

                var response = await _httpClient.PostAsync($"{ollamaUrl}/api/generate", jsonContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Ollama returned {response.StatusCode}");
                    return "Check official vendor advisories.";
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);

                // Extraiem la resposta neta
                var responseText = doc.RootElement.GetProperty("response").GetString()?.Trim();

                return responseText ?? "Check vendor updates.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Local AI (Ollama).");
                return "Check official vendor advisories.";
            }
        }

        public async Task<string> WriteDescriptionWithAi(Findings finding)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LocalAiService>();
                return await context.GenerateDescriptionAsync(finding.Description);
            }
        }

        public async Task<string> FindSolutionWithAi(Findings finding)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<LocalAiService>();
                return await context.GenerateFixAsync(finding.Cve_id, finding.Description, finding.Affected_service);
            }
        }
    }
}