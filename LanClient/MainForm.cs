namespace LanClient
{
    public class MainForm : Form
    {
        private Label _statusLabel = null!;
        private Label _statusDot = null!;
        private Label _serverLabel = null!;
        private Label _machineLabel = null!;
        private ListBox _commandLog = null!;
        private readonly LanWebSocketClient _wsClient;

        private static readonly Color BgDark = Color.FromArgb(18, 18, 18);
        private static readonly Color BgPanel = Color.FromArgb(28, 28, 28);
        private static readonly Color BgControl = Color.FromArgb(42, 42, 42);
        private static readonly Color TextPrimary = Color.FromArgb(230, 230, 230);
        private static readonly Color TextSecondary = Color.FromArgb(150, 150, 150);

        public MainForm(LanWebSocketClient wsClient)
        {
            _wsClient = wsClient;
            InitializeUI();
            UpdateStatus("Searching for server...", false);
            _wsClient.StatusChanged += s => UpdateStatus(s, s.StartsWith("Connected"));
            _wsClient.CommandCompleted += (cmd, ok) => AddLog($"{cmd}  →  {(ok ? "✓ Success" : "✗ Failed")}");
        }

        private void UpdateStatus(string status, bool connected)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatus(status, connected)); return; }
            _statusLabel.Text = status;
            _statusLabel.ForeColor = connected ? Color.FromArgb(100, 220, 120) : Color.FromArgb(220, 100, 80);
            _statusDot.ForeColor = connected ? Color.FromArgb(100, 220, 120) : Color.FromArgb(220, 100, 80);
            _serverLabel.Text = string.IsNullOrEmpty(_wsClient.ServerIp)
                ? "Server:  Not connected"
                : $"Server:  {_wsClient.ServerIp}     HTTP Port: {_wsClient.HttpPort}";
        }

        private void AddLog(string msg)
        {
            if (InvokeRequired) { Invoke(() => AddLog(msg)); return; }
            _commandLog.Items.Add($"[{DateTime.Now:HH:mm:ss}]  {msg}");
            _commandLog.TopIndex = _commandLog.Items.Count - 1;
        }

        private void InitializeUI()
        {
            Text = "LanC Client";
            Size = new Size(540, 480);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BgDark;
            ForeColor = TextPrimary;
            FormClosing += (s, e) => { e.Cancel = true; Hide(); };

            // Title bar
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(10, 10, 10) };
            var titleLabel = new Label
            {
                Text = "LanC  Client",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true, Top = 10, Left = 16
            };
            var subTitle = new Label
            {
                Text = "LAN Command & Control",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextSecondary,
                AutoSize = true, Top = 33, Left = 18
            };
            titleBar.Controls.AddRange(new Control[] { titleLabel, subTitle });

            // Status card
            var statusCard = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = BgPanel, Padding = new Padding(16, 12, 16, 12) };
            _statusDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(220, 100, 80),
                AutoSize = true, Top = 14, Left = 16
            };
            _statusLabel = new Label
            {
                Text = "Searching for server...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 100, 80),
                AutoSize = true, Top = 14, Left = 38
            };
            _serverLabel = new Label
            {
                Text = "Server:  Not connected",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextSecondary,
                AutoSize = true, Top = 42, Left = 16
            };
            _machineLabel = new Label
            {
                Text = $"This Machine:  {Environment.MachineName}     Auto-start: {(StartupManager.IsEnabled() ? "Enabled" : "Disabled")}",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextSecondary,
                AutoSize = true, Top = 66, Left = 16
            };
            statusCard.Controls.AddRange(new Control[] { _statusDot, _statusLabel, _serverLabel, _machineLabel });

            // Divider
            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(50, 50, 50) };

            // Log section
            var logPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgPanel, Padding = new Padding(16, 10, 16, 16) };
            var logHeader = new Label
            {
                Text = "Command History",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                Dock = DockStyle.Top,
                Height = 28
            };
            _commandLog = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12),
                ForeColor = Color.FromArgb(100, 220, 120),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None
            };
            logPanel.Controls.Add(_commandLog);
            logPanel.Controls.Add(logHeader);

            Controls.Add(logPanel);
            Controls.Add(divider);
            Controls.Add(statusCard);
            Controls.Add(titleBar);
        }
    }
}
