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
            Width = 220;
            Dock = DockStyle.Left;
            BackColor = Theme.BgSidebar;

            // ── Logo area ─────────────────────────────────────────────────────
            var logo = new Panel { Height = 72, Dock = DockStyle.Top, BackColor = Theme.BgSidebar };
            logo.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, logo.Height - 1, logo.Width, logo.Height - 1);
            };
            var logoTitle = new Label
            {
                Text = "LanC",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 20, Top = 12
            };
            var logoSub = new Label
            {
                Text = "Server Control Panel",
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 20, Top = 38
            };
            logo.Controls.AddRange(new Control[] { logoTitle, logoSub });

            // ── Status badge ──────────────────────────────────────────────────
            var statusPanel = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Theme.BgSidebar, Padding = new Padding(20, 0, 0, 0) };
            statusPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, statusPanel.Height - 1, statusPanel.Width, statusPanel.Height - 1);
            };
            var statusDot = new Label
            {
                Text = "●",
                ForeColor = Theme.Green,
                Font = new Font("Segoe UI", 8f),
                AutoSize = true,
                Left = 20, Top = 12
            };
            var statusLbl = new Label
            {
                Text = "ONLINE",
                ForeColor = Theme.Green,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                AutoSize = true,
                Left = 34, Top = 12
            };
            statusPanel.Controls.AddRange(new Control[] { statusDot, statusLbl });

            // ── Nav items ─────────────────────────────────────────────────────
            var navFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = Theme.BgSidebar,
                Padding = new Padding(10, 8, 10, 8)
            };

            foreach (var (page, icon, label) in _nav)
            {
                var item = new SidebarItem(icon, label);
                item.Click += (_, _) => SetActive(page);
                _items[page] = item;
                navFlow.Controls.Add(item);
            }

            // ── Shutdown button ───────────────────────────────────────────────
            var shutdownPanel = new Panel { Height = 64, Dock = DockStyle.Bottom, BackColor = Theme.BgSidebar };
            shutdownPanel.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, shutdownPanel.Width, 0);
            };
            var shutdownBtn = new Button
            {
                Text = "⏻  Shutdown Server",
                BackColor = Color.FromArgb(40, 225, 29, 72),
                ForeColor = Theme.Red,
                FlatStyle = FlatStyle.Flat,
                Font = Theme.FontBold,
                Height = 36,
                Width = 180,
                Left = 20,
                Top = 14,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(80, 225, 29, 72) }
            };
            shutdownBtn.MouseEnter += (_, _) => shutdownBtn.BackColor = Color.FromArgb(70, 225, 29, 72);
            shutdownBtn.MouseLeave += (_, _) => shutdownBtn.BackColor = Color.FromArgb(40, 225, 29, 72);
            shutdownBtn.Click += (_, _) => ShutdownRequested?.Invoke();
            shutdownPanel.Controls.Add(shutdownBtn);

            Controls.Add(navFlow);
            Controls.Add(shutdownPanel);
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

    // ── Individual nav item ───────────────────────────────────────────────────
    internal class SidebarItem : Panel
    {
        private readonly Label _icon;
        private readonly Label _label;
        private bool _active;

        public SidebarItem(string icon, string label)
        {
            Height = 40;
            Width = 200;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            Margin = new Padding(0, 2, 0, 2);

            _icon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 12, Top = 10
            };
            _label = new Label
            {
                Text = label,
                Font = Theme.FontBase,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 38, Top = 12
            };

            Controls.AddRange(new Control[] { _icon, _label });

            MouseEnter += (_, _) => { if (!_active) BackColor = Theme.BgHover; };
            MouseLeave += (_, _) => { if (!_active) BackColor = Color.Transparent; };

            // Forward click from child labels
            _icon.Click  += (_, e) => OnClick(e);
            _label.Click += (_, e) => OnClick(e);
            _icon.MouseEnter  += (_, _) => { if (!_active) BackColor = Theme.BgHover; };
            _icon.MouseLeave  += (_, _) => { if (!_active) BackColor = Color.Transparent; };
            _label.MouseEnter += (_, _) => { if (!_active) BackColor = Theme.BgHover; };
            _label.MouseLeave += (_, _) => { if (!_active) BackColor = Color.Transparent; };
        }

        public void SetActive(bool active)
        {
            _active = active;
            BackColor = active ? Color.FromArgb(30, 37, 99, 235) : Color.Transparent;
            _icon.ForeColor  = active ? Theme.Blue : Theme.TextSecond;
            _label.ForeColor = active ? Theme.TextPrimary : Theme.TextSecond;

            // Left accent bar via Paint
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_active)
            {
                using var brush = new SolidBrush(Theme.Blue);
                e.Graphics.FillRectangle(brush, 0, 6, 3, Height - 12);
            }
        }
    }
}
