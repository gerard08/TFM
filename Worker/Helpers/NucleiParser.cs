using DetectorVulnerabilitatsDatabase.Models;
using Worker.Services;
using System.Text.Json.Nodes;

namespace Worker.Helpers
{
    public static class NucleiParser
    {
        public static List<Findings> Parse(string jsonOutput)
        {
            var findings = new List<Findings>();
            if (string.IsNullOrWhiteSpace(jsonOutput)) return findings;

            // 1. DIVIDIM PER LÍNIES (Perquè és JSONL, no un Array JSON)
            var lines = jsonOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                try
                {
                    // Ignorem línies que no semblen JSON (per si es cola algun log)
                    if (!line.Trim().StartsWith("{")) continue;

                    var item = JsonNode.Parse(line);
                    if (item == null) continue;

                    // 2. EXTRACCIÓ DE DADES
                    var info = item["info"];
                    string name = info?["name"]?.ToString() ?? "Unknown Vuln";
                    string severity = info?["severity"]?.ToString() ?? "info";
                    string description = info?["description"]?.ToString() ?? "";
                    
                    // Extracció segura del Host/IP
                    string host = item["host"]?.ToString() ?? item["ip"]?.ToString() ?? "Target";
                    string port = item["port"]?.ToString() ?? "";
                    string affectedService = string.IsNullOrEmpty(port) ? host : $"{host}:{port}";

                    // 3. EXTRACCIÓ DE CVEs (Pot ser null, string o array)
                    string cveId = "N/A";
                    var classification = info?["classification"];

                    if (classification != null)
                    {
                        var cveNode = classification["cve-id"];
                        if (cveNode is JsonArray cveArray && cveArray.Count > 0)
                        {
                            cveId = cveArray[0].ToString(); // Agafem el primer CVE
                        }
                        else if (cveNode != null)
                        {
                            cveId = cveNode.ToString();
                        }
                    }

                    // 4. NETEJA DE LA DESCRIPCIÓ (Per evitar errors de DB amb textos gegants com el de Redis)
                    // Si la resposta és molt gran (com el log de Redis), la trunquem o la posem al final.
                    string responseSnippet = item["response"]?.ToString() ?? "";
                    if (responseSnippet.Length > 500) responseSnippet = responseSnippet.Substring(0, 500) + "... [TRUNCATED]";

                    string fullDescription = $"{description}\n\n**Matcher:** {item["matcher-name"]}\n**Evidence:**\n{responseSnippet}";

                    findings.Add(new Findings
                    {
                        Title = $"[Nuclei] {name}",
                        Severity = MapSeverity(severity),
                        Cve_id = cveId,
                        Affected_service = affectedService,
                        Description = fullDescription,
                        Created_at = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Error parsejant línia de Nuclei: {ex.Message}");
                    // Continuem amb la següent línia, no volem que un error peti tot l'scan
                }
            }

            return findings;
        }
        

        private static string MapSeverity(string text)
        {
            return text.ToLower() switch
            {
                "critical" => "10.0",
                "high" => "8.0",
                "medium" => "5.0",
                "low" => "2.0",
                _ => "0.0" // Info o unknown
            };
        }
    }
}