using LanServer.Controls;

namespace LanServer.Pages
{
    public class FileManagerPage : Panel
    {
        private readonly DarkListView _table;
        private readonly Panel        _emptyPanel;
        private readonly Panel        _tablePanel;
        private readonly TextBox      _searchBox;
        private readonly Label        _countLbl;
        private List<ManagedFile>     _files = new();

        public FileManagerPage()
        {
            Dock      = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("File Manager", "Upload, share and deploy files to connected clients.");

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.BgCard, Padding = new Padding(24, 10, 24, 10) };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            _searchBox = Theme.MakeInput();
            _searchBox.Width = 220; _searchBox.Top = 12; _searchBox.Left = 24;
            _searchBox.TextChanged += (_, _) => ApplyFilter();

            _countLbl = new Label
            {
                Text = "0 files", Font = Theme.FontSm, ForeColor = Theme.TextSecond,
                AutoSize = true, Top = 18, Left = 258, BackColor = Color.Transparent
            };

            var uploadBtn = Theme.MakeBtn("↑  Upload File", Theme.Blue);
            uploadBtn.AutoSize = true;
            uploadBtn.Padding  = new Padding(16, 0, 16, 0);
            uploadBtn.Top      = 11;
            uploadBtn.Click   += Upload_Click;

            var openUrlBtn = Theme.MakeBtn("⊕  Open URL on Clients", Theme.Purple);
            openUrlBtn.AutoSize = true;
            openUrlBtn.Padding  = new Padding(14, 0, 14, 0);
            openUrlBtn.Top      = 11;
            openUrlBtn.Click   += OpenUrl_Click;

            toolbar.SizeChanged += (_, _) =>
            {
                uploadBtn.Left  = toolbar.Width - uploadBtn.Width - 24;
                openUrlBtn.Left = uploadBtn.Left - openUrlBtn.Width - 10;
            };
            toolbar.Controls.AddRange(new Control[] { _searchBox, _countLbl, openUrlBtn, uploadBtn });

            // ── Info banner ───────────────────────────────────────────────────
            var infoBanner = MakeInfoBanner();

            // ── Table ─────────────────────────────────────────────────────────
            _tablePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp, Padding = new Padding(24, 12, 24, 0) };

            _table = new DarkListView { Dock = DockStyle.Fill };
            _table.Columns.Add("File Name",  -2);
            _table.Columns.Add("Type",       100);
            _table.Columns.Add("Size",        90);
            _table.Columns.Add("Action",      80);
            _tablePanel.Controls.Add(_table);

