namespace LanClient
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(true, "LanClientSingleInstance", out bool isNew);
            if (!isNew) return;

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApp());
        }
    }
}
