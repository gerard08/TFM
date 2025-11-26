using System.Text.Json;
using System.Xml.Linq;

namespace Worker.Helpers
{
    public static class NmapParser
    {
        public static string ParseNmapXmlResult(this string xmlOutput)
        {
            // Carreguem l'XML (si l'output ve brut, potser cal netejar capçaleres abans)
            var doc = XDocument.Parse(xmlOutput);

            // Busquem l'adreça IP
            var hostInfo = doc.Descendants("address")
                              .FirstOrDefault(x => x.Attribute("addrtype")?.Value == "ipv4");

            var hostIp = hostInfo?.Attribute("addr")?.Value;

            // Busquem els ports
            var portsList = new List<object>();

            // Nmap estructura: host -> ports -> port
            var ports = doc.Descendants("port");

            foreach (var port in ports)
            {
                var service = port.Element("service");
                var portId = int.Parse(port.Attribute("portid")?.Value ?? "0");
                var protocol = port.Attribute("protocol")?.Value;
                var state = port.Element("state")?.Attribute("state")?.Value;

                // Informació del servei
                var serviceName = service?.Attribute("name")?.Value ?? "unknown";
                var product = service?.Attribute("product")?.Value;
                var version = service?.Attribute("version")?.Value;
                var fullVersion = $"{product} {version}".Trim();

                // ---------------------------------------------------------
                // AQUÍ ÉS ON EXTREIEM LES VULNERABILITATS (Script 'vulners')
                // ---------------------------------------------------------
                var vulns = new List<object>();

                // Busquem l'element <script> que tingui id="vulners"
                var vulnersScript = port.Elements("script")
                                        .FirstOrDefault(s => s.Attribute("id")?.Value == "vulners");

                if (vulnersScript != null)
                {
                    // L'estructura de vulners dins l'XML sol ser taules 'table'
                    foreach (var table in vulnersScript.Descendants("table"))
                    {
                        // Busquem els elements clau dins de cada entrada de vulnerabilitat
                        var id = GetElemValue(table, "id");
                        var cvss = GetElemValue(table, "cvss");
                        var type = GetElemValue(table, "type"); // ex: cve

                        // Només afegim si tenim un ID vàlid (ex: CVE-2021-...)
                        if (!string.IsNullOrEmpty(id))
                        {
                            // AFEGIT: Obtenim la descripció real (això fa el procés més lent!)
                            // Nota: Com que ParseNmapXmlResult segurament no és async, 
                            // forcem l'espera amb .Result (compte amb els bloquejos en UI, en un Worker està bé)

                            string realSummary = "Loading...";

                            if (string.IsNullOrEmpty(id) || !id.StartsWith("CVE-"))
                            {
                                continue;
                            }

                            realSummary = CveHelper.GetCveDescriptionAsync(id).Result;
                            vulns.Add(new
                            {
                                id = id,
                                cvss = cvss,
                                type = type,
                                summary = realSummary, // <--- Aquí tens la descripció completa del NIST
                                link = $"https://vulners.com/{type}/{id}"
                            });
                        }
                    }
                }

                portsList.Add(new
                {
                    port = portId,
                    protocol = protocol,
                    state = state,
                    service = serviceName,
                    version = fullVersion,
                    vulnerabilities = vulns.Count > 0 ? vulns : null // Només mostrem si n'hi ha
                });
            }

            var result = new
            {
                host = hostIp,
                ports = portsList
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // Helper per treure valors de les taules estranyes de Nmap XML
        private static string? GetElemValue(XElement table, string key)
        {
            return table.Elements("elem")
                        .FirstOrDefault(e => e.Attribute("key")?.Value == key)
                        ?.Value;
        }
    }
}