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
            var targetsFile = await CreateTargetFilesAsync(request.Target, openPorts, request.ScanType);

            var resultsFile = Path.GetTempFileName();

            // Decideix quina eina fer servir segons el tipus
            var (command, arguments) = request.ScanType switch
            {
                ScanTypeEnum.Services => ("nmap", $"-sV --version-intensity 9 -p {string.Join(",", openPorts)} --script vulners -oX - {request.Target}"),
                ScanTypeEnum.Infrastructure => ("nuclei", $" -l {targetsFile} -tags network,db,redis,memcached,misconfig -timeout 5 -retries 2 -c 15 -ni -nc -silent -o {resultsFile}"),
                ScanTypeEnum.WebEnumeration => ("nuclei", $" -l {targetsFile} -tags tech,exposed-panels,login,token,exposure -timeout 5 -retries 1 -c 15 -ni -nc -silent -o {resultsFile}"),
                ScanTypeEnum.WebVuln => ("nuclei", $" -l {targetsFile} -tags cve,critical,high,medium,rce,sqli,xss,lfi -timeout 8 -retries 1 -c 10 -bs 10 -ni -nc -silent -o {resultsFile}"),
                ScanTypeEnum.DDBB => ("nuclei", $" -l {targetsFile} -tags mysql,mariadb,db,credential -timeout 5 -retries 1 -c 10 -ni -nc -silent -o {resultsFile}"),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };

            var rawResult = await SystemOperations.ExecuteCommandAsync(command, arguments, cancellationToken);

            var findings = request.ScanType switch
            {
                ScanTypeEnum.Services => await NmapParser.ParseServicesToFindingsAsync(rawResult),
                ScanTypeEnum.Infrastructure => NucleiParser.Parse(rawResult),
                ScanTypeEnum.WebEnumeration => NucleiParser.Parse(rawResult),
                ScanTypeEnum.WebVuln => NucleiParser.Parse(rawResult),
                ScanTypeEnum.DDBB => NucleiParser.Parse(rawResult),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };

            File.Delete(targetsFile);
            File.Delete(resultsFile);
            return findings;
        }

        private static async Task<string> CreateTargetFilesAsync(string host, List<NetworkService> services, ScanTypeEnum scanType)
        {
            var filePath = Path.GetTempFileName();
            var lines = new List<string>();

            foreach (var service in services)
            {
                // Normalitzem el nom a minúscules per comparar fàcilment
                string sName = service.ServiceName.ToLower();
                string product = service.Product?.ToLower() ?? "";

                // Lògica intel·ligent basada en el NOM del servei
                bool isWeb = sName.Contains("http") || sName.Contains("https") || sName.Contains("ssl") || product.Contains("apache") || product.Contains("nginx");
                bool isDatabase = sName.Contains("sql") || sName.Contains("redis") || sName.Contains("mongo") || sName.Contains("oracle") || sName.Contains("maria");

                switch (scanType)
                {
                    case ScanTypeEnum.WebEnumeration:
                    case ScanTypeEnum.WebVuln:
                        // Només afegim si Nmap diu que és HTTP/HTTPS, estigui al port que estigui
                        if (isWeb)
                        {
                            string prefix = sName.Contains("ssl") || sName.Contains("https") ? "https://" : "http://";
                            lines.Add($"{prefix}{host}:{service.Port}");
                        }
                        break;

                    case ScanTypeEnum.DDBB:
                        // Només afegim si Nmap diu que és una Base de Dades
                        if (isDatabase)
                        {
                            lines.Add($"{host}:{service.Port}");
                        }
                        break;

                    case ScanTypeEnum.Infrastructure:
                        // Infrastructure mira DBs, FTP, SSH, etc. Excloem web per no duplicar feina
                        if (!isWeb)
                        {
                            lines.Add($"{host}:{service.Port}");
                        }
                        break;
                }
            }

            // Si no trobem res per aquell tipus, deixem el fitxer buit (Nuclei acabarà ràpid)
            await File.WriteAllLinesAsync(filePath, lines);
            return filePath;
        }

        private static async Task<List<NetworkService>> DiscoverOpenPortsAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            const string command = "nmap";
            var arguments = $"-p- -sS -n -T4 --max-retries 3 -Pn --open -oX - {request.Target}";
            var rawResult = await SystemOperations.ExecuteCommandAsync(command, arguments, cancellationToken);

            if (string.IsNullOrWhiteSpace(rawResult)) return new();
            if (rawResult == "CANCELLED") return new();
            if (rawResult.StartsWith("ERROR"))
            {
                Console.WriteLine(rawResult);
                return new();
            }

            return NmapParser.ParseNmapXmlToServices(rawResult);
        }
    }
}
