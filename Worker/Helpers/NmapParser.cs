using DetectorVulnerabilitatsDatabase.Models;
using System.Xml.Linq;
using Worker.Models;

namespace Worker.Helpers
{
    public static class NmapParser
    {

        public static List<NetworkService> ParseNmapXmlToServices(string xmlContent)
        {
            var services = new List<NetworkService>();

            try
            {
                var doc = XDocument.Parse(xmlContent);

                // Busquem tots els elements <port>
                var ports = doc.Descendants("port");

                foreach (var port in ports)
                {
                    // Només ens interessen els ports que estan "open"
                    var state = port.Element("state")?.Attribute("state")?.Value;
                    if (state != "open") continue;

                    int portId = int.Parse(port.Attribute("portid")?.Value ?? "0");
                    string protocol = port.Attribute("protocol")?.Value ?? "tcp";

                    // Extraiem la info del servei (nmap -sV)
                    var serviceElement = port.Element("service");
                    string serviceName = serviceElement?.Attribute("name")?.Value ?? "unknown";
                    string product = serviceElement?.Attribute("product")?.Value ?? "";
                    string version = serviceElement?.Attribute("version")?.Value ?? "";

                    services.Add(new NetworkService(portId, protocol, serviceName, product, version));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error Parsing Nmap XML]: {ex.Message}");
                // En cas d'error, retornem llista buida o parcial
            }

            return services;
        }

        public static List<int> ParseNmapXmlPorts(string xmlContent)
        {
            var openPorts = new List<int>();

            try
            {
                // A vegades Nmap pot deixar text brut abans de l'XML si es barreja stdout/stderr.
                // Intentem trobar l'inici real de l'XML.
                int xmlStartIndex = xmlContent.IndexOf("<nmaprun");
                if (xmlStartIndex == -1) return openPorts; // No és un XML vàlid

                // Netegem el string per si hi ha brossa al principi
                string cleanXml = xmlContent.Substring(xmlStartIndex);

                // Carreguem l'XML
                var doc = XDocument.Parse(cleanXml);

                // LINQ:
                // 1. Busquem tots els elements <port>
                // 2. Filtrem aquells on el fill <state> tingui l'atribut state="open"
                // 3. Seleccionem l'atribut "portid" i el convertim a int
                openPorts = doc.Descendants("port")
                    .Where(port => port.Element("state")?.Attribute("state")?.Value == "open")
                    .Select(port => int.Parse(port.Attribute("portid").Value))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsejant XML de Nmap: {ex.Message}");
            }

            return openPorts;
        }



        public static async Task<List<Findings>> ParseServicesToFindingsAsync(string xmlOutput)
        {
            var findingsList = new List<Findings>();

            // Validació bàsica per si l'output és buit
            if (string.IsNullOrWhiteSpace(xmlOutput)) return findingsList;

            XDocument doc;
            try
            { 
                doc = XDocument.Parse(xmlOutput);
            }
            catch
            {
                // Si l'XML no és vàlid, retornem llista buida (o pots llançar excepció)
                return findingsList;
            }

            // Iterem sobre tots els ports trobats
            var ports = doc.Descendants("port");

            foreach (var port in ports)
            {
                // 1. Informació del servei (Affected Service)
                var serviceElem = port.Element("service");
                var product = serviceElem?.Attribute("product")?.Value ?? "Unknown Service";
                var version = serviceElem?.Attribute("version")?.Value ?? "";
                var fullServiceName = $"{product} {version}".Trim();

                // 2. Busquem l'script de vulners
                var vulnersScript = port.Elements("script")
                                        .FirstOrDefault(s => s.Attribute("id")?.Value == "vulners");

                if (vulnersScript == null) continue;

                // 3. Iterem sobre les taules de vulnerabilitats
                foreach (var table in vulnersScript.Descendants("table"))
                {
                    var id = GetElemValue(table, "id");

                    // --- FILTRE CRÍTIC: Només volem CVEs ---
                    if (string.IsNullOrEmpty(id) || !id.StartsWith("CVE-"))
                    {
                        continue;
                    }

                    // --- Extracció de dades ---
                    var severity = GetElemValue(table, "cvss");

                    // --- Obtenir descripció real (NIST API) ---
                    // Nota: Aquí fem servir await, per això el mètode és async
                    var realDescription = await CveHelper.GetCveDescriptionAsync(id);

                    // 4. Creació de l'objecte Findings
                    findingsList.Add(new Findings()
                    {
                        Title = $"{fullServiceName} - {id}", // Ex: Apache httpd 2.4.7 - CVE-2021-41773
                        Severity = severity!,
                        Cve_id = id,
                        Affected_service = fullServiceName,
                        Description = realDescription,
                        Created_at = DateTime.UtcNow
                    });
                }
            }

            // Opcional: Ordenar per severitat (més greus primer)
            return findingsList.OrderByDescending(f => f.Severity).ToList();
        }

