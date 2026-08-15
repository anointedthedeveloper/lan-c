namespace LanServer
{
    public class ManagedFile
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string InstallerType { get; set; } = "NSIS";
        public long FileSize { get; set; }
    }

    public static class FileManager
    {
        public static readonly string UploadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uploads");

        static FileManager() => Directory.CreateDirectory(UploadsDir);

        public static List<ManagedFile> GetFiles() =>
            Directory.GetFiles(UploadsDir)
                .Select(f => new ManagedFile
                {
                    FileName = Path.GetFileName(f),
                    FilePath = f,
                    FileSize = new FileInfo(f).Length
                }).ToList();

        public static string SaveFile(string sourcePath, string installerType)
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
    }
}
