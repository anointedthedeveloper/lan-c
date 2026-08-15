using LanServer.Controls;

namespace LanServer.Pages
{
    public class FileManagerPage : Panel
    {
        private readonly DarkListView _table;
        private readonly Panel _emptyPanel;
        private readonly Panel _tablePanel;
        private readonly TextBox _searchBox;
        private readonly Label _countLbl;
        private List<ManagedFile> _files = new();

        public FileManagerPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("File Manager", "Manage installer packages and deployment files.");

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.BgCard, Padding = new Padding(24, 10, 24, 10) };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            _searchBox = Theme.MakeInput();
            _searchBox.Width = 240; _searchBox.Top = 12; _searchBox.Left = 24;
            _searchBox.TextChanged += (_, _) => ApplyFilter();

            _countLbl = new Label { Text = "0 files", Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Top = 18, Left = 280 };

            var uploadBtn = Theme.MakeBtn("↑  Upload File", Theme.Blue);
            uploadBtn.AutoSize = true;
            uploadBtn.Padding = new Padding(16, 0, 16, 0);
            uploadBtn.Top = 11;
            uploadBtn.Click += Upload_Click;
            toolbar.SizeChanged += (_, _) => uploadBtn.Left = toolbar.Width - uploadBtn.Width - 24;
            toolbar.Controls.AddRange(new Control[] { _searchBox, _countLbl, uploadBtn });

