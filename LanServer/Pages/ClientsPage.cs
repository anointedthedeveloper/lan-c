using LanServer.Controls;

namespace LanServer.Pages
{
    public class ClientsPage : Panel
    {
        private readonly Panel          _emptyPanel;
        private readonly Panel          _tablePanel;
        private readonly Label          _onlineLbl;
        private readonly Label          _offlineLbl;
        private readonly Label          _totalLbl;
        private readonly CheckBox       _selectAllChk;
        private bool _suppressCheckEvents = false;

        public ClientsPage()
        {
            Dock      = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Connected Clients", "Monitor devices connected to your LanC server.");

            // ── Stats strip ───────────────────────────────────────────────────
            var statsStrip = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Theme.BgCard,
                Padding   = new Padding(24, 0, 24, 0)
            };
            statsStrip.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, statsStrip.Height - 1, statsStrip.Width, statsStrip.Height - 1);
            };

            _totalLbl   = MakePill("Total: 0",   Theme.TextSecond, 0);
            _onlineLbl  = MakePill("Online: 0",  Theme.Green,      96);
            _offlineLbl = MakePill("Offline: 0", Theme.TextMuted,  192);
            statsStrip.Controls.AddRange(new Control[] { _totalLbl, _onlineLbl, _offlineLbl });

            var shutdownSelBtn = Theme.MakeBtn("⏻  Shutdown Selected", Theme.Red);
            shutdownSelBtn.Width  = 190;
            shutdownSelBtn.Height = 34;
            shutdownSelBtn.Top    = 11;
            shutdownSelBtn.Click += ShutdownSelected_Click;
            statsStrip.SizeChanged += (_, _) => shutdownSelBtn.Left = statsStrip.Width - shutdownSelBtn.Width - 24;
            statsStrip.Controls.Add(shutdownSelBtn);

            // ── Table panel ───────────────────────────────────────────────────
            _tablePanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.BgApp,
                Padding   = new Padding(24, 16, 24, 24)
            };

            // Header row with Select All checkbox
            var listHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 34,
                BackColor = Theme.BgCard2
            };
            listHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, listHeader.Height - 1, listHeader.Width, listHeader.Height - 1);
            };

            _selectAllChk = new CheckBox
            {
                Text      = "Select All",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent,
                AutoSize  = true,
                Left      = 10,
                Top       = 8,
                Cursor    = Cursors.Hand
            };
            _selectAllChk.CheckedChanged += SelectAll_CheckedChanged;

            var hdrComputer = MakeColHeader("Computer",   200);
            var hdrIp       = MakeColHeader("IP Address", 160);
            var hdrStatus   = MakeColHeader("Status",     100);
            var hdrSeen     = MakeColHeader("Last Seen",  120);
            var hdrId       = MakeColHeader("Device ID",  -1); // fills remaining

            listHeader.Controls.AddRange(new Control[] { _selectAllChk, hdrComputer, hdrIp, hdrStatus, hdrSeen, hdrId });
            listHeader.SizeChanged += (_, _) => LayoutHeaderCols(listHeader, hdrComputer, hdrIp, hdrStatus, hdrSeen, hdrId);

            // Custom owner-drawn list using a Panel + FlowLayout for checkboxes + rows
            var listContainer = new Panel
            {
                Dock        = DockStyle.Fill,
                BackColor   = Theme.BgCard,
                AutoScroll  = true,
                BorderStyle = BorderStyle.None
            };
            listContainer.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, listContainer.Width - 1, listContainer.Height - 1);
            };

            // We use a DarkListView with checkbox column workaround via owner-draw
            // Actually use ListView with CheckBoxes = true for proper checkbox support
            var listView = new ClientListView
            {
                Dock           = DockStyle.Fill,
                BackColor      = Theme.BgCard,
                ForeColor      = Theme.TextPrimary,
                Font           = Theme.FontBase,
                FullRowSelect  = true,
                GridLines      = false,
                MultiSelect    = true,
                BorderStyle    = BorderStyle.None,
                HeaderStyle    = ColumnHeaderStyle.Nonclickable,
                CheckBoxes     = true,
                View           = View.Details,
                OwnerDraw      = false  // Let OS draw checkboxes natively
            };
            // Supress header checkbox (we have our own Select All)
            listView.Columns.Add("Computer",   200);
            listView.Columns.Add("IP Address", 160);
            listView.Columns.Add("Status",     100);
            listView.Columns.Add("Last Seen",  130);
            listView.Columns.Add("Device ID",   -2);

            // Sync select-all checkbox state
            listView.ItemChecked += (_, __) =>
            {
                if (_suppressCheckEvents) return;
                _suppressCheckEvents = true;
                bool allChecked = listView.Items.Count > 0 && listView.Items.Cast<ListViewItem>().All(i => i.Checked);
                bool noneChecked = listView.Items.Cast<ListViewItem>().All(i => !i.Checked);
                _selectAllChk.CheckState = allChecked ? CheckState.Checked
                                         : noneChecked ? CheckState.Unchecked
                                         : CheckState.Indeterminate;
                _suppressCheckEvents = false;
            };

            // Store reference for actions
            _listView    = listView;

            listContainer.Controls.Add(listView);

            _tablePanel.Controls.Add(listContainer);
            _tablePanel.Controls.Add(listHeader);

            // ── Empty state ───────────────────────────────────────────────────
            _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };
            _emptyPanel.Controls.Add(new EmptyState(
                "◉",
                "No connected clients",
                "Clients will appear here once they connect to the LanC server."));

            Controls.Add(_emptyPanel);
            Controls.Add(_tablePanel);
            Controls.Add(statsStrip);
            Controls.Add(header);

            ClientManager.ClientsChanged += Refresh;
            Refresh();
        }

        private ListView _listView = null!;

        private void SelectAll_CheckedChanged(object? s, EventArgs e)
        {
            if (_suppressCheckEvents) return;
            _suppressCheckEvents = true;
            bool check = _selectAllChk.Checked;
            foreach (ListViewItem item in _listView.Items)
                item.Checked = check;
            _suppressCheckEvents = false;
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

            // Preserve checked state by deviceId
            var checkedIds = _listView.Items.Cast<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => i.Tag?.ToString() ?? "")
                .ToHashSet();

            _suppressCheckEvents = true;
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (var c in all.OrderByDescending(x => x.IsOnline).ThenBy(x => x.ComputerName))
            {
                var item = new ListViewItem(c.ComputerName) { Tag = c.Id };
                item.SubItems.Add(c.IpAddress);
                item.SubItems.Add(c.IsOnline ? "● Online" : "○ Offline");
                item.SubItems.Add(c.LastSeen.ToString("HH:mm:ss"));
                item.SubItems.Add(c.Id);  // device ID column

                if (!c.IsOnline)
                {
                    item.ForeColor = Theme.TextMuted;
                    item.SubItems[2].ForeColor = Theme.TextMuted;
                }
                else
                {
                    item.SubItems[2].ForeColor = Theme.Green;
                }

                item.Checked = checkedIds.Contains(c.Id);
                _listView.Items.Add(item);
            }
            _listView.EndUpdate();
            _suppressCheckEvents = false;

            // Sync select-all
            bool allC = _listView.Items.Count > 0 && _listView.Items.Cast<ListViewItem>().All(i => i.Checked);
            bool noneC = _listView.Items.Cast<ListViewItem>().All(i => !i.Checked);
            _selectAllChk.CheckState = allC ? CheckState.Checked : noneC ? CheckState.Unchecked : CheckState.Indeterminate;
        }

        private void ShutdownSelected_Click(object? s, EventArgs e)
        {
            // Use checked items if any, else fall back to all online
            var checkedIds = _listView.Items.Cast<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => i.Tag?.ToString() ?? "")
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            var ids = checkedIds.Any()
                ? checkedIds
                : ClientManager.GetOnline().Select(c => c.Id).ToList();

            if (!ids.Any()) { ToastManager.Show("No clients selected or connected.", ToastKind.Warning); return; }

            if (!ConfirmDialog.Ask(FindForm()!,
                "Shutdown Machines?",
                $"This will shut down {ids.Count} machine(s). This cannot be undone.",
                "Shutdown", danger: true)) return;

            CommandDispatcher.IssueShutdown(ids);
            AppState.Log($"Shutdown issued → {ids.Count} client(s)", LogLevel.Warning);
            ToastManager.Show($"Shutdown sent to {ids.Count} client(s).", ToastKind.Warning);
        }

        // ── Column header helpers ─────────────────────────────────────────────
        private static Label MakeColHeader(string text, int width) => new()
        {
            Text      = text,
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Theme.TextSecond,
            AutoSize  = false,
            Height    = 34,
            Width     = width < 0 ? 200 : width,
            Top       = 0,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            Padding   = new Padding(2, 0, 0, 0)
        };

        private static void LayoutHeaderCols(Panel header, Label comp, Label ip, Label status, Label seen, Label id)
        {
            // 36px for the checkbox
            int x = 36;
            comp.Left = x;   comp.Width = 200; x += 200;
            ip.Left   = x;   ip.Width   = 160; x += 160;
            status.Left = x; status.Width = 100; x += 100;
            seen.Left = x;   seen.Width  = 130; x += 130;
            id.Left   = x;   id.Width    = Math.Max(80, header.Width - x - 10);
        }

        private static Label MakePill(string text, Color color, int left) => new()
        {
            Text      = text,
            Font      = Theme.FontSm,
            ForeColor = color,
            AutoSize  = true,
            Left      = left,
            Top       = 19,
            BackColor = Color.Transparent
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClientManager.ClientsChanged -= Refresh;
            base.Dispose(disposing);
        }
    }

    // Simple styled ListView — overrides colors
    internal class ClientListView : ListView
    {
        private int _hoveredIndex = -1;

        public ClientListView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            MouseMove += (_, e) =>
            {
                var item = GetItemAt(e.X, e.Y);
                int idx = item?.Index ?? -1;
                if (idx != _hoveredIndex) { _hoveredIndex = idx; Invalidate(); }
            };
            MouseLeave += (_, _) =>
            {
                if (_hoveredIndex != -1) { _hoveredIndex = -1; Invalidate(); }
            };
        }
    }
}
