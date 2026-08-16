namespace LanClient
{
    public class MainForm : Form
    {
        private Label       _statusDot    = null!;
        private Label       _statusLabel  = null!;
        private Label       _serverVal    = null!;
        private Label       _machineVal   = null!;
        private Label       _autoStartVal = null!;
        private RichTextBox _commandLog   = null!;
        private Panel       _statusCard   = null!;
        private readonly LanWebSocketClient _wsClient;

        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color BgApp    = Color.FromArgb(245, 247, 250);
        private static readonly Color BgSide   = Color.FromArgb(255, 255, 255);
        private static readonly Color BgCard   = Color.FromArgb(255, 255, 255);
        private static readonly Color BgCard2  = Color.FromArgb(248, 250, 253);
        private static readonly Color BgHover  = Color.FromArgb(239, 244, 255);
        private static readonly Color Border   = Color.FromArgb(226, 232, 240);
        private static readonly Color Blue     = Color.FromArgb(37,  99,  235);
        private static readonly Color BlueSoft = Color.FromArgb(239, 246, 255);
        private static readonly Color Green    = Color.FromArgb(5,   150, 105);
        private static readonly Color GreenSoft= Color.FromArgb(236, 253, 245);
        private static readonly Color Red      = Color.FromArgb(220,  38,  38);
        private static readonly Color RedSoft  = Color.FromArgb(254, 242, 242);
        private static readonly Color Amber    = Color.FromArgb(217, 119,   6);
        private static readonly Color Purple   = Color.FromArgb(99,  102, 241);
        private static readonly Color PurpleSoft = Color.FromArgb(238, 242, 255);
        private static readonly Color TxtPri   = Color.FromArgb(15,   23,  42);
        private static readonly Color TxtSec   = Color.FromArgb(71,   85, 105);
        private static readonly Color TxtMuted = Color.FromArgb(148, 163, 184);

        public MainForm(LanWebSocketClient wsClient)
        {
            _wsClient = wsClient;
            InitializeUI();
            bool alreadyConnected = wsClient.IsConnected;
            UpdateStatus(alreadyConnected ? "Connected" : "Searching for server...", alreadyConnected);
            _wsClient.StatusChanged    += s  => UpdateStatus(s, s.StartsWith("Connected"));
            _wsClient.CommandCompleted += (cmd, ok) => AddLog(cmd, ok);
        }

        // ── Live updates ──────────────────────────────────────────────────────
        private void UpdateStatus(string status, bool connected)
        {
            if (InvokeRequired) { Invoke(() => UpdateStatus(status, connected)); return; }
            _statusLabel.Text      = connected ? "Connected" : status;
            _statusLabel.ForeColor = connected ? Green : Red;
            _statusDot.ForeColor   = connected ? Green : Red;

            // Update status card accent
            _statusCard.BackColor  = connected ? GreenSoft : RedSoft;
            _statusCard?.Invalidate();

            _serverVal.Text = connected && !string.IsNullOrEmpty(_wsClient.ServerIp)
                ? $"{_wsClient.ServerIp}   ·   HTTP:{_wsClient.HttpPort}"
                : "—";
        }

        private void AddLog(string cmd, bool ok)
        {
            if (InvokeRequired) { Invoke(() => AddLog(cmd, ok)); return; }
            _commandLog.SelectionStart  = _commandLog.TextLength;
            _commandLog.SelectionColor  = TxtMuted;
            _commandLog.AppendText($"[{DateTime.Now:HH:mm:ss}]  ");
            _commandLog.SelectionColor  = ok ? Green : Red;
            _commandLog.AppendText($"{cmd}  →  {(ok ? "✓ Success" : "✗ Failed")}\n");
            _commandLog.ScrollToCaret();
        }

        // ── UI build ──────────────────────────────────────────────────────────
        private void InitializeUI()
        {
            Text            = "LanC — Client Dashboard";
            Size            = new Size(1100, 700);
            MinimumSize     = new Size(860, 540);
            StartPosition   = FormStartPosition.CenterScreen;
            WindowState     = FormWindowState.Maximized;
            BackColor       = BgApp;
            ForeColor       = TxtPri;
            Font            = new Font("Segoe UI", 9f);
            ShowInTaskbar   = true;
            FormClosing    += (_, e) => { e.Cancel = true; Hide(); };

            // Enable double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // ── Sidebar ───────────────────────────────────────────────────────
            var sidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 230,
                BackColor = BgSide
            };
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

            // Blue accent
            var accentMark = new Panel { Width = 4, Height = 28, Left = 20, Top = 22, BackColor = Blue };

            var logoTitle = new Label
            {
                Text      = "LanC",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = TxtPri,
                AutoSize  = true,
                Left      = 32, Top = 16,
                BackColor = Color.Transparent
            };
            var logoSub = new Label
            {
                Text      = "Client Dashboard",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = TxtMuted,
                AutoSize  = true,
                Left      = 32, Top = 40,
                BackColor = Color.Transparent
            };
            logoPanel.Controls.AddRange(new Control[] { accentMark, logoTitle, logoSub });

            // Status badge
            var statusBadge = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = RedSoft };
            statusBadge.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, statusBadge.Height - 1, statusBadge.Width, statusBadge.Height - 1);
            };

            // Animated dot
            var animDot = new AnimatedStatusDot(Red) { Left = 20, Top = 16 };
            _statusDot = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Red,
                AutoSize  = true,
                Left      = 20, Top = 18,
                BackColor = Color.Transparent
            };
            _statusLabel = new Label
            {
                Text      = "Searching...",
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Red,
                AutoSize  = true,
                Left      = 38, Top = 16,
                BackColor = Color.Transparent
            };
            statusBadge.Controls.AddRange(new Control[] { animDot, _statusLabel });

            // Nav label
            var navLabelPanel = new Panel { Dock = DockStyle.Top, Height = 32, BackColor = BgSide };
            navLabelPanel.Controls.Add(new Label
            {
                Text      = "NAVIGATION",
                Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = TxtMuted,
                AutoSize  = true,
                Left      = 20, Top = 12,
                BackColor = Color.Transparent
            });

            sidebar.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = BgSide }); // spacer
            sidebar.Controls.Add(navLabelPanel);
            sidebar.Controls.Add(statusBadge);
            sidebar.Controls.Add(logoPanel);

            // ── Top bar ───────────────────────────────────────────────────────
            var topBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = BgCard };
            topBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };
            var pageTitle = new Label
            {
                Text      = "Overview",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = TxtPri,
                AutoSize  = true,
                Left      = 24, Top = 16,
                BackColor = Color.Transparent
            };
            var pageSub = new Label
            {
                Text      = "Monitor this machine's connection to the LanC server.",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = TxtSec,
                AutoSize  = true,
                Left      = 24, Top = 40,
                BackColor = Color.Transparent
            };

            // Machine name badge (top-right)
            var machineBadge = new Panel
            {
                Height = 32, Width = 200, Top = 16,
                BackColor = BlueSoft
            };
            machineBadge.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, Blue.R, Blue.G, Blue.B), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, machineBadge.Width - 1, machineBadge.Height - 1);
            };
            var machineNameLbl = new Label
            {
                Text      = $"⊞  {Environment.MachineName}",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Blue,
                AutoSize  = true,
                Left      = 10, Top = 7,
                BackColor = Color.Transparent
            };
            machineBadge.Controls.Add(machineNameLbl);
            topBar.SizeChanged += (_, _) => machineBadge.Left = topBar.Width - machineBadge.Width - 24;
            topBar.Controls.AddRange(new Control[] { pageTitle, pageSub, machineBadge });

            // ── Body layout ───────────────────────────────────────────────────
            var body = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = BgApp,
                Padding     = new Padding(20, 16, 20, 20)
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 110)); // stat cards
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 148)); // info card
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // log

            // ── Row 0: Stat cards ─────────────────────────────────────────────
            var statsRow = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 1,
                ColumnCount = 3,
                BackColor   = Color.Transparent,
                Margin      = new Padding(0, 0, 0, 0)
            };
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));

            Label connVal, machVal, autoVal;
            statsRow.Controls.Add(MakeStatCard("Connection",  "Searching",                    "LanC server status",       Red,    out connVal),   0, 0);
            statsRow.Controls.Add(MakeStatCard("Machine",     Environment.MachineName,         "This computer",            Blue,   out machVal),   1, 0);
            statsRow.Controls.Add(MakeStatCard("Auto-Start",  StartupManager.IsEnabled()
                                                                ? "Enabled" : "Disabled",      "Runs on Windows startup",  Purple, out autoVal),   2, 0);

            // ── Row 1: Info card ──────────────────────────────────────────────
            _statusCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = RedSoft,
                Margin    = new Padding(0, 12, 0, 12)
            };
            _statusCard.Paint += (_, e) =>
            {
                var connected = _statusLabel.ForeColor == Green;
                var accentColor = connected ? Green : Red;
                using var pen = new Pen(Color.FromArgb(80, accentColor.R, accentColor.G, accentColor.B), 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, _statusCard.Width - 1, _statusCard.Height - 1);
                using var bar = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(bar, 0, 0, 4, _statusCard.Height);
            };

            var infoLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 1,
                ColumnCount = 3,
                BackColor   = Color.Transparent,
                Padding     = new Padding(24, 18, 24, 18)
            };
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            infoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            infoLayout.Controls.Add(MakeInfoGroup("Server Address", out _serverVal,    "—"),                        0, 0);
            infoLayout.Controls.Add(MakeInfoGroup("Machine Name",   out _machineVal,   Environment.MachineName),    1, 0);
            infoLayout.Controls.Add(MakeInfoGroup("Auto-Start",     out _autoStartVal, StartupManager.IsEnabled()
                                                                                           ? "● Enabled"
                                                                                           : "○ Disabled"),          2, 0);

            _autoStartVal.ForeColor = StartupManager.IsEnabled() ? Green : TxtMuted;

            _statusCard.Controls.Add(infoLayout);

            // ── Row 2: Command log ────────────────────────────────────────────
            var logCard = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgCard
            };
            logCard.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, logCard.Width - 1, logCard.Height - 1);
            };

            var logHdr = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = BgCard2
            };
            logHdr.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, logHdr.Height - 1, logHdr.Width, logHdr.Height - 1);
            };
            var logHdrTitle = new Label
            {
                Text      = "≡  Command History",
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Blue,
                AutoSize  = true,
                Left      = 16, Top = 13,
                BackColor = Color.Transparent
            };
            var clearBtn = new Button
            {
                Text        = "Clear",
                Font        = new Font("Segoe UI", 8f),
                BackColor   = BgCard,
                ForeColor   = TxtSec,
                FlatStyle   = FlatStyle.Flat,
                Width       = 60, Height = 26,
                Top         = 9,
                Cursor      = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Border }
            };
            clearBtn.MouseEnter += (_, _) => { clearBtn.BackColor = BgHover; clearBtn.ForeColor = Blue; };
            clearBtn.MouseLeave += (_, _) => { clearBtn.BackColor = BgCard;  clearBtn.ForeColor = TxtSec; };
            clearBtn.Click += (_, _) => _commandLog.Clear();
            logHdr.SizeChanged += (_, _) => clearBtn.Left = logHdr.Width - clearBtn.Width - 16;
            logHdr.Controls.AddRange(new Control[] { logHdrTitle, clearBtn });

            _commandLog = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = BgCard2,
                ForeColor   = TxtPri,
                Font        = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Padding     = new Padding(10)
            };

            // Empty state placeholder
            var emptyLbl = new Label
            {
                Text      = "No commands received yet.",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TxtMuted,
                AutoSize  = true,
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
            logCard.Controls.Add(logHdr);

            body.Controls.Add(statsRow,    0, 0);
            body.Controls.Add(_statusCard, 0, 1);
            body.Controls.Add(logCard,     0, 2);

            // ── Main container ────────────────────────────────────────────────
            var content = new Panel { Dock = DockStyle.Fill, BackColor = BgApp };
            content.Controls.Add(body);
            content.Controls.Add(topBar);

            Controls.Add(content);
            Controls.Add(sidebar);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Panel MakeStatCard(string title, string value, string sub, Color accent, out Label valueLabel)
        {
            var p = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = BgCard,
                Margin    = new Padding(0, 0, 12, 0)
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

            var titleLbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = TxtSec,
                AutoSize  = true,
                Left      = 18, Top = 18,
                BackColor = Color.Transparent
            };
            valueLabel = new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = TxtPri,
                AutoSize  = true,
                Left      = 18, Top = 34,
                BackColor = Color.Transparent
            };
            var subLbl = new Label
            {
                Text      = sub,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = TxtMuted,
                AutoSize  = true,
                Left      = 18, Top = 74,
                BackColor = Color.Transparent
            };
            p.Controls.AddRange(new Control[] { titleLbl, valueLabel, subLbl });
            return p;
        }

        private Panel MakeInfoGroup(string label, out Label valueLabel, string initialValue)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = TxtMuted,
                AutoSize  = true,
                Left      = 0, Top = 0,
                BackColor = Color.Transparent
            };
            valueLabel = new Label
            {
                Text      = initialValue,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = TxtSec,
                AutoSize  = true,
                Left      = 0, Top = 20,
                BackColor = Color.Transparent
            };
            p.Controls.AddRange(new Control[] { lbl, valueLabel });
            return p;
        }
    }

    // ── Animated connection status dot ────────────────────────────────────────
    internal class AnimatedStatusDot : Control
    {
        private Color _color;
        private float _scale = 1f;
        private bool _growing = false;
        private readonly System.Windows.Forms.Timer _timer;

        public AnimatedStatusDot(Color color)
        {
            _color = color;
            Width = 12; Height = 12;
            // Enable transparent background support
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            _timer = new System.Windows.Forms.Timer { Interval = 40 };
            _timer.Tick += (_, _) =>
            {
                _scale += _growing ? 0.06f : -0.06f;
                if (_scale >= 1.4f) _growing = false;
                if (_scale <= 0.7f) _growing = true;
                Invalidate();
            };
            _timer.Start();
        }

        public void SetColor(Color c) { _color = c; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int cx = Width / 2, cy = Height / 2;
            float r = 4.5f * _scale;
            using var glow = new SolidBrush(Color.FromArgb(55, _color));
            e.Graphics.FillEllipse(glow, cx - r - 2, cy - r - 2, (r + 2) * 2, (r + 2) * 2);
            using var dot = new SolidBrush(_color);
            e.Graphics.FillEllipse(dot, cx - r + 1f, cy - r + 1f, (r - 1f) * 2, (r - 1f) * 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer.Stop(); _timer.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
