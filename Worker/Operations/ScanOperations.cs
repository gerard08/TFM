using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Worker.Helpers;
using Worker.Models;

namespace Worker.Operations
{
    public static class ScanOperations
    {
        public static async Task<string> RunScanAsync(ScanRequest request, CancellationToken cancellationToken)
        {
            // Decideix quina eina fer servir segons el tipus
            var (command, arguments) = request.ScanType switch
            {
                "services" => ("nmap", $"-sV {request.Target}"),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };

            var rawResult = await ExecuteCommandAsync(command, arguments, cancellationToken);

            return request.ScanType switch
            {
                "services" => rawResult.ParseNmapServiceDiscoveryResult(),
                _ => throw new InvalidOperationException($"Tipus d'escaneig desconegut: {request.ScanType}")
            };
        }

        private static async Task<string> ExecuteCommandAsync(string command, string arguments, CancellationToken cancellationToken)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var outputBuilder = new StringBuilder();

            try
            {
                var process = new Process { StartInfo = processStartInfo };

                process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

                if (!process.Start())
                    return $"ERROR: Could not start process {command}.";

                Console.WriteLine($"Executant {command} {arguments}");

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var waitTask = process.WaitForExitAsync(cancellationToken);
                try
                {
                    await waitTask;
                }
                catch (OperationCanceledException)
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    return $"CANCELLED: Execution of {command} {arguments} was cancelled.";
                }

                if (process.ExitCode != 0)
                {
                    return $"ERROR: Process exited with code {process.ExitCode}.\nOutput:\n{outputBuilder}";
                }

                return outputBuilder.ToString();
            }
            catch (System.ComponentModel.Win32Exception w32ex)
            {
                return $"ERROR: Could not start '{command}': {w32ex.Message}";
            }
            catch (Exception ex)
            {
                return $"ERROR: Exception while executing '{command}': {ex.Message}";
            }
        }
    }
}
