namespace LanClient
{
    public class MainForm : Form
    {
        private Label        _statusLabel  = null!;
        private Label        _connCardVal  = null!;   // Connection stat card value
        private Label        _serverVal    = null!;
        private Label        _autoStartVal = null!;
        private RichTextBox  _commandLog   = null!;
        private Panel        _statusCard   = null!;
        private Panel        _statusBadge  = null!;
        private AnimatedStatusDot _animDot = null!;
        private readonly LanWebSocketClient _wsClient;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color BgApp     = Color.FromArgb(245, 247, 250);
        private static readonly Color BgSide    = Color.FromArgb(255, 255, 255);
        private static readonly Color BgCard    = Color.FromArgb(255, 255, 255);
        private static readonly Color BgCard2   = Color.FromArgb(248, 250, 253);
        private static readonly Color BgHover   = Color.FromArgb(239, 244, 255);
        private static readonly Color Border    = Color.FromArgb(226, 232, 240);
        private static readonly Color Blue      = Color.FromArgb(37,  99,  235);
        private static readonly Color BlueSoft  = Color.FromArgb(239, 246, 255);
        private static readonly Color Green     = Color.FromArgb(5,   150, 105);
        private static readonly Color GreenSoft = Color.FromArgb(236, 253, 245);
        private static readonly Color Red       = Color.FromArgb(220,  38,  38);
        private static readonly Color RedSoft   = Color.FromArgb(254, 242, 242);
        private static readonly Color Purple    = Color.FromArgb(99,  102, 241);
        private static readonly Color TxtPri    = Color.FromArgb(15,   23,  42);
        private static readonly Color TxtSec    = Color.FromArgb(71,   85, 105);
        private static readonly Color TxtMuted  = Color.FromArgb(148, 163, 184);

        public MainForm(LanWebSocketClient wsClient)
        {
            _wsClient = wsClient;
            InitializeUI();
            bool already = wsClient.IsConnected;
            UpdateStatus(already ? "Connected" : "Searching for server...", already);
            _wsClient.StatusChanged    += s   => UpdateStatus(s, s.StartsWith("Connected"));
            _wsClient.CommandCompleted += (cmd, ok) => AddLog(cmd, ok);
        }

