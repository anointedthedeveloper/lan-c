namespace LanClient
{
    public class MainForm : Form
    {
        private Label _statusDot   = null!;
        private Label _statusLabel = null!;
        private Label _serverLabel = null!;
        private Label _machineLabel = null!;
        private RichTextBox _commandLog = null!;
        private readonly LanWebSocketClient _wsClient;

        // ── Palette (mirrors server Theme) ────────────────────────────────────
        private static readonly Color BgApp     = Color.FromArgb(8,  13, 24);
        private static readonly Color BgCard    = Color.FromArgb(17, 26, 43);
        private static readonly Color BgCard2   = Color.FromArgb(22, 34, 56);
        private static readonly Color BgInput   = Color.FromArgb(15, 23, 40);
        private static readonly Color Border    = Color.FromArgb(36, 51, 77);
        private static readonly Color Blue      = Color.FromArgb(37,  99, 235);
        private static readonly Color Green     = Color.FromArgb(16, 185, 129);
        private static readonly Color Red       = Color.FromArgb(225, 29,  72);
        private static readonly Color Amber     = Color.FromArgb(245, 158, 11);
        private static readonly Color TxtPri    = Color.FromArgb(248, 250, 252);
        private static readonly Color TxtSec    = Color.FromArgb(148, 163, 184);
        private static readonly Color TxtMuted  = Color.FromArgb(71,  85, 105);

        public MainForm(LanWebSocketClient wsClient)
        {
            _wsClient = wsClient;
            InitializeUI();
            UpdateStatus("Searching for server...", false);
            _wsClient.StatusChanged    += s => UpdateStatus(s, s.StartsWith("Connected"));
            _wsClient.CommandCompleted += (cmd, ok) => AddLog(cmd, ok);
        }

        private void UpdateStatus(string status, bool connected)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatus(status, connected)); return; }
            _statusLabel.Text     = status;
            _statusLabel.ForeColor = connected ? Green : Red;
            _statusDot.ForeColor   = connected ? Green : Red;
            _serverLabel.Text = string.IsNullOrEmpty(_wsClient.ServerIp)
                ? "Not connected"
                : $"{_wsClient.ServerIp}   HTTP:{_wsClient.HttpPort}";
        }

        private void AddLog(string cmd, bool ok)
        {
            if (InvokeRequired) { Invoke(() => AddLog(cmd, ok)); return; }
            var time = $"[{DateTime.Now:HH:mm:ss}]  ";
            _commandLog.SelectionStart = _commandLog.TextLength;
            _commandLog.SelectionColor = TxtMuted;
            _commandLog.AppendText(time);
            _commandLog.SelectionColor = ok ? Green : Red;
            _commandLog.AppendText($"{cmd}  →  {(ok ? "✓ Success" : "✗ Failed")}\n");
            _commandLog.ScrollToCaret();
        }

        private void InitializeUI()
        {
            Text = "LanC Client";
            Size = new Size(520, 500);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BgApp;
            ForeColor = TxtPri;
            Font = new Font("Segoe UI", 9f);
            FormClosing += (_, e) => { e.Cancel = true; Hide(); };

            // ── Title bar ─────────────────────────────────────────────────────
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(6, 10, 18) };
            titleBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Blue, 1);
                e.Graphics.DrawLine(pen, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };

            var titleLbl = new Label
            {
                Text = "LanC",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true, Left = 20, Top = 10
            };
            var subLbl = new Label
            {
                Text = "Client Dashboard",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 196, 255),
                AutoSize = true, Left = 20, Top = 36
            };
            titleBar.Controls.AddRange(new Control[] { titleLbl, subLbl });

            // ── Status card ───────────────────────────────────────────────────
            var statusCard = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = BgCard };
            statusCard.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, statusCard.Height - 1, statusCard.Width, statusCard.Height - 1);
                // left accent bar
                using var bar = new SolidBrush(Blue);
                e.Graphics.FillRectangle(bar, 0, 0, 3, statusCard.Height);
            };

            _statusDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 13f),
                ForeColor = Red,
                AutoSize = true, Left = 24, Top = 16
            };
            _statusLabel = new Label
            {
                Text = "Searching...",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Red,
                AutoSize = true, Left = 44, Top = 18
            };

            // Server info row
            var serverRowLbl = new Label { Text = "Server", Font = new Font("Segoe UI", 7.5f), ForeColor = TxtMuted, AutoSize = true, Left = 24, Top = 52 };
            _serverLabel = new Label { Text = "Not connected", Font = new Font("Segoe UI", 8.5f), ForeColor = TxtSec, AutoSize = true, Left = 80, Top = 51 };

            // Machine info row
            var machineRowLbl = new Label { Text = "Machine", Font = new Font("Segoe UI", 7.5f), ForeColor = TxtMuted, AutoSize = true, Left = 24, Top = 74 };
            _machineLabel = new Label
            {
                Text = $"{Environment.MachineName}   Auto-start: {(StartupManager.IsEnabled() ? "Enabled" : "Disabled")}",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TxtSec,
                AutoSize = true, Left = 80, Top = 73
            };

            statusCard.Controls.AddRange(new Control[] { _statusDot, _statusLabel, serverRowLbl, _serverLabel, machineRowLbl, _machineLabel });

            // ── Log section ───────────────────────────────────────────────────
            var logCard = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Padding = new Padding(0) };

            var logHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = BgCard2 };
            logHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, logHeader.Height - 1, logHeader.Width, logHeader.Height - 1);
            };
            var logTitle = new Label
            {
                Text = "≡  Command History",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 196, 255),
                AutoSize = true, Left = 16, Top = 11
            };
            logHeader.Controls.Add(logTitle);

            _commandLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = TxtPri,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(8)
            };

            // Empty state label (shown until first log entry)
            var emptyLbl = new Label
            {
                Text = "No commands received yet.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TxtMuted,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _commandLog.SizeChanged += (_, _) =>
            {
                emptyLbl.Left = (_commandLog.Width  - emptyLbl.Width)  / 2;
                emptyLbl.Top  = (_commandLog.Height - emptyLbl.Height) / 2;
            };
            _commandLog.TextChanged += (_, _) => emptyLbl.Visible = _commandLog.TextLength == 0;
            _commandLog.Controls.Add(emptyLbl);

            logCard.Controls.Add(_commandLog);
            logCard.Controls.Add(logHeader);

            Controls.Add(logCard);
            Controls.Add(statusCard);
            Controls.Add(titleBar);
        }
    }
}
