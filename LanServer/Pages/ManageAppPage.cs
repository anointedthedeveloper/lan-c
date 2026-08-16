using LanServer.Controls;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanServer.Pages
{
    /// <summary>
    /// Manage App page — upload a new LanClient.exe version and push update/uninstall
    /// to all connected clients. Shows each client's installed version.
    /// </summary>
    public class ManageAppPage : Panel
    {
        private readonly ListView   _clientList;
        private readonly Label      _countLbl;
        private readonly Label      _versionLbl;
        private string?             _pendingUpdatePath;
        private readonly Label      _pendingLbl;
        private readonly Button     _pushUpdateBtn;
        private readonly Button     _uninstallBtn;

        public ManageAppPage()
        {
            Dock      = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Manage App",
                "Push LanC Client updates and manage installation on connected devices.");

            // ── Info banner ───────────────────────────────────────────────────
            var banner = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Theme.BlueSoft };
            banner.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, banner.Height - 1, banner.Width, banner.Height - 1);
                using var bar = new SolidBrush(Theme.Blue);
                e.Graphics.FillRectangle(bar, 0, 0, 3, banner.Height);
            };
            var bannerLbl = new Label
            {
                Text = "ℹ  Upload a new LanClient.exe to push updates to all devices. Use Uninstall to remove the app remotely.",
                Font = Theme.FontSm, ForeColor = Theme.Blue,
                AutoSize = false, Left = 16, Top = 14, Width = 800, Height = 20,
                BackColor = Color.Transparent
            };
            banner.Controls.Add(bannerLbl);

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top, Height = 56,
                BackColor = Theme.BgCard, Padding = new Padding(24, 10, 24, 10)
            };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            _countLbl = new Label
            {
                Text = "0 devices", Font = Theme.FontSm, ForeColor = Theme.TextSecond,
                AutoSize = true, Top = 19, Left = 24, BackColor = Color.Transparent
            };

            _versionLbl = new Label
            {
                Text = $"Server expects: v{GetExpectedVersion()}",
                Font = Theme.FontSm, ForeColor = Theme.Blue,
                AutoSize = true, Top = 19, Left = 110, BackColor = Color.Transparent
            };

            var uploadBtn = Theme.MakeOutlineBtn("↑  Select Update EXE", Theme.Blue);
            uploadBtn.AutoSize = true; uploadBtn.Padding = new Padding(14, 0, 14, 0); uploadBtn.Top = 11;
            uploadBtn.Click += SelectUpdate_Click;

            toolbar.SizeChanged += (_, _) => uploadBtn.Left = toolbar.Width - uploadBtn.Width - 24;
            toolbar.Controls.AddRange(new Control[] { _countLbl, _versionLbl, uploadBtn });

            // ── Pending update bar ────────────────────────────────────────────
            var pendingBar = new Panel
            {
                Dock = DockStyle.Top, Height = 52,
                BackColor = Theme.AmberSoft, Visible = false
            };
            pendingBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, pendingBar.Height - 1, pendingBar.Width, pendingBar.Height - 1);
                using var bar2 = new SolidBrush(Theme.Amber);
                e.Graphics.FillRectangle(bar2, 0, 0, 4, pendingBar.Height);
            };
            _pendingLbl = new Label
            {
                Text = "No file selected", Font = Theme.FontSm, ForeColor = Theme.Amber,
                AutoSize = true, Left = 16, Top = 17, BackColor = Color.Transparent
            };
            _pushUpdateBtn = Theme.MakeBtn("▶  Push Update to All Online", Theme.Green);
            _pushUpdateBtn.Width = 230; _pushUpdateBtn.Top = 9; _pushUpdateBtn.Left = 500;
            _pushUpdateBtn.Click += PushUpdate_Click;

            var clearPendingBtn = Theme.MakeOutlineBtn("✕  Clear", Theme.Amber);
            clearPendingBtn.Width = 80; clearPendingBtn.Top = 9;
            clearPendingBtn.Click += (_, _) => { _pendingUpdatePath = null; pendingBar.Visible = false; };

            pendingBar.SizeChanged += (_, _) =>
            {
                clearPendingBtn.Left = pendingBar.Width - clearPendingBtn.Width - 16;
                _pushUpdateBtn.Left  = clearPendingBtn.Left - _pushUpdateBtn.Width - 10;
            };
            pendingBar.Controls.AddRange(new Control[] { _pendingLbl, _pushUpdateBtn, clearPendingBtn });

            // ── Client table panel ────────────────────────────────────────────
            var tablePanel = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Theme.BgApp, Padding = new Padding(24, 16, 24, 24)
            };

            // Column header
            var listHeader = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Theme.BgCard2 };
            listHeader.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, listHeader.Height - 1, listHeader.Width, listHeader.Height - 1);
            };
            foreach (var (text, left, width) in new[]
            {
                ("", 0, 36), ("Computer", 36, 200), ("IP Address", 236, 150),
                ("Status", 386, 100), ("App Version", 486, 120), ("Up to Date", 606, 100)
            })
            {
                listHeader.Controls.Add(new Label
                {
                    Text = text, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Theme.TextSecond, Left = left, Top = 0,
                    Width = width, Height = 34, TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent
                });
            }

            _clientList = new ListView
            {
                Dock = DockStyle.Fill, BackColor = Theme.BgCard, ForeColor = Theme.TextPrimary,
                Font = Theme.FontBase, FullRowSelect = true, GridLines = false,
                MultiSelect = true, BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable, CheckBoxes = true,
                View = View.Details
            };
            _clientList.Columns.Add("Computer",    200);
            _clientList.Columns.Add("IP Address",  150);
            _clientList.Columns.Add("Status",      100);
            _clientList.Columns.Add("Version",     120);
            _clientList.Columns.Add("Up to Date",  110);

            tablePanel.Controls.Add(_clientList);
            tablePanel.Controls.Add(listHeader);

            // ── Action bar ────────────────────────────────────────────────────
            var actionBar = new Panel
            {
                Dock = DockStyle.Bottom, Height = 52, BackColor = Theme.BgCard, Padding = new Padding(0, 9, 0, 9)
            };
            actionBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, actionBar.Width, 0);
            };

            var selectAllChk = new CheckBox
            {
                Text = "Select All", Font = Theme.FontSm, ForeColor = Theme.TextSecond,
                BackColor = Color.Transparent, AutoSize = true, Left = 24, Top = 14, Cursor = Cursors.Hand
            };
            selectAllChk.CheckedChanged += (_, _) =>
            {
                foreach (ListViewItem item in _clientList.Items) item.Checked = selectAllChk.Checked;
            };

            _uninstallBtn = Theme.MakeBtn("✕  Uninstall Selected", Theme.RedMuted);
            _uninstallBtn.Width = 180; _uninstallBtn.Top = 9;
            _uninstallBtn.Click += Uninstall_Click;

            actionBar.SizeChanged += (_, _) => _uninstallBtn.Left = actionBar.Width - _uninstallBtn.Width - 24;
            actionBar.Controls.AddRange(new Control[] { selectAllChk, _uninstallBtn });
            tablePanel.Controls.Add(actionBar);

            Controls.Add(tablePanel);
            Controls.Add(pendingBar);
            Controls.Add(banner);
            Controls.Add(toolbar);
            Controls.Add(header);

            // Store pendingBar reference for SelectUpdate_Click
            _pendingBar = pendingBar;

            ClientManager.ClientsChanged += RefreshList;
            RefreshList();
        }

        private readonly Panel _pendingBar;

        private void RefreshList()
        {
            if (InvokeRequired) { Invoke(RefreshList); return; }

            var all = ClientManager.GetAll().ToList();
            _countLbl.Text = $"{all.Count} device(s)";

            var expected = GetExpectedVersion();

            // Preserve checked
            var checkedIds = _clientList.Items.Cast<ListViewItem>()
                .Where(i => i.Checked).Select(i => i.Tag?.ToString() ?? "").ToHashSet();

            _clientList.BeginUpdate();
            _clientList.Items.Clear();
            foreach (var c in all.OrderByDescending(x => x.IsOnline).ThenBy(x => x.ComputerName))
            {
                var upToDate = c.Version == expected;
                var item = new ListViewItem(c.ComputerName) { Tag = c.Id };
                item.SubItems.Add(c.IpAddress);
                item.SubItems.Add(c.IsOnline ? "● Online" : "○ Offline");
                item.SubItems.Add(c.Version);
                item.SubItems.Add(upToDate ? "✓ Yes" : "✗ Outdated");

                if (!c.IsOnline) item.ForeColor = Theme.TextMuted;
                item.SubItems[2].ForeColor = c.IsOnline ? Theme.Green : Theme.TextMuted;
                item.SubItems[4].ForeColor = upToDate ? Theme.Green : Theme.Red;
                item.Checked = checkedIds.Contains(c.Id);
                _clientList.Items.Add(item);
            }
            _clientList.EndUpdate();
        }

        private void SelectUpdate_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select new LanClient.exe",
                Filter = "Executable (*.exe)|*.exe"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _pendingUpdatePath = dlg.FileName;
            _pendingLbl.Text = $"⚡  Ready to push: {Path.GetFileName(dlg.FileName)}  ({new FileInfo(dlg.FileName).Length / 1024 / 1024:F1} MB)";
            _pendingBar.Visible = true;
        }

        private void PushUpdate_Click(object? s, EventArgs e)
        {
            if (_pendingUpdatePath == null) { ToastManager.Show("Select an update EXE first.", ToastKind.Warning); return; }

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any()) { ToastManager.Show("No clients online.", ToastKind.Warning); return; }

            if (!ConfirmDialog.Ask(FindForm()!, "Push Update?",
                $"This will push the update to {targets.Count} online client(s). They will auto-install and restart.",
                "Push Update", danger: false)) return;

            try
            {
                FileManager.SaveFile(_pendingUpdatePath, "Download");
                var fileName = Path.GetFileName(_pendingUpdatePath);
                var ip  = GetLocalIp();
                var url = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(fileName)}";
                CommandDispatcher.IssueUpdate(targets, fileName, url);
                AppState.Log($"Update pushed: {fileName} → {targets.Count} client(s)", LogLevel.Success);
                ToastManager.Show($"Update sent to {targets.Count} client(s).", ToastKind.Success);
            }
            catch (Exception ex)
            {
                AppState.Log($"Update push failed: {ex.Message}", LogLevel.Error);
                ToastManager.Show("Push failed. See Activity.", ToastKind.Error);
            }
        }

        private void Uninstall_Click(object? s, EventArgs e)
        {
            var ids = _clientList.Items.Cast<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => i.Tag?.ToString() ?? "")
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            if (!ids.Any()) { ToastManager.Show("Check the clients to uninstall from.", ToastKind.Warning); return; }

            if (!ConfirmDialog.Ask(FindForm()!, "Uninstall App?",
                $"This will uninstall LanC Client from {ids.Count} machine(s). This cannot be undone.",
                "Uninstall", danger: true)) return;

            CommandDispatcher.IssueUninstall(ids);
            AppState.Log($"Uninstall issued → {ids.Count} client(s)", LogLevel.Warning);
            ToastManager.Show($"Uninstall sent to {ids.Count} client(s).", ToastKind.Warning);
        }

        private static string GetExpectedVersion()
        {
            // Read from a version file next to the server exe, or default
            var vFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_version.txt");
            return File.Exists(vFile) ? File.ReadAllText(vFile).Trim() : "1.0.0";
        }

        private static string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                        return addr.Address.ToString();
            }
            return "127.0.0.1";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ClientManager.ClientsChanged -= RefreshList;
            base.Dispose(disposing);
        }
    }
}
