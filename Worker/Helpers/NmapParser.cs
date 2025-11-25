using System.Text.Json;
using System.Text.RegularExpressions;

namespace Worker.Helpers
{
    public static class NmapParser
    {
        public static string ParseNmapServiceDiscoveryResult(this string output)
        {
            var result = new
            {
                host = ParseHosts(output),
                ports = ParsePorts(output)
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private static string ParseHosts(string output)
        {
            var match = Regex.Match(output, @"Nmap scan report for ([\d\.]+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static List<object> ParsePorts(string output)
        {
            var ports = new List<object>();
            // Captura línies com: 22/tcp   open  ssh        OpenSSH 9.2p1 Debian 2+deb12u7 (protocol 2.0)
            var regex = new Regex(@"(\d+)\/(\w+)\s+(\w+)\s+(\S+)\s+(.*)");

            foreach (var line in output.Split('\n'))
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    ports.Add(new
                    {
                        port = int.Parse(match.Groups[1].Value),
                        protocol = match.Groups[2].Value,
                        state = match.Groups[3].Value,
                        service = match.Groups[4].Value,
                        version = match.Groups[5].Value.Trim()
                    });
                }
            }

            return ports;
        }
    }
}
