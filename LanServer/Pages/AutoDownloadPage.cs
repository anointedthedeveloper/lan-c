using LanServer.Controls;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

namespace LanServer.Pages
{
    /// <summary>
    /// AutoDownload page — admin uploads a file and gets a short LAN URL.
    /// When any device on the LAN visits that URL, it instantly downloads the file.
    /// Connected LanC clients can also be pushed the file automatically.
    /// </summary>
    public class AutoDownloadPage : Panel
    {
        private readonly DarkListView _table;
        private readonly Panel        _emptyPanel;
        private readonly Panel        _tablePanel;
        private readonly Label        _countLbl;

        public AutoDownloadPage()
        {
            Dock      = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Auto Download",
                "Upload a file and share a short LAN link — anyone who visits it downloads it instantly.");

            // ── Info banner ───────────────────────────────────────────────────
            var banner = MakeInfoBanner();

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Theme.BgCard,
                Padding   = new Padding(24, 10, 24, 10)
            };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            _countLbl = new Label
            {
                Text      = "0 links",
                Font      = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize  = true,
                Top       = 18,
                Left      = 24,
                BackColor = Color.Transparent
            };

            var uploadBtn = Theme.MakeBtn("↑  Upload & Generate Link", Theme.Blue);
            uploadBtn.AutoSize = true;
            uploadBtn.Padding  = new Padding(16, 0, 16, 0);
            uploadBtn.Top      = 11;
            uploadBtn.Click   += Upload_Click;

            toolbar.SizeChanged += (_, _) => uploadBtn.Left = toolbar.Width - uploadBtn.Width - 24;
            toolbar.Controls.AddRange(new Control[] { _countLbl, uploadBtn });

