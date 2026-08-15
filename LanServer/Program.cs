namespace LanServer
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            if (Config.IsFirstLaunch())
            {
                using var setup = new PasswordSetupForm();
                if (setup.ShowDialog() != DialogResult.OK) return;
                Config.Current.AdminPassword = setup.Password;
                Config.Save();
            }

            Application.Run(new MainForm());
        }
    }
}
