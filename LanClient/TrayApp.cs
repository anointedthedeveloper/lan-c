using System.Reflection;

namespace LanClient
{
    public class TrayApp : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private MainForm? _mainForm;
        private readonly LanWebSocketClient _wsClient = new();
        private readonly ServerDiscovery _discovery   = new();
        private bool _connected;
        private Icon? _appIcon;

        public TrayApp()
        {
            _appIcon = LoadIcon();

            _tray = new NotifyIcon
            {
                Icon    = _appIcon ?? SystemIcons.Application,
                Visible = true,
                Text    = "LanC Client — Searching for server..."
            };

            // ── Context menu ──────────────────────────────────────────────────
            var menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(255, 255, 255),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font      = new Font("Segoe UI", 9f),
                Renderer  = new DarkMenuRenderer()
            };

            var openItem = new ToolStripMenuItem("Open Dashboard")
            {
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Image     = null
            };
            openItem.Click += (_, _) => ShowDashboard();

            var statusItem = new ToolStripMenuItem("Status: Searching...")
            {
                Enabled   = false,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font      = new Font("Segoe UI", 8.5f)
            };

            var sep = new ToolStripSeparator();

            var exitItem = new ToolStripMenuItem("Exit LanC Client")
            {
                ForeColor = Color.FromArgb(220, 38, 38),
                Font      = new Font("Segoe UI", 9f)
            };
            exitItem.Click += (_, _) => ExitApp();

            menu.Items.AddRange(new ToolStripItem[] { openItem, statusItem, sep, exitItem });
            _tray.ContextMenuStrip = menu;

            // Single click or double click both open the dashboard
            _tray.Click       += (_, _) => ShowDashboard();
            _tray.DoubleClick += (_, _) => ShowDashboard();

            // ── Wire up client ────────────────────────────────────────────────
            _wsClient.StatusChanged += status =>
            {
                var connected = status.StartsWith("Connected");
                _connected = connected;

                // Update tray tooltip and icon on UI thread
                if (Application.OpenForms.Count > 0)
                    Application.OpenForms[0]?.Invoke(() => UpdateTrayState(status, connected, statusItem));
                else
                    UpdateTrayState(status, connected, statusItem);

                if (!connected) _discovery.Start();
            };

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

        private void UpdateTrayState(string status, bool connected, ToolStripMenuItem statusItem)
        {
            var displayStatus = connected ? "Connected" : status;
            _tray.Text = $"LanC Client — {displayStatus}";
            _tray.Icon = connected ? (_appIcon ?? SystemIcons.Information) : (_appIcon ?? SystemIcons.Application);
            statusItem.Text      = $"Status: {displayStatus}";
            statusItem.ForeColor = connected
                ? Color.FromArgb(5, 150, 105)
                : Color.FromArgb(148, 163, 184);
        }

        private void ShowDashboard()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
            {
                _mainForm = new MainForm(_wsClient);
                if (_appIcon != null) _mainForm.Icon = _appIcon;
            }

            if (_mainForm.WindowState == FormWindowState.Minimized)
                _mainForm.WindowState = FormWindowState.Maximized;

            _mainForm.Show();
            _mainForm.Activate();
            _mainForm.BringToFront();
        }

        private void ExitApp()
        {
            _wsClient.Disconnect();
            _discovery.Stop();
            _tray.Visible = false;
            Application.Exit();
        }

        private static Icon? LoadIcon()
        {
            try
            {
                var asm  = Assembly.GetExecutingAssembly();
                var name = asm.GetManifestResourceNames()
                              .FirstOrDefault(n => n.EndsWith("client.ico", StringComparison.OrdinalIgnoreCase));
                if (name != null)
                {
                    using var stream = asm.GetManifestResourceStream(name)!;
                    return new Icon(stream);
                }
            }
            catch { }
            return null;
        }
    }

    // ── Light context menu renderer ───────────────────────────────────────────
    internal class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color BgLight   = Color.FromArgb(255, 255, 255);
        private static readonly Color BgHover   = Color.FromArgb(239, 246, 255);
        private static readonly Color BorderClr = Color.FromArgb(226, 232, 240);
        private static readonly Color BlueAccent= Color.FromArgb(37,  99, 235);

        public DarkMenuRenderer() : base(new LightColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var item = e.Item;
            var g    = e.Graphics;
            if (!item.Enabled) return;
            if (item.Selected)
            {
                using var brush = new SolidBrush(BgHover);
                g.FillRectangle(brush, new Rectangle(2, 1, item.Width - 4, item.Height - 2));
                using var pen = new Pen(Color.FromArgb(80, BlueAccent.R, BlueAccent.G, BlueAccent.B));
                g.DrawRectangle(pen, new Rectangle(2, 1, item.Width - 5, item.Height - 3));
            }
            else
            {
                using var brush = new SolidBrush(BgLight);
                g.FillRectangle(brush, new Rectangle(0, 0, item.Width, item.Height));
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(BgLight);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(BorderClr, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using var pen = new Pen(BorderClr, 1);
            e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled
                ? (e.Item.Selected ? BlueAccent : e.Item.ForeColor)
                : Color.FromArgb(148, 163, 184);
            base.OnRenderItemText(e);
        }
    }

    internal class LightColorTable : ProfessionalColorTable
    {
        private static readonly Color Bg = Color.FromArgb(255, 255, 255);
        public override Color MenuBorder                       => Color.FromArgb(226, 232, 240);
        public override Color MenuItemBorder                   => Color.Transparent;
        public override Color MenuItemSelected                 => Color.FromArgb(239, 246, 255);
        public override Color MenuItemSelectedGradientBegin    => Color.FromArgb(239, 246, 255);
        public override Color MenuItemSelectedGradientEnd      => Color.FromArgb(239, 246, 255);
        public override Color ToolStripDropDownBackground      => Bg;
        public override Color ImageMarginGradientBegin         => Bg;
        public override Color ImageMarginGradientMiddle        => Bg;
        public override Color ImageMarginGradientEnd           => Bg;
        public override Color SeparatorDark                    => Color.FromArgb(226, 232, 240);
        public override Color SeparatorLight                   => Color.FromArgb(226, 232, 240);
    }
}
