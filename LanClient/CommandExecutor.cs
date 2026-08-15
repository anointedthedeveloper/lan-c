using System.Diagnostics;
using System.Net.Http;

namespace LanClient
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    public class CommandExecutor
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };
        private static readonly string _downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LanClient", "Downloads");

        static CommandExecutor() => Directory.CreateDirectory(_downloadDir);

        public async Task<CommandResult> Download(string fileName, string url, IProgress<int>? progress = null)
        {
            try
            {
                var dest = Path.Combine(_downloadDir, fileName);
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? -1;
                using var stream = await response.Content.ReadAsStreamAsync();
                using var file = File.Create(dest);
                var buffer = new byte[81920];
                long downloaded = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read));
                    downloaded += read;
                    if (total > 0) progress?.Report((int)(downloaded * 100 / total));
                }
                return new CommandResult { Success = true, Message = dest };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<CommandResult> Install(string fileName, string url, string installerType, IProgress<int>? progress = null)
        {
            var dl = await Download(fileName, url, progress);
            if (!dl.Success) return dl;

            var filePath = dl.Message;
            var (exe, args) = GetInstallCommand(filePath, installerType);

            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var proc = Process.Start(psi);
                if (proc == null) return new CommandResult { Success = false, Message = "Failed to start installer." };
                await proc.WaitForExitAsync();
                return new CommandResult { Success = proc.ExitCode == 0, Message = $"Exit code: {proc.ExitCode}" };
            }
            catch (Exception ex)
            {
                return new CommandResult { Success = false, Message = ex.Message };
            }
        }

        private static (string exe, string args) GetInstallCommand(string filePath, string type) => type switch
        {
            "MSI" => ("msiexec", $"/i \"{filePath}\" /quiet /norestart"),
            "Inno Setup" => (filePath, "/VERYSILENT /NORESTART"),
            "InstallShield" => (filePath, "/s /v\"/qn\""),
            _ => (filePath, "/S") // NSIS default
        };

        public static void Shutdown() =>
            Process.Start(new ProcessStartInfo("shutdown", "/s /t 10") { UseShellExecute = true });
    }
}
