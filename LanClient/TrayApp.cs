namespace LanClient
{
    public class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private MainForm? _mainForm;
        private readonly LanWebSocketClient _wsClient = new();
        private readonly ServerDiscovery _discovery = new();
        private bool _connected;

        public TrayApp()
        {
            _tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "LanC Client - Searching..."
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Dashboard", null, (s, e) => ShowDashboard());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (s, e) => ShowDashboard();

            _wsClient.StatusChanged += OnStatusChanged;

            _discovery.ServerFound += async (ip, wsPort, httpPort) =>
            {
                if (_connected) return;
                _connected = true;
                _discovery.Stop();
                await _wsClient.ConnectAsync(ip, wsPort, httpPort);
            };

            _discovery.Start();
            StartupManager.Enable();
        }

        private void OnStatusChanged(string status)
        {
            _tray.Text = $"LanC Client - {status}";
            _tray.Icon = status.StartsWith("Connected") ? SystemIcons.Information : SystemIcons.Application;

            if (!status.StartsWith("Connected"))
            {
                _connected = false;
                _discovery.Start();
            }
        }

        private void ShowDashboard()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
                _mainForm = new MainForm(_wsClient);
            _mainForm.Show();
            _mainForm.BringToFront();
        }

        private void ExitApp()
        {
            _wsClient.Disconnect();
            _discovery.Stop();
            _tray.Visible = false;
            Application.Exit();
        }
    }
}