            // Action bar
            var actionBar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Theme.BgCard, Padding = new Padding(0, 9, 0, 9) };
            actionBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, actionBar.Width, 0);
            };

            var deployBtn = Theme.MakeBtn("▶  Install on Clients", Theme.Green);
            deployBtn.Left = 24; deployBtn.Top = 9; deployBtn.Width = 170;
            var deployHint = new Label
            {
                Text = "Runs the installer silently on all connected clients",
                Font = Theme.FontSm, ForeColor = Theme.TextMuted,
                AutoSize = true, Left = 206, Top = 16,
                BackColor = Color.Transparent
            };
            deployBtn.Click += Deploy_Click;

            var sendBtn = Theme.MakeOutlineBtn("↓  Send to Clients", Theme.Blue);
            sendBtn.Left = 24; sendBtn.Top = 9; sendBtn.Width = 160;
            sendBtn.Visible = false;
            sendBtn.Click += Download_Click;

            var deleteBtn = Theme.MakeBtn("✕  Delete", Theme.RedMuted);
            deleteBtn.Top = 9; deleteBtn.Width = 96;
            actionBar.SizeChanged += (_, _) => deleteBtn.Left = actionBar.Width - deleteBtn.Width - 24;
            deleteBtn.Click += Delete_Click;

            // Swap Install vs Send buttons based on file type
            _table.SelectedIndexChanged += (_, _) =>
            {
                if (_table.SelectedItems.Count == 0) return;
                var fn  = _table.SelectedItems[0].Tag?.ToString() ?? "";
                bool isInst = FileManager.IsInstaller(fn);
                deployBtn.Visible = isInst;
                deployHint.Visible = isInst;
                sendBtn.Visible   = !isInst;
            };

            actionBar.Controls.AddRange(new Control[] { deployBtn, deployHint, sendBtn, deleteBtn });
            _tablePanel.Controls.Add(actionBar);

            // ── Empty state ───────────────────────────────────────────────────
            _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };
            _emptyPanel.Controls.Add(new EmptyState(
                "▤", "No files uploaded",
                "Upload any file — installers, documents, images, etc.",
                "↑  Upload File", () => Upload_Click(null, EventArgs.Empty)));

            Controls.Add(_emptyPanel);
            Controls.Add(_tablePanel);
            Controls.Add(infoBanner);
            Controls.Add(toolbar);
            Controls.Add(header);

            Refresh();
        }

        // ── Info banner ───────────────────────────────────────────────────────
        private static Panel MakeInfoBanner()
        {
            var banner = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Theme.BlueSoft };
            banner.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, banner.Height - 1, banner.Width, banner.Height - 1);
                // Left accent
                using var bar = new SolidBrush(Theme.Blue);
                e.Graphics.FillRectangle(bar, 0, 0, 3, banner.Height);
            };
            var lbl = new Label
            {
                Text = "ℹ  Supports all file types: installers (.exe, .msi), documents (.docx, .pdf), images (.png, .jpg), archives (.zip), Java (.jar), and more.",
                Font = Theme.FontSm, ForeColor = Theme.Blue,
                AutoSize = true, Left = 16, Top = 14, BackColor = Color.Transparent
            };
            banner.Controls.Add(lbl);
            return banner;
        }

        // ── Refresh ───────────────────────────────────────────────────────────
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
                if (string.IsNullOrEmpty(ext)) ext = "FILE";
                var size = f.FileSize > 1024 * 1024
                    ? $"{f.FileSize / 1024.0 / 1024.0:F1} MB"
                    : $"{f.FileSize / 1024} KB";
                var action = FileManager.IsInstaller(f.FileName) ? "Install" : "Send";
                var item = _table.AddRow(Theme.BgCard, Theme.TextPrimary, f.FileName, ext, size, action);
                item.Tag = f.FileName;
                item.SubItems[3].ForeColor = FileManager.IsInstaller(f.FileName) ? Theme.Green : Theme.Blue;
            }
        }

        // ── Upload ────────────────────────────────────────────────────────────
        private void Upload_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Select any file to upload",
                Filter = "All Files (*.*)|*.*" +
                         "|Installers (*.exe;*.msi)|*.exe;*.msi" +
                         "|Documents (*.pdf;*.docx;*.xlsx;*.pptx)|*.pdf;*.docx;*.xlsx;*.pptx" +
                         "|Images (*.png;*.jpg;*.gif;*.bmp)|*.png;*.jpg;*.gif;*.bmp" +
                         "|Archives (*.zip;*.7z;*.rar)|*.zip;*.7z;*.rar" +
                         "|Java (*.jar;*.java)|*.jar;*.java",
                FilterIndex = 1
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                FileManager.SaveFile(dlg.FileName, "Download");
                AppState.Log($"Uploaded: {Path.GetFileName(dlg.FileName)}", LogLevel.Success);
                ToastManager.Show($"'{Path.GetFileName(dlg.FileName)}' uploaded successfully.", ToastKind.Success);
                Refresh();
            }
            catch (Exception ex)
            {
                AppState.Log($"Upload failed: {ex.Message}", LogLevel.Error);
                ToastManager.Show("Upload failed. See Activity for details.", ToastKind.Error);
            }
        }

        // ── Open URL on clients ───────────────────────────────────────────────
        private void OpenUrl_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenUrlDialog();
            if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any()) { ToastManager.Show("No clients connected.", ToastKind.Warning); return; }

            CommandDispatcher.IssueOpenUrl(targets, dlg.Url);
            AppState.Log($"Open URL → {targets.Count} client(s): {dlg.Url}", LogLevel.Info);
            ToastManager.Show($"Opening URL on {targets.Count} client(s).", ToastKind.Info);
        }

        // ── Deploy (install) ──────────────────────────────────────────────────
        private void Deploy_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) { ToastManager.Show("Select an installer file to deploy.", ToastKind.Warning); return; }
            var fileName = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var file = _files.FirstOrDefault(f => f.FileName == fileName);
            if (file == null) return;

            if (!FileManager.IsInstaller(fileName))
            {
                ToastManager.Show("Select an .exe or .msi installer to use Install.", ToastKind.Warning);
                return;
            }

            // Ask installer type
            using var typeDlg = new InstallerTypeDialog();
            if (typeDlg.ShowDialog(FindForm()) != DialogResult.OK) return;

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any()) { ToastManager.Show("No clients connected.", ToastKind.Warning); return; }

            var ip  = GetLocalIp();
            var url = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            CommandDispatcher.IssueInstall(targets, file.FileName, typeDlg.SelectedType, url);
            AppState.AddDeployment(new DeploymentRecord
            {
                FileName = file.FileName, InstallerType = typeDlg.SelectedType,
                FileSize = file.FileSize, TargetIds = targets
            });
            AppState.Log($"Install '{file.FileName}' [{typeDlg.SelectedType}] → {targets.Count} client(s)", LogLevel.Success);
            ToastManager.Show($"Installing on {targets.Count} client(s).", ToastKind.Success);
        }

        // ── Send (download to client) ─────────────────────────────────────────
        private void Download_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) { ToastManager.Show("Select a file to send.", ToastKind.Warning); return; }
            var fileName = _table.SelectedItems[0].Tag?.ToString() ?? "";
            var file = _files.FirstOrDefault(f => f.FileName == fileName);
            if (file == null) return;

            var targets = ClientManager.GetOnline().Select(c => c.Id).ToList();
            if (!targets.Any()) { ToastManager.Show("No clients connected.", ToastKind.Warning); return; }

            var ip  = GetLocalIp();
            var url = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            CommandDispatcher.IssueDownload(targets, file.FileName, url);
            AppState.Log($"Send '{file.FileName}' → {targets.Count} client(s)", LogLevel.Info);
            ToastManager.Show($"Sending '{file.FileName}' to {targets.Count} client(s).", ToastKind.Info);
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private void Delete_Click(object? s, EventArgs e)
        {
            if (_table.SelectedItems.Count == 0) return;
            var fileName = _table.SelectedItems[0].Tag?.ToString() ?? "";
            if (!ConfirmDialog.Ask(FindForm()!, "Delete File?",
                $"This will permanently remove '{fileName}' from the server.", "Delete")) return;
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

    // ── Open URL dialog ───────────────────────────────────────────────────────
    internal class OpenUrlDialog : Form
    {
        public string Url { get; private set; } = "";

        public OpenUrlDialog()
        {
            Text = "Open URL on Connected Clients";
            Size = new Size(480, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;

            var accentBar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Theme.Blue };

            var lbl = new Label
            {
                Text = "URL to open (e.g. https://google.com)",
                Font = Theme.FontSm, ForeColor = Theme.TextSecond,
                AutoSize = true, Left = 24, Top = 24, BackColor = Color.Transparent
            };
            var box = Theme.MakeInput();
            box.Left = 24; box.Top = 44; box.Width = 430; box.Height = 30;
            box.Text = "https://";

            var hintLbl = new Label
            {
                Text = "This will open the default web browser on all connected clients.",
                Font = Theme.FontSm, ForeColor = Theme.TextMuted,
                AutoSize = true, Left = 24, Top = 82, BackColor = Color.Transparent
            };

            var sep = new Panel { Left = 0, Top = 110, Width = 480, Height = 1, BackColor = Theme.Border };

            var okBtn = Theme.MakeBtn("Open on Clients", Theme.Blue);
            okBtn.Left = 24; okBtn.Top = 122; okBtn.Width = 200;
            okBtn.Click += (_, _) =>
            {
                var u = box.Text.Trim();
                if (!u.StartsWith("http://") && !u.StartsWith("https://"))
                { MessageBox.Show("Please enter a valid URL starting with http:// or https://", "Invalid URL"); return; }
                Url = u;
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { accentBar, lbl, box, hintLbl, sep, okBtn });
        }
    }

    // ── Installer type dialog ─────────────────────────────────────────────────
    internal class InstallerTypeDialog : Form
    {
        public string SelectedType { get; private set; } = "NSIS";

        public InstallerTypeDialog()
        {
            Text = "Select Installer Type";
            Size = new Size(380, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;

            var accentBar = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Theme.Green };

            var lbl = new Label
            {
                Text = "How was this installer packaged?",
                Font = Theme.FontSm, ForeColor = Theme.TextSecond,
                AutoSize = true, Left = 24, Top = 22, BackColor = Color.Transparent
            };
            var combo = Theme.MakeCombo();
            combo.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
            combo.SelectedIndex = 0;
            combo.Left = 24; combo.Top = 44; combo.Width = 330;

            var hintLbl = new Label
            {
                Text = "NSIS & Inno Setup → /S silent  ·  MSI → /quiet /norestart  ·  InstallShield → /s",
                Font = Theme.FontSm, ForeColor = Theme.TextMuted,
                AutoSize = false, Width = 330, Height = 32, Left = 24, Top = 82,
                BackColor = Color.Transparent
            };

            var ok = Theme.MakeBtn("Confirm & Deploy", Theme.Green);
            ok.Left = 24; ok.Top = 126; ok.Width = 330;
            ok.Click += (_, _) =>
            {
                SelectedType = combo.SelectedItem?.ToString() ?? "NSIS";
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { accentBar, lbl, combo, hintLbl, ok });
        }
    }
}