            // ── Table panel ───────────────────────────────────────────────────
            _tablePanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Theme.BgApp,
                Padding   = new Padding(24, 12, 24, 24)
            };

            _table = new DarkListView { Dock = DockStyle.Fill, MultiSelect = false };
            _table.Columns.Add("File Name",    -2);
            _table.Columns.Add("Short URL",    160);
            _table.Columns.Add("Full URL",     260);
            _table.Columns.Add("Uploaded At",  160);
            _table.Columns.Add("Action",        80);
            _tablePanel.Controls.Add(_table);

            // Action bar
            var actionBar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Theme.BgCard,
                Padding   = new Padding(0, 9, 0, 9)
            };
            actionBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, actionBar.Width, 0);
            };

            var copyUrlBtn = Theme.MakeOutlineBtn("⎘  Copy Link", Theme.Blue);
            copyUrlBtn.Left = 24; copyUrlBtn.Top = 9; copyUrlBtn.Width = 130;
            copyUrlBtn.Click += CopyLink_Click;

            var pushBtn = Theme.MakeBtn("▶  Push to All Clients", Theme.Green);
            pushBtn.Left = 166; pushBtn.Top = 9; pushBtn.Width = 180;
            pushBtn.Click += PushToClients_Click;

            var deleteBtn = Theme.MakeBtn("✕  Remove", Theme.RedMuted);
            deleteBtn.Top = 9; deleteBtn.Width = 100;
            actionBar.SizeChanged += (_, _) => deleteBtn.Left = actionBar.Width - deleteBtn.Width - 24;
            deleteBtn.Click += Delete_Click;

            actionBar.Controls.AddRange(new Control[] { copyUrlBtn, pushBtn, deleteBtn });
            _tablePanel.Controls.Add(actionBar);

            // ── Empty state ───────────────────────────────────────────────────
            _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };
            _emptyPanel.Controls.Add(new EmptyState(
                "⬇",
                "No auto-download links yet",
                "Upload a file to generate a short LAN link that auto-downloads on visit.",
                "↑  Upload & Generate Link",
                () => Upload_Click(null, EventArgs.Empty)));

            Controls.Add(_emptyPanel);
            Controls.Add(_tablePanel);
            Controls.Add(banner);
            Controls.Add(toolbar);
            Controls.Add(header);

            AutoDownloadManager.EntriesChanged += Refresh;
            Refresh();
        }

        // ── Info banner ───────────────────────────────────────────────────────
        private static Panel MakeInfoBanner()
        {
            var banner = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Theme.AmberSoft };
            banner.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, banner.Height - 1, banner.Width, banner.Height - 1);
                using var bar = new SolidBrush(Theme.Amber);
                e.Graphics.FillRectangle(bar, 0, 0, 3, banner.Height);
            };
            var lbl = new Label
            {
                Text = "⚡  Share the short link (e.g. http://192.168.1.x:5001/dl/AB3K7Q) — visiting it immediately downloads the file. " +
                       "Use \"Push to All Clients\" to automatically send it to connected LanC clients.",
                Font      = Theme.FontSm,
                ForeColor = Theme.Amber,
                AutoSize  = false,
                Left      = 16,
                Top       = 10,
                Width     = 900,
                Height    = 28,
                BackColor = Color.Transparent
            };
            banner.Controls.Add(lbl);
            return banner;
        }

        private new void Refresh()
        {
            if (InvokeRequired) { Invoke(Refresh); return; }

            var entries = AutoDownloadManager.GetEntries().ToList();
            _countLbl.Text = $"{entries.Count} link(s)";

            bool hasData = entries.Count > 0;
            _tablePanel.Visible = hasData;
            _emptyPanel.Visible = !hasData;
            if (!hasData) return;

            var ip = GetLocalIp();
            _table.Items.Clear();
            foreach (var e in entries)
            {
                var shortUrl = $"/dl/{e.ShortCode}";
                var fullUrl  = $"http://{ip}:{Config.Current.HttpPort}/dl/{e.ShortCode}";
                var item = _table.AddRow(
                    Theme.BgCard, Theme.TextPrimary,
                    e.FileName,
                    shortUrl,
                    fullUrl,
                    e.UploadedAt.ToString("MMM dd HH:mm"),
                    "Copy"
                );
                item.Tag = e.ShortCode;
                item.SubItems[4].ForeColor = Theme.Blue;
            }

            // Auto-size file name column
            if (_table.Columns.Count > 0)
            {
                int used = _table.Columns.Cast<ColumnHeader>().Skip(1).Sum(c => c.Width);
                _table.Columns[0].Width = Math.Max(120, _table.ClientSize.Width - used - 4);
            }
        }

        // ── Upload ────────────────────────────────────────────────────────────
        private void Upload_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select file to make available for auto-download",
                Filter = "All Files (*.*)|*.*|Installers (*.exe;*.msi)|*.exe;*.msi"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                // Save to uploads if not already there
                var dest = FileManager.SaveFile(dlg.FileName, "AutoDownload");
                var fileName = Path.GetFileName(dlg.FileName);
                var code = AutoDownloadManager.Register(fileName);
                var ip   = GetLocalIp();
                var fullUrl = $"http://{ip}:{Config.Current.HttpPort}/dl/{code}";

                AppState.Log($"Auto-download link created: {fullUrl}", LogLevel.Success);
                ToastManager.Show($"Link generated! Copied to clipboard.", ToastKind.Success);

                // Copy to clipboard automatically
                try { Clipboard.SetText(fullUrl); } catch { }

                Refresh();
            }
            catch (Exception ex)
            {
                AppState.Log($"Auto-download upload failed: {ex.Message}", LogLevel.Error);
                ToastManager.Show("Upload failed. See Activity for details.", ToastKind.Error);
            }
        }

        // ── Copy link ─────────────────────────────────────────────────────────
        private void CopyLink_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0)
            {
                ToastManager.Show("Select a link to copy.", ToastKind.Warning);
                return;
            }
            var code = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var ip   = GetLocalIp();
            var url  = $"http://{ip}:{Config.Current.HttpPort}/dl/{code}";
            try
            {
                Clipboard.SetText(url);
                ToastManager.Show($"Copied: {url}", ToastKind.Success);
            }
            catch
            {
                ToastManager.Show($"Link: {url}", ToastKind.Info);
            }
        }

        // ── Push to all connected clients ─────────────────────────────────────
        private void PushToClients_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0)
            {
                ToastManager.Show("Select a link first.", ToastKind.Warning);
                return;
            }

            var code  = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var entry = AutoDownloadManager.GetByCode(code);
            if (entry == null) return;

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any())
            {
                ToastManager.Show("No clients connected.", ToastKind.Warning);
                return;
            }

            var ip      = GetLocalIp();
            var dlUrl   = $"http://{ip}:{Config.Current.HttpPort}/dl/{code}";
            CommandDispatcher.IssueAutoDownload(targets, entry.FileName, dlUrl);

            AppState.Log($"Auto-download pushed '{entry.FileName}' → {targets.Count} client(s) [{dlUrl}]", LogLevel.Success);
            ToastManager.Show($"Pushed to {targets.Count} client(s).", ToastKind.Success);
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private void Delete_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) return;
            var code = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var entry = AutoDownloadManager.GetByCode(code);
            if (entry == null) return;

            if (!ConfirmDialog.Ask(FindForm()!,
                "Remove Auto-Download Link?",
                $"This will remove the short link for '{entry.FileName}'. The file itself stays in File Manager.",
                "Remove")) return;

            AutoDownloadManager.Remove(code);
            AppState.Log($"Auto-download link removed: {code}", LogLevel.Warning);
            ToastManager.Show("Link removed.", ToastKind.Warning);
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
            if (disposing) AutoDownloadManager.EntriesChanged -= Refresh;
            base.Dispose(disposing);
        }
    }
}
