namespace LanServer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            if (Config.IsFirstLaunch())
                Config.Save();

            Application.Run(new MainForm());
        }
    }
}
