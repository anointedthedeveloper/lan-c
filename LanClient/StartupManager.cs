using Microsoft.Win32;

namespace LanClient
{
    public static class StartupManager
    {
        private const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "LanClient";

        public static void Enable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey, true);
            key?.SetValue(AppName, $"\"{Environment.ProcessPath}\"");
        }

        public static void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey, true);
            key?.DeleteValue(AppName, false);
        }

        public static bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey);
            return key?.GetValue(AppName) != null;
        }
    }
}
