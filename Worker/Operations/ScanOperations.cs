using DetectorVulnerabilitatsDatabase.Models;
using System.Diagnostics;
using System.Text;
using Worker.Helpers;
using Worker.Models;

namespace Worker.Operations
{
    public static class ScanOperations
    {
        public static async Task<List<Findings>> RunScanAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            var portsOberts = await DiscoverOpenPortsAsync(request, cancellationToken);

            // Decideix quina eina fer servir segons el tipus
            var arguments = request.ScanType switch
            {
                ScanTypeEnum.Services => $"-sV --version-intensity 9 -p {string.Join(",", portsOberts)} --script vulners -oX - {request.Target}",
                ScanTypeEnum.WebEnumeration => $"-sV --version-intensity 9 -p {string.Join(",", portsOberts)} --\"script http-enum,http-title,http-headers,http-methods\" -oX - {request.Target}",
                ScanTypeEnum.WebVuln => $"-sV --version-intensity 9 -p {string.Join(",", portsOberts)} --script \"http-sql-injection,http-csrf,http-config-backup,http-git\" -oX - {request.Target}",
                ScanTypeEnum.CmsScan => $"-sV --version-intensity 9 -p {string.Join(",", portsOberts)} --script \"http-wordpress-enum,http-generator,http-drupal-enum\" -oX - {request.Target}",
                ScanTypeEnum.VulnDb => $"-sV --version-intensity 9 -p {string.Join(",", portsOberts)} --script \"(mysql* or ms-sql* or pgsql* or oracle*) and vuln\" -oX - {request.Target}",
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };

            var rawResult = await ExecuteCommandAsync(arguments, cancellationToken);

            return request.ScanType switch
            {
                ScanTypeEnum.Services => await NmapParser.ParseServicesToFindingsAsync(rawResult),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };
        }

        public static async Task<List<int>> DiscoverOpenPortsAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            var arguments = $"nmap -p- -sS -n -T4 --min-rate 2000 --max-retries 2 -oX - {request.Target}";
            var rawResult = await ExecuteCommandAsync(arguments, cancellationToken);

            if (string.IsNullOrWhiteSpace(rawResult)) return new();
            if (rawResult == "CANCELLED") return new();
            if (rawResult.StartsWith("ERROR"))
            {
                Console.WriteLine(rawResult);
                return new();
            }

            return NmapParser.ParseNmapXmlPorts(rawResult);
        }

        private static async Task<string> ExecuteCommandAsync(string arguments, CancellationToken cancellationToken)
        {
            const string command = "nmap";
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // 1. Separem la sortida neta (XML) dels errors/logs
            var stdOutBuilder = new StringBuilder();
            var stdErrBuilder = new StringBuilder();

            try
            {
                using var process = new Process { StartInfo = processStartInfo };

                process.OutputDataReceived += (s, e) => { if (e.Data != null) stdOutBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) stdErrBuilder.AppendLine(e.Data); };

                if (!process.Start())
                    return $"ERROR: Could not start process {command}.";

                Console.WriteLine($"Executant {command} {arguments}");

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Esperem que el procés acabi de forma asíncrona (permet cancel·lació)
                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                    return "CANCELLED";
                }

                // 2. TRUC CRUCIAL:
                // WaitForExitAsync retorna quan el procés mor, però NO garanteix que els events
                // OutputDataReceived hagin acabat de processar el buffer.
                // Cridar a WaitForExit() (síncron) sense arguments aquí força a buidar els buffers.
                // Com que el procés ja ha acabat (per l'await anterior), això és molt ràpid.
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    // Retornem l'error real (stderr) en lloc de l'XML
                    return $"ERROR: Process exited with code {process.ExitCode}.\nStderr:\n{stdErrBuilder}";
                }

                // Retornem només el Standard Output (l'XML net)
                return stdOutBuilder.ToString();
            }
            catch (Exception ex)
            {
                return $"ERROR: Exception while executing '{command}': {ex.Message}";
            }
        }
    }
}
