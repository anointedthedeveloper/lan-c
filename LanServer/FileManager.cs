namespace LanServer
{
    public class ManagedFile
    {
        public string FileName     { get; set; } = "";
        public string FilePath     { get; set; } = "";
        public string FileType     { get; set; } = "Download"; // "NSIS"|"Inno Setup"|"MSI"|"InstallShield"|"Download"
        public long   FileSize     { get; set; }
    }

    public static class FileManager
    {
        public static readonly string UploadsDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");

        static FileManager() => Directory.CreateDirectory(UploadsDir);

        public static List<ManagedFile> GetFiles()
        {
            return Directory.GetFiles(UploadsDir)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .Select(f => new ManagedFile
                {
                    FileName = Path.GetFileName(f),
                    FilePath = f,
                    FileType = "Download",
                    FileSize = new FileInfo(f).Length
                })
                .ToList();
        }

        public static string SaveFile(string sourcePath, string fileType)
        {
            var dest = Path.Combine(UploadsDir, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, dest, true);
            return dest;
        }

        public static void DeleteFile(string fileName)
        {
            var path = Path.Combine(UploadsDir, fileName);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>Returns true if the file is an executable installer.</summary>
        public static bool IsInstaller(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            return ext is ".exe" or ".msi";
        }
    }
}