        // ── Live updates ──────────────────────────────────────────────────────
        private void UpdateStatus(string status, bool connected)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatus(status, connected)); return; }

            // Sidebar label
            _statusLabel.Text      = connected ? "● Connected" : "● " + status;
            _statusLabel.ForeColor = connected ? Green : Red;
            _statusBadge.BackColor = connected ? GreenSoft : RedSoft;
            _animDot.SetColor(connected ? Green : Red);

            // Connection stat card
            _connCardVal.Text      = connected ? "Connected" : "Searching";
            _connCardVal.ForeColor = connected ? Green : Red;

            // Status info card background + border
            _statusCard.BackColor  = connected ? GreenSoft : RedSoft;
            _statusCard.Invalidate();

            // Server address
            _serverVal.Text = connected && !string.IsNullOrEmpty(_wsClient.ServerIp)
                ? $"{_wsClient.ServerIp}   ·   HTTP:{_wsClient.HttpPort}"
                : "—";
        }

        private void AddLog(string cmd, bool ok)
        {
            if (InvokeRequired) { Invoke(() => AddLog(cmd, ok)); return; }
            _commandLog.SelectionStart = _commandLog.TextLength;
            _commandLog.SelectionColor = TxtMuted;
            _commandLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  ");
            _commandLog.SelectionColor = ok ? Green : Red;
            _commandLog.AppendText($"{cmd}  →  {(ok ? "✓ OK" : "✗ Failed")}\n");
            _commandLog.ScrollToCaret();
        }

        // ── UI build ──────────────────────────────────────────────────────────
        private void InitializeUI()
        {
            Text          = "LanC — Client Dashboard";
            Size          = new Size(1100, 700);
            MinimumSize   = new Size(860, 540);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState   = FormWindowState.Maximized;
            BackColor     = BgApp;
            ForeColor     = TxtPri;
            Font          = new Font("Segoe UI", 9f);
            ShowInTaskbar = true;
            FormClosing  += (_, e) => { e.Cancel = true; Hide(); };
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // ── Sidebar ───────────────────────────────────────────────────────
            var sidebar = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = BgSide };
            sidebar.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };

            // Logo
            var logoPanel = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = BgSide };
            logoPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, logoPanel.Height - 1, logoPanel.Width, logoPanel.Height - 1);
            };
            var accentMark = new Panel { Width = 4, Height = 28, Left = 20, Top = 22, BackColor = Blue };
            var logoTitle  = new Label
            {
                Text = "LanC", Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = TxtPri, AutoSize = true, Left = 34, Top = 14, BackColor = Color.Transparent
            };
            var logoSub = new Label
            {
                Text = "Client Dashboard", Font = new Font("Segoe UI", 7.5f),
                ForeColor = TxtMuted, AutoSize = true, Left = 34, Top = 40, BackColor = Color.Transparent
            };
            logoPanel.Controls.AddRange(new Control[] { accentMark, logoTitle, logoSub });

            // Status badge in sidebar
            _statusBadge = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = RedSoft };
            _statusBadge.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, _statusBadge.Height - 1, _statusBadge.Width, _statusBadge.Height - 1);
            };
            _animDot = new AnimatedStatusDot(Red) { Left = 18, Top = 16 };
            _statusLabel = new Label
            {
                Text = "● Searching...", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Red, AutoSize = true, Left = 36, Top = 15, BackColor = Color.Transparent
            };
            _statusBadge.Controls.AddRange(new Control[] { _animDot, _statusLabel });

            // Nav label
            var navLabelPnl = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = BgSide };
            navLabelPnl.Controls.Add(new Label
            {
                Text = "NAVIGATION", Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TxtMuted, AutoSize = true, Left = 20, Top = 12, BackColor = Color.Transparent
            });

            sidebar.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = BgSide });
            sidebar.Controls.Add(navLabelPnl);
            sidebar.Controls.Add(_statusBadge);
            sidebar.Controls.Add(logoPanel);

            // ── Content area ──────────────────────────────────────────────────
            var contentArea = new Panel { Dock = DockStyle.Fill, BackColor = BgApp };

            // Top bar
            var topBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = BgCard };
            topBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };
            var pageTitle = new Label
            {
                Text = "Overview", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = TxtPri, AutoSize = true, Left = 24, Top = 14, BackColor = Color.Transparent
            };
            var pageSub = new Label
            {
                Text = "Monitor this machine's connection to the LanC server.",
                Font = new Font("Segoe UI", 8f), ForeColor = TxtSec,
                AutoSize = true, Left = 24, Top = 40, BackColor = Color.Transparent
            };
            // Machine badge top-right
            var machineBadge = new Panel { Height = 30, Width = 210, Top = 17, BackColor = BlueSoft };
            machineBadge.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(100, Blue.R, Blue.G, Blue.B), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, machineBadge.Width - 1, machineBadge.Height - 1);
            };
            machineBadge.Controls.Add(new Label
            {
                Text = $"⊞  {Environment.MachineName}", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Blue, AutoSize = true, Left = 10, Top = 6, BackColor = Color.Transparent
            });
            topBar.SizeChanged += (_, _) => machineBadge.Left = topBar.Width - machineBadge.Width - 20;
            topBar.Controls.AddRange(new Control[] { pageTitle, pageSub, machineBadge });

            // Body
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = BgApp, Padding = new Padding(20, 16, 20, 20)
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ── Stat cards ────────────────────────────────────────────────────
            var statsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3,
                BackColor = Color.Transparent
            };
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            statsRow.Controls.Add(MakeStatCard("Connection",  "Searching",
                "LanC server status", Red,    out _connCardVal),  0, 0);
            statsRow.Controls.Add(MakeStatCard("Machine",     Environment.MachineName,
                "This computer",      Blue,   out _),  1, 0);
            statsRow.Controls.Add(MakeStatCard("Auto-Start",  StartupManager.IsEnabled() ? "Enabled" : "Disabled",
                "Runs on Windows startup", Purple, out _), 2, 0);

            // ── Info / status card ────────────────────────────────────────────
            _statusCard = new Panel
            {
                Dock = DockStyle.Fill, BackColor = RedSoft,
                Margin = new Padding(0, 12, 0, 12)
            };
            _statusCard.Paint += (_, e) =>
            {
                var connected = _statusLabel.ForeColor == Green;
                var ac = connected ? Green : Red;
                using var pen = new Pen(Color.FromArgb(60, ac.R, ac.G, ac.B), 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, _statusCard.Width - 1, _statusCard.Height - 1);
                using var bar = new SolidBrush(ac);
                e.Graphics.FillRectangle(bar, 0, 0, 4, _statusCard.Height);
            };
            var infoLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3,
                BackColor = Color.Transparent, Padding = new Padding(24, 16, 24, 16)
            };
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            infoLayout.Controls.Add(MakeInfoGroup("Server Address", out _serverVal,    "—"),                 0, 0);
            infoLayout.Controls.Add(MakeInfoGroup("Machine Name",   out _,             Environment.MachineName), 1, 0);
            infoLayout.Controls.Add(MakeInfoGroup("Auto-Start",     out _autoStartVal,
                StartupManager.IsEnabled() ? "● Enabled" : "○ Disabled"),  2, 0);
            _autoStartVal.ForeColor = StartupManager.IsEnabled() ? Green : TxtMuted;
            _statusCard.Controls.Add(infoLayout);

            // ── Command log ───────────────────────────────────────────────────
            var logCard = new Panel { Dock = DockStyle.Fill, BackColor = BgCard };
            logCard.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, logCard.Width - 1, logCard.Height - 1);
            };
            var logHdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = BgCard2 };
            logHdr.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, logHdr.Height - 1, logHdr.Width, logHdr.Height - 1);
            };
            var logTitle = new Label
            {
                Text = "≡  Command History", Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Blue, AutoSize = true, Left = 16, Top = 13, BackColor = Color.Transparent
            };
            var clearBtn = new Button
            {
                Text = "Clear", Font = new Font("Segoe UI", 8f),
                BackColor = BgCard, ForeColor = TxtSec, FlatStyle = FlatStyle.Flat,
                Width = 62, Height = 26, Top = 9, Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Border }
            };
            clearBtn.MouseEnter += (_, _) => { clearBtn.BackColor = BgHover; clearBtn.ForeColor = Blue; };
            clearBtn.MouseLeave += (_, _) => { clearBtn.BackColor = BgCard;  clearBtn.ForeColor = TxtSec; };
            clearBtn.Click += (_, _) => _commandLog.Clear();
            logHdr.SizeChanged += (_, _) => clearBtn.Left = logHdr.Width - clearBtn.Width - 16;
            logHdr.Controls.AddRange(new Control[] { logTitle, clearBtn });

            _commandLog = new RichTextBox
            {
                Dock = DockStyle.Fill, BackColor = BgCard2, ForeColor = TxtPri,
                Font = new Font("Consolas", 8.5f), BorderStyle = BorderStyle.None,
                ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Vertical, Padding = new Padding(8)
            };
            var emptyLbl = new Label
            {
                Text = "No commands received yet.", Font = new Font("Segoe UI", 9f),
                ForeColor = TxtMuted, AutoSize = true, BackColor = Color.Transparent
            };
            _commandLog.SizeChanged += (_, _) =>
            {
                emptyLbl.Left = (_commandLog.Width  - emptyLbl.Width)  / 2;
                emptyLbl.Top  = (_commandLog.Height - emptyLbl.Height) / 2;
            };
            _commandLog.TextChanged += (_, _) => emptyLbl.Visible = _commandLog.TextLength == 0;
            _commandLog.Controls.Add(emptyLbl);

            logCard.Controls.Add(_commandLog);
            logCard.Controls.Add(logHdr);

            body.Controls.Add(statsRow,    0, 0);
            body.Controls.Add(_statusCard, 0, 1);
            body.Controls.Add(logCard,     0, 2);

            contentArea.Controls.Add(body);
            contentArea.Controls.Add(topBar);

            Controls.Add(contentArea);
            Controls.Add(sidebar);
        }

        // ── Stat card helper ──────────────────────────────────────────────────
        private Panel MakeStatCard(string title, string value, string sub, Color accent, out Label valueLabel)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill, BackColor = BgCard,
                Margin = new Padding(0, 0, 12, 0)
            };
            bool hovered = false;
            p.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (hovered)
                {
                    using var hb = new SolidBrush(Color.FromArgb(8, accent.R, accent.G, accent.B));
                    g.FillRectangle(hb, 0, 0, p.Width, p.Height);
                }
                using var pen = new Pen(hovered ? Color.FromArgb(150, accent.R, accent.G, accent.B) : Border, 1.5f);
                g.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                using var bar = new SolidBrush(accent);
                g.FillRectangle(bar, 0, 0, p.Width, 3);
            };
            p.MouseEnter += (_, _) => { hovered = true;  p.Invalidate(); };
            p.MouseLeave += (_, _) => { hovered = false; p.Invalidate(); };

            var tl = new Label { Text = title, Font = new Font("Segoe UI", 8f), ForeColor = TxtSec,  AutoSize = true, Left = 18, Top = 18, BackColor = Color.Transparent };
            valueLabel = new Label { Text = value, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = TxtPri, AutoSize = true, Left = 18, Top = 34, BackColor = Color.Transparent };
            var sl = new Label { Text = sub,   Font = new Font("Segoe UI", 7.5f), ForeColor = TxtMuted, AutoSize = true, Left = 18, Top = 74, BackColor = Color.Transparent };
            p.Controls.AddRange(new Control[] { tl, valueLabel, sl });
            return p;
        }

        // ── Info group helper ─────────────────────────────────────────────────
        private Panel MakeInfoGroup(string label, out Label valueLabel, string initial)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 7.5f), ForeColor = TxtMuted, AutoSize = true, Left = 0, Top = 0, BackColor = Color.Transparent };
            valueLabel = new Label { Text = initial, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = TxtSec, AutoSize = true, Left = 0, Top = 20, BackColor = Color.Transparent };
            p.Controls.AddRange(new Control[] { lbl, valueLabel });
            return p;
        }
    }

    // ── Animated dot ─────────────────────────────────────────────────────────
    internal class AnimatedStatusDot : Control
    {
        private Color _color;
        private float _pulse = 0f;
        private readonly System.Windows.Forms.Timer _timer;

        public AnimatedStatusDot(Color color)
        {
            _color = color;
            Width = 12; Height = 12;
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            _timer = new System.Windows.Forms.Timer { Interval = 50 };
            _timer.Tick += (_, _) =>
            {
                _pulse = (_pulse + 0.08f) % (float)(Math.PI * 2);
                Invalidate();
            };
            _timer.Start();
        }

        public void SetColor(Color c) { _color = c; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float scale = 1f + 0.3f * (float)Math.Sin(_pulse);
            int cx = Width / 2, cy = Height / 2;
            float r = 3.5f * scale;
            using var glow = new SolidBrush(Color.FromArgb(50, _color));
            g.FillEllipse(glow, cx - r - 2, cy - r - 2, (r + 2) * 2, (r + 2) * 2);
            using var dot = new SolidBrush(_color);
            g.FillEllipse(dot, cx - 3f, cy - 3f, 6f, 6f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer.Stop(); _timer.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
