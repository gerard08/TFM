using DetectorVulnerabilitatsDatabase.Models;
using Worker.Helpers;
using Worker.Models;

namespace Worker.Operations
{
    public static class ScanOperations
    {
        public static async Task<List<Findings>> RunScanAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            var openPorts = await DiscoverOpenPortsAsync(request, cancellationToken);
            if (openPorts.Count == 0)
            {
                throw new InvalidOperationException("The host is down");
            }
            var targetsFile = await CreateTargetFilesAsync(request.Target, openPorts);

            var resultsFile = Path.GetTempFileName();

            // Decideix quina eina fer servir segons el tipus
            var (command, arguments) = request.ScanType switch
            {
                ScanTypeEnum.Services => ("nmap", $"-sV --version-intensity 9 -p {string.Join(",", openPorts)} --script vulners -oX - {request.Target}"),
                ScanTypeEnum.WebEnumeration => ("nuclei", $" -l {targetsFile} -tags ech,config,exposure,misconfiguration,panel,token -j -timeout 2 -retries 0 -c 10 -bs 25 -ni -disable-update-check -nc -silent -o {resultsFile}"),
                ScanTypeEnum.WebVuln => ("nuclei", $" -l {targetsFile} -tags cve,critical,high,medium,vulnerability,default-login,sqli,xss,lfi,rce -j -timeout 2 -retries 0 -c 10 -bs 25 -ni -disable-update-check -nc -silent -o {resultsFile}"),
                ScanTypeEnum.CmsScan => ("nuclei", $" -l {targetsFile} -tags tech,exposure,panel,misconfiguration,token,wordpress,wp-plugin -j -timeout 3 -retries 0 -c 15 -bs 5 -rl 50 -ni -disable-update-check -o {resultsFile}"),
                ScanTypeEnum.DDBB => ("nmap", $"-sV -p {string.Join(",", openPorts)} --script \"mysql-empty-password,mysql-vuln*,ms-sql-empty-password,ms-sql-info,mongodb-info,redis-info\" -oX - {request.Target}"),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };

            var rawResult = await SystemOperations.ExecuteCommandAsync(command, arguments, cancellationToken);

            var findings = request.ScanType switch
            {
                ScanTypeEnum.Services => await NmapParser.ParseServicesToFindingsAsync(rawResult),
                ScanTypeEnum.WebEnumeration => NucleiParser.Parse(rawResult),
                ScanTypeEnum.WebVuln => NucleiParser.Parse(rawResult),
                ScanTypeEnum.CmsScan => NucleiParser.Parse(rawResult),
                ScanTypeEnum.DDBB => await NmapParser.ParseServicesToFindingsAsync(rawResult),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };

            File.Delete(targetsFile);
            return findings;
        }

        private static async Task<string> CreateTargetFilesAsync(string host, List<int> openPorts)
        {
            var filePath = Path.GetTempFileName();
            var lines = new List<string>();

            var nonWebPorts = new HashSet<int>
            {
                21, 22, 23, 25, 53,         // Infra (FTP, SSH, Telnet, SMTP, DNS)
                3306, 5432, 1433, 27017,    // Bases de Dades (MySQL, Postgres, SQLServer, Mongo)
                6379, 11211,                // Cache (Redis, Memcached)
                5672                        // RabbitMQ (AMQP) - Nota: El 15672 SÍ que és web
            };

            foreach (var port in openPorts)
            {
                if (nonWebPorts.Contains(port))
                {
                    continue;
                }
                lines.Add($"{host}:{port}");
            }

            await File.WriteAllLinesAsync(filePath, lines);
            return filePath;
        }

        private static async Task<List<int>> DiscoverOpenPortsAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            const string command = "nmap";
            var arguments = $"-p- -sS -n -T4 --min-rate 2000 --max-retries 2 -oX - {request.Target}";
            var rawResult = await SystemOperations.ExecuteCommandAsync(command, arguments, cancellationToken);

            if (string.IsNullOrWhiteSpace(rawResult)) return new();
            if (rawResult == "CANCELLED") return new();
            if (rawResult.StartsWith("ERROR"))
            {
                Console.WriteLine(rawResult);
                return new();
            }

            return NmapParser.ParseNmapXmlPorts(rawResult);
        }
    }
}
