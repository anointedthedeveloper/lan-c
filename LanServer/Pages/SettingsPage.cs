using LanServer.Controls;

namespace LanServer.Pages
{
    public class SettingsPage : Panel
    {
        private readonly Panel _contentArea;
        private Panel? _activeSection = null;

        private static readonly (string id, string label)[] _sections =
        {
            ("general",  "General"),
            ("network",  "Network"),
        };

        public SettingsPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Settings", "Configure your LanC server.");

            // ── Layout: settings sidebar + content ────────────────────────────
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.BgApp,
                Padding = new Padding(24, 16, 24, 24)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Settings nav
            var nav = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Margin = new Padding(0, 0, 16, 0) };
            nav.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, nav.Width - 1, nav.Height - 1);
            };

            var navFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(8)
            };

            _contentArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };
            _contentArea.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _contentArea.Width - 1, _contentArea.Height - 1);
            };

            Panel? firstBtn = null;
            foreach (var (id, label) in _sections)
            {
                var btn = MakeNavBtn(label, id);
                navFlow.Controls.Add(btn);
                firstBtn ??= btn;
            }

            nav.Controls.Add(navFlow);
            layout.Controls.Add(nav, 0, 0);
            layout.Controls.Add(_contentArea, 1, 0);

            Controls.Add(layout);
            Controls.Add(header);

            // Show first section
            ShowSection("general");
        }

        private Panel MakeNavBtn(string label, string id)
        {
            var p = new Panel { Height = 38, Width = 164, BackColor = Color.Transparent, Cursor = Cursors.Hand, Margin = new Padding(0, 2, 0, 2) };
            var lbl = new Label { Text = label, Font = Theme.FontBase, ForeColor = Theme.TextSecond, AutoSize = true, Left = 14, Top = 10 };
            p.Controls.Add(lbl);
            p.Click += (_, _) => { ShowSection(id); SetNavActive(p, lbl); };
            lbl.Click += (_, _) => { ShowSection(id); SetNavActive(p, lbl); };
            p.MouseEnter += (_, _) => { if (_activeSection != p) p.BackColor = Theme.BgHover; };
            p.MouseLeave += (_, _) => { if (_activeSection != p) p.BackColor = Color.Transparent; };
            lbl.MouseEnter += (_, _) => { if (_activeSection != p) p.BackColor = Theme.BgHover; };
            lbl.MouseLeave += (_, _) => { if (_activeSection != p) p.BackColor = Color.Transparent; };
            return p;
        }

        private void SetNavActive(Panel p, Label lbl)
        {
            // Reset all
            var navFlow = (FlowLayoutPanel)p.Parent!;
            foreach (Control c in navFlow.Controls)
            {
                c.BackColor = Color.Transparent;
                if (c.Controls.Count > 0) c.Controls[0].ForeColor = Theme.TextSecond;
            }
            p.BackColor = Color.FromArgb(30, 37, 99, 235);
            lbl.ForeColor = Theme.TextPrimary;
        }

        private void ShowSection(string id)
        {
            _contentArea.Controls.Clear();
            Panel section = id switch
            {
                "network" => BuildNetworkSection(),
                _         => BuildGeneralSection()
            };
            section.Dock = DockStyle.Fill;
            _contentArea.Controls.Add(section);
        }

        private static Panel BuildGeneralSection()
        {
            var p = SectionPanel();
            int y = 24;

            AddSectionTitle(p, "General", ref y);
            AddField(p, "Server Name", "LanC Server", "Display name for this server.", ref y);

            var autoStartChk = new CheckBox
            {
                Text = "Start server automatically on launch",
                Checked = true,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBase,
                BackColor = Color.Transparent,
                AutoSize = true,
                Left = 24, Top = y
            };
            autoStartChk.FlatStyle = FlatStyle.Flat;
            autoStartChk.FlatAppearance.BorderColor = Theme.Blue;
            autoStartChk.FlatAppearance.CheckedBackColor = Theme.Blue;
            p.Controls.Add(autoStartChk);
            y += 36;

            AddSaveButton(p, y, () =>
            {
                ToastManager.Show("Settings saved.", ToastKind.Success);
            });

            return p;
        }

        private static Panel BuildNetworkSection()
        {
            var p = SectionPanel();
            int y = 24;

            AddSectionTitle(p, "Network", ref y);

            var wsPortBox = AddField(p, "WebSocket Port", Config.Current.WebSocketPort.ToString(), "Port used for real-time client communication.", ref y);
            var httpPortBox = AddField(p, "HTTP Port", Config.Current.HttpPort.ToString(), "Port used for file serving and web access.", ref y);
            var udpPortBox = AddField(p, "UDP Beacon Port", Config.Current.UdpPort.ToString(), "Port used for LAN auto-discovery broadcasts.", ref y);

            AddSaveButton(p, y, () =>
            {
                if (!int.TryParse(wsPortBox.Text, out int ws) || ws < 1024 || ws > 65535)
                { ToastManager.Show("Invalid WebSocket port.", ToastKind.Error); return; }
                if (!int.TryParse(httpPortBox.Text, out int http) || http < 1024 || http > 65535)
                { ToastManager.Show("Invalid HTTP port.", ToastKind.Error); return; }
                if (!int.TryParse(udpPortBox.Text, out int udp) || udp < 1024 || udp > 65535)
                { ToastManager.Show("Invalid UDP port.", ToastKind.Error); return; }

                Config.Current.WebSocketPort = ws;
                Config.Current.HttpPort      = http;
                Config.Current.UdpPort       = udp;
                Config.Save();
                AppState.Log("Network settings saved. Restart required to apply port changes.", LogLevel.Warning);
                ToastManager.Show("Network settings saved. Restart to apply.", ToastKind.Warning);
            });

            return p;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Panel SectionPanel() => new() { BackColor = Theme.BgCard, AutoScroll = true };

        private static void AddSectionTitle(Panel p, string title, ref int y)
        {
            var lbl = new Label { Text = title, Font = Theme.FontLg, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 24, Top = y };
            var sep = new Panel { Left = 24, Top = y + 32, Width = 500, Height = 1, BackColor = Theme.Border };
            p.Controls.AddRange(new Control[] { lbl, sep });
            y += 48;
        }

        private static TextBox AddField(Panel p, string label, string value, string hint, ref int y, bool password = false)
        {
            var lbl = new Label { Text = label, Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 24, Top = y };
            var box = Theme.MakeInput();
            box.Text = value;
            box.Left = 24; box.Top = y + 20; box.Width = 320;
            if (password) box.PasswordChar = '●';
            var hintLbl = new Label { Text = hint, Font = Theme.FontSm, ForeColor = Theme.TextMuted, AutoSize = true, Left = 24, Top = y + 56 };
            p.Controls.AddRange(new Control[] { lbl, box, hintLbl });
            y += 84;
            return box;
        }

        private static void AddSaveButton(Panel p, int y, Action onSave)
        {
            var btn = Theme.MakeBtn("Save Changes", Theme.Blue);
            btn.Left = 24; btn.Top = y + 16; btn.Width = 160;
            btn.Click += (_, _) => onSave();
            p.Controls.Add(btn);
        }
    }
}