            // ── Table ─────────────────────────────────────────────────────────
            _tablePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp, Padding = new Padding(24, 16, 24, 24) };
            _table = new DarkListView { Dock = DockStyle.Fill };
            _table.Columns.Add("File Name",  -2);
            _table.Columns.Add("Type",        80);
            _table.Columns.Add("Size",        90);
            _table.Columns.Add("Status",      80);
            _tablePanel.Controls.Add(_table);

            // Action buttons below table
            var actionBar = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.BgCard, Padding = new Padding(0, 7, 0, 7) };
            actionBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, actionBar.Width, 0);
            };

            var deployBtn = Theme.MakeBtn("▶  Deploy Selected", Theme.Green);
            deployBtn.Left = 24; deployBtn.Top = 7; deployBtn.Width = 160;
            deployBtn.Click += Deploy_Click;

            var downloadBtn = Theme.MakeOutlineBtn("↓  Download", Theme.Blue);
            downloadBtn.Left = 196; downloadBtn.Top = 7; downloadBtn.Width = 130;
            downloadBtn.Click += Download_Click;

            var deleteBtn = Theme.MakeBtn("✕  Delete", Theme.RedMuted);
            deleteBtn.Top = 7; deleteBtn.Width = 100;
            actionBar.SizeChanged += (_, _) => deleteBtn.Left = actionBar.Width - deleteBtn.Width - 24;
            deleteBtn.Click += Delete_Click;

            actionBar.Controls.AddRange(new Control[] { deployBtn, downloadBtn, deleteBtn });
            _tablePanel.Controls.Add(actionBar);

            // ── Empty state ───────────────────────────────────────────────────
            _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };
            _emptyPanel.Controls.Add(new EmptyState("▤", "No files uploaded", "Upload an installer package to get started.", "↑  Upload File", () => Upload_Click(null, EventArgs.Empty)));

            Controls.Add(_emptyPanel);
            Controls.Add(_tablePanel);
            Controls.Add(toolbar);
            Controls.Add(header);

            Refresh();
        }

        public new void Refresh()
        {
            if (InvokeRequired) { Invoke(Refresh); return; }
            _files = FileManager.GetFiles();
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var q = _searchBox.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(q)
                ? _files
                : _files.Where(f => f.FileName.ToLower().Contains(q)).ToList();

            _countLbl.Text = $"{filtered.Count} file(s)";
            bool hasData = filtered.Count > 0;
            _tablePanel.Visible = hasData;
            _emptyPanel.Visible = !hasData;

            if (!hasData) return;

            _table.Items.Clear();
            foreach (var f in filtered)
            {
                var ext  = Path.GetExtension(f.FileName).TrimStart('.').ToUpper();
                var size = f.FileSize > 1024 * 1024
                    ? $"{f.FileSize / 1024.0 / 1024.0:F1} MB"
                    : $"{f.FileSize / 1024} KB";
                var item = _table.AddRow(Theme.BgCard, Theme.TextPrimary, f.FileName, ext, size, "Ready");
                item.Tag = f.FileName;
                item.SubItems[3].ForeColor = Theme.Green;
            }
        }

        private void Upload_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "Installers|*.exe;*.msi|All files|*.*" };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            using var typeDlg = new InstallerTypeDialog();
            if (typeDlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            try
            {
                FileManager.SaveFile(dlg.FileName, typeDlg.SelectedType);
                AppState.Log($"Uploaded: {Path.GetFileName(dlg.FileName)}", LogLevel.Success);
                ToastManager.Show($"'{Path.GetFileName(dlg.FileName)}' uploaded.", ToastKind.Success);
                Refresh();
            }
            catch (Exception ex)
            {
                AppState.Log($"Upload failed: {ex.Message}", LogLevel.Error);
                ToastManager.Show("Upload failed.", ToastKind.Error);
            }
        }

        private void Deploy_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) { ToastManager.Show("Select a file to deploy.", ToastKind.Warning); return; }
            var fileName = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var file = _files.FirstOrDefault(f => f.FileName == fileName);
            if (file == null) return;

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any()) { ToastManager.Show("No clients connected.", ToastKind.Warning); return; }

            var ip  = GetLocalIp();
            var url = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            CommandDispatcher.IssueInstall(targets, file.FileName, file.InstallerType, url);
            AppState.AddDeployment(new DeploymentRecord { FileName = file.FileName, InstallerType = file.InstallerType, FileSize = file.FileSize, TargetIds = targets });
            AppState.Log($"Deployed '{file.FileName}' → {targets.Count} client(s)", LogLevel.Success);
            ToastManager.Show($"'{file.FileName}' deployed to {targets.Count} client(s).", ToastKind.Success);
        }

        private void Download_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) { ToastManager.Show("Select a file.", ToastKind.Warning); return; }
            var fileName = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var file = _files.FirstOrDefault(f => f.FileName == fileName);
            if (file == null) return;

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any()) { ToastManager.Show("No clients connected.", ToastKind.Warning); return; }

            var ip  = GetLocalIp();
            var url = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            CommandDispatcher.IssueDownload(targets, file.FileName, url);
            AppState.Log($"Download '{file.FileName}' → {targets.Count} client(s)", LogLevel.Info);
            ToastManager.Show($"Download sent to {targets.Count} client(s).", ToastKind.Info);
        }

        private void Delete_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) return;
            var fileName = _table.SelectedItems[0].Tag?.ToString() ?? "";
            if (!ConfirmDialog.Ask(FindForm(), "Delete File?", $"This will permanently remove '{fileName}'.", "Delete File")) return;
            FileManager.DeleteFile(fileName);
            AppState.Log($"Deleted: {fileName}", LogLevel.Warning);
            ToastManager.Show($"'{fileName}' deleted.", ToastKind.Warning);
            Refresh();
        }

        private static string GetLocalIp()
        {
            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        !System.Net.IPAddress.IsLoopback(addr.Address))
                        return addr.Address.ToString();
            }
            return "127.0.0.1";
        }
    }

    // ── Installer type picker dialog ──────────────────────────────────────────
    internal class InstallerTypeDialog : Form
    {
        public string SelectedType { get; private set; } = "NSIS";

        public InstallerTypeDialog()
        {
            Text = "Select Installer Type";
            Size = new Size(360, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;

            var lbl = new Label { Text = "Installer Type", Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 24, Top = 24 };
            var combo = Theme.MakeCombo();
            combo.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
            combo.SelectedIndex = 0;
            combo.Left = 24; combo.Top = 44; combo.Width = 300;

            var ok = Theme.MakeBtn("Confirm", Theme.Blue);
            ok.Left = 24; ok.Top = 96; ok.Width = 300;
            ok.Click += (_, _) => { SelectedType = combo.SelectedItem?.ToString() ?? "NSIS"; DialogResult = DialogResult.OK; Close(); };

            Controls.AddRange(new Control[] { lbl, combo, ok });
        }
    }
}
