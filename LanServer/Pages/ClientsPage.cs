using LanServer.Controls;

namespace LanServer.Pages
{
    public class ClientsPage : Panel
    {
        private readonly DarkListView _table;
        private readonly Panel _emptyPanel;
        private readonly Panel _tablePanel;
        private readonly Label _onlineLbl;
        private readonly Label _offlineLbl;
        private readonly Label _totalLbl;

        public ClientsPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Connected Clients", "Monitor devices connected to your LanC server.");

            // ── Stats strip ───────────────────────────────────────────────────
            var statsStrip = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.BgCard, Padding = new Padding(24, 0, 24, 0) };
            statsStrip.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, statsStrip.Height - 1, statsStrip.Width, statsStrip.Height - 1);
            };

            _totalLbl   = MakePill("Total: 0",   Theme.TextSecond, 24);
            _onlineLbl  = MakePill("Online: 0",  Theme.Green,      120);
            _offlineLbl = MakePill("Offline: 0", Theme.TextMuted,  210);

            var shutdownSelBtn = Theme.MakeBtn("⏻  Shutdown Selected", Theme.RedMuted);
            shutdownSelBtn.AutoSize = true;
            shutdownSelBtn.Padding = new Padding(12, 0, 12, 0);
            shutdownSelBtn.Top = 9;
            shutdownSelBtn.Click += ShutdownSelected_Click;
            statsStrip.SizeChanged += (_, _) => shutdownSelBtn.Left = statsStrip.Width - shutdownSelBtn.Width - 24;
            statsStrip.Controls.AddRange(new Control[] { _totalLbl, _onlineLbl, _offlineLbl, shutdownSelBtn });

            // ── Table ─────────────────────────────────────────────────────────
            _tablePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp, Padding = new Padding(24, 16, 24, 24) };
            _table = new DarkListView { Dock = DockStyle.Fill, MultiSelect = true };
            _table.Columns.Add("Computer",    160);
            _table.Columns.Add("IP Address",  130);
            _table.Columns.Add("Status",       90);
            _table.Columns.Add("Last Seen",   110);
            _tablePanel.Controls.Add(_table);

            // ── Empty state ───────────────────────────────────────────────────
            _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };
            _emptyPanel.Controls.Add(new EmptyState("◉", "No connected clients", "Clients will appear here when they connect to the server."));

            Controls.Add(_emptyPanel);
            Controls.Add(_tablePanel);
            Controls.Add(statsStrip);
            Controls.Add(header);

            ClientManager.ClientsChanged += Refresh;
            Refresh();
        }

        private new void Refresh()
        {
            if (InvokeRequired) { Invoke(Refresh); return; }
            var all     = ClientManager.GetAll().ToList();
            var online  = all.Count(c => c.IsOnline);
            var offline = all.Count - online;

            _totalLbl.Text   = $"Total: {all.Count}";
            _onlineLbl.Text  = $"Online: {online}";
            _offlineLbl.Text = $"Offline: {offline}";

            bool hasData = all.Count > 0;
            _tablePanel.Visible = hasData;
            _emptyPanel.Visible = !hasData;
            if (!hasData) return;

            _table.Items.Clear();
            foreach (var c in all.OrderByDescending(x => x.IsOnline).ThenBy(x => x.ComputerName))
            {
                var item = _table.AddRow(
                    Theme.BgCard, Theme.TextPrimary,
                    c.ComputerName,
                    c.IpAddress,
                    c.IsOnline ? "● Online" : "○ Offline",
                    c.LastSeen.ToString("HH:mm:ss")
                );
                item.Tag = c.Id;
                item.SubItems[2].ForeColor = c.IsOnline ? Theme.Green : Theme.TextMuted;
            }
        }

        private void ShutdownSelected_Click(object? s, EventArgs e)
        {
            var ids = _table.SelectedItems.Count > 0
                ? _table.SelectedItems.Cast<ListViewItem>().Select(i => i.Tag?.ToString() ?? "").Where(id => !string.IsNullOrEmpty(id)).ToList()
                : ClientManager.GetOnline().Select(c => c.Id).ToList();

            if (!ids.Any()) { ToastManager.Show("No clients selected or connected.", ToastKind.Warning); return; }

            if (!ConfirmDialog.Ask(FindForm(), "Shutdown Machines?",
                $"This will shut down {ids.Count} machine(s). This cannot be undone.",
                "Shutdown", danger: true)) return;

            CommandDispatcher.IssueShutdown(ids);
            AppState.Log($"Shutdown issued → {ids.Count} client(s)", LogLevel.Warning);
            ToastManager.Show($"Shutdown sent to {ids.Count} client(s).", ToastKind.Warning);
        }

        private static Label MakePill(string text, Color color, int left) => new()
        {
            Text = text,
            Font = Theme.FontSm,
            ForeColor = color,
            AutoSize = true,
            Left = left,
            Top = 18,
            BackColor = Color.Transparent
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClientManager.ClientsChanged -= Refresh;
            base.Dispose(disposing);
        }
    }
}
