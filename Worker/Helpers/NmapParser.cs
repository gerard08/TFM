using DetectorVulnerabilitatsDatabase.Models;
using System.Globalization; // Necessari per parsejar decimals amb punts
using System.Xml.Linq;

namespace Worker.Helpers
{
    public static class NmapParser
    {

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
    }
}