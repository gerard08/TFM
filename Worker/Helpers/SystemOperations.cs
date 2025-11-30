using System.Diagnostics;
using System.Text;

namespace Worker.Helpers
{
    public static class SystemOperations
    {

        public static async Task<string> ExecuteCommandAsync(string command, string arguments, CancellationToken cancellationToken, string? outputFile = null)
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

            var stdOutBuilder = new StringBuilder();
            var stdErrBuilder = new StringBuilder();

            try
            {
                using var process = new Process { StartInfo = processStartInfo };

                // --- OPTIMITZACIÓ DE MEMÒRIA ---
                // Si tenim un outputFile (Nuclei), no ens interessa guardar la sortida de consola a la RAM.
                // Però l'hem de llegir igualment per "buidar la canonada", si no el procés es penja.
                bool saveOutputToRam = string.IsNullOrEmpty(outputFile);

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        // Només guardem a la RAM si és necessari (Nmap)
                        if (saveOutputToRam)
                        {
                            stdOutBuilder.AppendLine(e.Data);
                        }
                        // Si és Nuclei (amb fitxer), llegim e.Data però no fem res amb ell.
                        // El Garbage Collector l'elimina immediatament.
                    }
                };

                process.ErrorDataReceived += (s, e) => { if (e.Data != null) stdErrBuilder.AppendLine(e.Data); };

                Console.WriteLine($"Executant: {command} {arguments}");

                if (!process.Start())
                    return $"ERROR: Could not start process {command}.";

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                    return "CANCELLED";
                }

                process.WaitForExit(); // Assegurar que els buffers estan buits

                if (process.ExitCode != 0)
                {
                    // Lògica d'errors: Si falla i no tenim ni fitxer ni output de consola, retornem error.
                    bool fileCreated = outputFile != null && File.Exists(outputFile);

                    if (stdErrBuilder.Length > 0 && stdOutBuilder.Length == 0 && !fileCreated)
                    {
                        return $"ERROR: Process exited with code {process.ExitCode}.\nStderr:\n{stdErrBuilder}";
                    }
                }

                // Si tenim fitxer, el llegim ara (Molt més eficient que acumular-ho en temps real)
                if (!string.IsNullOrEmpty(outputFile) && File.Exists(outputFile))
                {
                    return await File.ReadAllTextAsync(outputFile, cancellationToken);
                }

                // Si no tenim fitxer, retornem el que hem capturat a la RAM (Nmap)
                return stdOutBuilder.ToString();
            }
            catch (Exception ex)
            {
                return $"ERROR: Exception while executing '{command}': {ex.Message}";
            }
        }
    }
}

