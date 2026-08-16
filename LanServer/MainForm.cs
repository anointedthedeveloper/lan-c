using System.Reflection;
using LanServer.Controls;
using LanServer.Pages;

namespace LanServer
{
    public class MainForm : Form
    {
        private readonly Sidebar _sidebar;
        private readonly Panel _pageHost;
        private readonly WebServer _webServer = new();
        private readonly UdpBeacon _beacon    = new();

        public MainForm()
        {
            Text = "LanC — Server Control Panel";
            Size = new Size(1360, 820);
            MinimumSize = new Size(1100, 680);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            ShowInTaskbar = true;
            BackColor = Theme.BgApp;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;
            // Enable double buffering for smoother rendering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            LoadIcon();
            ToastManager.Init(this);

            // ── Layout ────────────────────────────────────────────────────────
            _sidebar = new Sidebar();
            _sidebar.Navigate += ShowPage;
            _sidebar.ShutdownRequested += OnShutdownRequested;

            _pageHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };

            Controls.Add(_pageHost);
            Controls.Add(_sidebar);

            // ── Start backend ─────────────────────────────────────────────────
            _webServer.LogMessage += msg => AppState.Log(msg);
            _webServer.Start();
            _beacon.Start();

            AppState.Log("LanC Server started.", LogLevel.Success);

            // Show dashboard first
            ShowPage(NavPage.Dashboard);
        }

        private void ShowPage(NavPage page)
        {
            // Dispose old page
            foreach (Control c in _pageHost.Controls)
                c.Dispose();
            _pageHost.Controls.Clear();

            Control newPage = page switch
            {
                NavPage.Deployments  => new DeploymentsPage(),
                NavPage.FileManager  => new FileManagerPage(),
                NavPage.Clients      => new ClientsPage(),
                NavPage.AutoDownload => new AutoDownloadPage(),
                NavPage.ManageApp    => new ManageAppPage(),
                NavPage.Activity     => new ActivityPage(),
                NavPage.Settings     => new SettingsPage(),
                _                    => BuildDashboard()
            };

            _pageHost.Controls.Add(newPage);
        }

        private DashboardPage BuildDashboard()
        {
            var dash = new DashboardPage();
            dash.NavigateToActivity    += () => _sidebar.SetActive(NavPage.Activity);
            dash.NavigateToFileManager += () => _sidebar.SetActive(NavPage.FileManager);
            return dash;
        }

        private void OnShutdownRequested()
        {
            if (!ConfirmDialog.Ask(this,
                "Shutdown Server?",
                "Are you sure you want to shut down the LanC server? All connected clients will be disconnected.",
                "Shutdown Server", danger: true)) return;

            AppState.Log("Server shutting down.", LogLevel.Warning);
            ToastManager.Show("Server shutting down...", ToastKind.Warning);
            _webServer.Stop();
            _beacon.Stop();
            Application.Exit();
        }

        private void LoadIcon()
        {
            try
            {
                var asm  = Assembly.GetExecutingAssembly();
                var name = asm.GetManifestResourceNames()
                              .FirstOrDefault(n => n.EndsWith("server.ico", StringComparison.OrdinalIgnoreCase));
                if (name != null)
                {
                    using var stream = asm.GetManifestResourceStream(name)!;
                    Icon = new Icon(stream);
                }
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _webServer.Stop();
            _beacon.Stop();
            base.OnFormClosing(e);
        }
    }
}
