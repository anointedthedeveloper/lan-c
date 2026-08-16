namespace LanServer.Controls
{
    public enum NavPage { Dashboard, Deployments, FileManager, Clients, Activity, Settings }

    public class Sidebar : Panel
    {
        public event Action<NavPage>? Navigate;
        public event Action? ShutdownRequested;

        private NavPage _active = NavPage.Dashboard;
        private readonly Dictionary<NavPage, SidebarItem> _items = new();

        private static readonly (NavPage page, string icon, string label)[] _nav =
        {
            (NavPage.Dashboard,    "⊞", "Dashboard"),
            (NavPage.Deployments,  "⬡", "Deployments"),
            (NavPage.FileManager,  "▤", "File Manager"),
            (NavPage.Clients,      "◉", "Clients"),
            (NavPage.Activity,     "≡", "Activity"),
            (NavPage.Settings,     "⚙", "Settings"),
        };

        public Sidebar()
        {
            Width = 230;
            Dock = DockStyle.Left;
            BackColor = Theme.BgSidebar;

            Paint += (_, e) =>
            {
                // Right border shadow
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
            };

            // ── Logo area ─────────────────────────────────────────────────────
            var logo = new Panel { Height = 72, Dock = DockStyle.Top, BackColor = Theme.BgSidebar };
            logo.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, logo.Height - 1, logo.Width, logo.Height - 1);
            };

            // Blue accent mark
            var accentMark = new Panel
            {
                Width = 4, Height = 28,
                Left = 20, Top = 22,
                BackColor = Theme.Blue
            };

            var logoTitle = new Label
            {
                Text = "LanC",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 32, Top = 16
            };
            var logoSub = new Label
            {
                Text = "Server Control Panel",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Left = 32, Top = 40
            };
            logo.Controls.AddRange(new Control[] { accentMark, logoTitle, logoSub });

            // ── Status badge ──────────────────────────────────────────────────
            var statusPanel = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = Theme.GreenSoft, Padding = new Padding(20, 0, 0, 0) };
            statusPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, statusPanel.Height - 1, statusPanel.Width, statusPanel.Height - 1);
            };

            var statusDot = new AnimatedDot(Theme.Green) { Left = 20, Top = 17 };
            var statusLbl = new Label
            {
                Text = "SERVER ONLINE",
                ForeColor = Theme.Green,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                AutoSize = true,
                Left = 38, Top = 15,
                BackColor = Color.Transparent
            };
            statusPanel.Controls.AddRange(new Control[] { statusDot, statusLbl });

            // ── Nav label ──────────────────────────────────────────────────────
            var navLabel = new Panel { Height = 32, Dock = DockStyle.Top, BackColor = Theme.BgSidebar };
            var navLabelTxt = new Label
            {
                Text = "NAVIGATION",
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Left = 20, Top = 12,
                BackColor = Color.Transparent
            };
            navLabel.Controls.Add(navLabelTxt);

            // ── Nav items ─────────────────────────────────────────────────────
            var navFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Theme.BgSidebar,
                Padding = new Padding(10, 4, 10, 8)
            };

            foreach (var (page, icon, label) in _nav)
            {
                var item = new SidebarItem(icon, label);
                item.Click += (_, _) => SetActive(page);
                _items[page] = item;
                navFlow.Controls.Add(item);
            }

            // ── Shutdown button ───────────────────────────────────────────────
            var shutdownPanel = new Panel { Height = 68, Dock = DockStyle.Bottom, BackColor = Theme.BgSidebar };
            shutdownPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, shutdownPanel.Width, 0);
            };
            var shutdownBtn = new Button
            {
                Text = "⏻  Shutdown Server",
                BackColor = Theme.RedSoft,
                ForeColor = Theme.Red,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontBold,
                Height = 36,
                Width = 190,
                Left = 20,
                Top = 16,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(254, 202, 202) }
            };
            shutdownBtn.MouseEnter += (_, _) => { shutdownBtn.BackColor = Color.FromArgb(254, 226, 226); };
            shutdownBtn.MouseLeave += (_, _) => { shutdownBtn.BackColor = Theme.RedSoft; };
            shutdownBtn.Click += (_, _) => ShutdownRequested?.Invoke();
            shutdownPanel.Controls.Add(shutdownBtn);

            Controls.Add(navFlow);
            Controls.Add(shutdownPanel);
            Controls.Add(navLabel);
            Controls.Add(statusPanel);
            Controls.Add(logo);

            SetActive(NavPage.Dashboard);
        }

        public void SetActive(NavPage page)
        {
            _active = page;
            foreach (var kv in _items)
                kv.Value.SetActive(kv.Key == page);
            Navigate?.Invoke(page);
        }
    }

    // ── Animated status dot ───────────────────────────────────────────────────
    internal class AnimatedDot : Control
    {
        private readonly Color _color;
        private float _scale = 1f;
        private bool _growing = false;
        private readonly System.Windows.Forms.Timer _timer;

        public AnimatedDot(Color color)
        {
            _color = color;
            Width = 10; Height = 10;
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;

            _timer = new System.Windows.Forms.Timer { Interval = 40 };
            _timer.Tick += (_, _) =>
            {
                _scale += _growing ? 0.05f : -0.05f;
                if (_scale >= 1.3f) _growing = false;
                if (_scale <= 0.7f) _growing = true;
                Invalidate();
            };
            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int cx = Width / 2, cy = Height / 2;
            float r = 4f * _scale;
            // outer glow
            using var glowBrush = new SolidBrush(Color.FromArgb(60, _color));
            e.Graphics.FillEllipse(glowBrush, cx - r - 2, cy - r - 2, (r + 2) * 2, (r + 2) * 2);
            // inner dot
            using var dotBrush = new SolidBrush(_color);
            e.Graphics.FillEllipse(dotBrush, cx - r + 1, cy - r + 1, (r - 1) * 2, (r - 1) * 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer.Stop(); _timer.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // ── Individual nav item ───────────────────────────────────────────────────
    internal class SidebarItem : Panel
    {
        private readonly Label _icon;
        private readonly Label _label;
        private bool _active;

        public SidebarItem(string icon, string label)
        {
            Height = 42;
            Width = 210;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 2, 0, 2);

            // Rounded pill background panel
            _icon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 14, Top = 11,
                BackColor = Color.Transparent
            };
            _label = new Label
            {
                Text = label,
                Font = Theme.FontBase,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 42, Top = 13,
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { _icon, _label });

            MouseEnter += OnHoverEnter;
            MouseLeave += OnHoverLeave;

            _icon.Click  += (_, e) => OnClick(e);
            _label.Click += (_, e) => OnClick(e);
            _icon.MouseEnter  += OnHoverEnter;
            _icon.MouseLeave  += OnHoverLeave;
            _label.MouseEnter += OnHoverEnter;
            _label.MouseLeave += OnHoverLeave;
        }

        private void OnHoverEnter(object? s, EventArgs e)
        {
            if (!_active) BackColor = Theme.BgHover;
        }

        private void OnHoverLeave(object? s, EventArgs e)
        {
            if (!_active) BackColor = Color.Transparent;
        }

        public void SetActive(bool active)
        {
            _active = active;
            BackColor = active ? Theme.BlueSoft : Color.Transparent;
            _icon.ForeColor  = active ? Theme.Blue : Theme.TextSecond;
            _label.ForeColor = active ? Theme.Blue : Theme.TextSecond;
            if (active)
            {
                _label.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            else
            {
                _label.Font = Theme.FontBase;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_active)
            {
                using var brush = new SolidBrush(Theme.Blue);
                // Left rounded accent
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillRectangle(brush, 0, 8, 3, Height - 16);
            }
        }
    }
}