        // Helper per treure valors de les taules de Nmap
        private static string? GetElemValue(XElement table, string key)
        {
            return table.Elements("elem")
                        .FirstOrDefault(e => e.Attribute("key")?.Value == key)
                        ?.Value;
        }

        public static List<Findings> ParseDatabaseFindings(string xmlOutput)
        {
            var findings = new List<Findings>();
            if (string.IsNullOrWhiteSpace(xmlOutput)) return findings;

            try
            {
                int xmlStartIndex = xmlOutput.IndexOf("<nmaprun");
                if (xmlStartIndex == -1) return findings;
                var doc = XDocument.Parse(xmlOutput.Substring(xmlStartIndex));

                var ports = doc.Descendants("port");

                foreach (var port in ports)
                {
                    var portId = port.Attribute("portid")?.Value;
                    var serviceName = port.Element("service")?.Attribute("product")?.Value
                                      ?? port.Element("service")?.Attribute("name")?.Value
                                      ?? "Database";

                    foreach (var script in port.Elements("script"))
                    {
                        var scriptId = script.Attribute("id")?.Value ?? "";
                        var output = script.Attribute("output")?.Value ?? "";

                        if (string.IsNullOrWhiteSpace(output)) continue;

                        // --- LÒGICA DE DETECCIÓ ---

                        // 1. Redis Obert (redis-info)
                        if (scriptId == "redis-info" && output.Contains("authentication_required:0")) // O text similar
                        {
                            // A vegades nmap diu "authentication: required" o no. 
                            // Una pista millor és si veiem dades internes com "redis_version" sense error.
                            if (output.Contains("redis_version"))
                            {
                                findings.Add(CreateDbFinding("Redis Unauthenticated", "High", serviceName, portId, output));
                            }
                        }

                        // 2. MySQL/Postgres/MSSQL Empty Password
                        if (scriptId.Contains("empty-password") || scriptId.Contains("no-auth"))
                        {
                            if (output.Contains("success") || output.Contains("root") || output.Contains("sa") || output.Contains("admin"))
                            {
                                findings.Add(CreateDbFinding("CRITICAL: Database Empty Password", "Critical", serviceName, portId, output));
                            }
                        }

                        // 3. Vulnerabilitats CVE (mysql-vuln-*)
                        if (scriptId.Contains("vuln") && (output.Contains("VULNERABLE") || output.Contains("State: VULNERABLE")))
                        {
                            findings.Add(CreateDbFinding($"Vulnerable Database Component ({scriptId})", "High", serviceName, portId, output));
                        }

                        // 4. MongoDB Info (sense auth)
                        if (scriptId == "mongodb-info" && !output.Contains("authentication failed"))
                        {
                            findings.Add(CreateDbFinding("MongoDB Information Exposure", "Medium", serviceName, portId, output));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsejant DB XML: {ex.Message}");
            }

            return findings;
        }

        // Helper intern per no repetir codi
        private static Findings CreateDbFinding(string title, string severity, string service, string port, string details)
        {
            return new Findings
            {
                Title = title,
                Severity = MapSeverity(severity), // Assegura't de tenir un mapeig a string "10.0", "7.0", etc.
                Cve_id = "Misconfiguration",
                Affected_service = $"{service} (Port {port})",
                Description = $"Nmap Script Result:\n{details}",
                Created_at = DateTime.UtcNow
            };
        }

        private static string MapSeverity(string level) => level switch
        {
            "Critical" => "10.0",
            "High" => "8.0",
            "Medium" => "5.0",
            _ => "1.0"
        };
    }
}