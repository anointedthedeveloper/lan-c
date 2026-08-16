using LanServer.Controls;

namespace LanServer.Pages
{
    public class DeploymentsPage : Panel
    {
        private readonly DarkListView _table;
        private readonly Panel _emptyPanel;
        private readonly Panel _tablePanel;
        private readonly Label _countLbl;

        public DeploymentsPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Deployments", "Manage application deployments on your LanC server.");

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.BgCard, Padding = new Padding(24, 10, 24, 10) };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            _countLbl = new Label { Text = "0 deployments", Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Top = 18, Left = 24 };

            var newBtn = Theme.MakeBtn("+ New Deployment", Theme.Blue);
            newBtn.AutoSize = true;
            newBtn.Padding = new Padding(16, 0, 16, 0);
            newBtn.Top = 11;
            newBtn.Click += NewDeployment_Click;
            toolbar.SizeChanged += (_, _) => newBtn.Left = toolbar.Width - newBtn.Width - 24;
            toolbar.Controls.AddRange(new Control[] { _countLbl, newBtn });

            // ── Table ─────────────────────────────────────────────────────────
            _tablePanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp, Padding = new Padding(24, 16, 24, 24) };

            _table = new DarkListView { Dock = DockStyle.Fill, MultiSelect = false };
            _table.Columns.Add("File Name",      200);
            _table.Columns.Add("Installer Type",  110);
            _table.Columns.Add("Size",             80);
            _table.Columns.Add("Deployed At",     160);
            _table.Columns.Add("Targets",          70);
            _table.Columns.Add("Status",           80);
            _tablePanel.Controls.Add(_table);

            // ── Empty state ───────────────────────────────────────────────────
            _emptyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp };
            var empty = new EmptyState("⬡", "No deployments yet", "Create your first deployment to get started.", "+ New Deployment", () => NewDeployment_Click(null, EventArgs.Empty));
            _emptyPanel.Controls.Add(empty);

            Controls.Add(_emptyPanel);
            Controls.Add(_tablePanel);
            Controls.Add(toolbar);
            Controls.Add(header);

            AppState.DeploymentsChanged += Refresh;
            Refresh();
        }

        private new void Refresh()
        {
            if (InvokeRequired) { Invoke(Refresh); return; }
            var deps = AppState.GetDeployments();
            _countLbl.Text = $"{deps.Count} deployment(s)";

            bool hasData = deps.Count > 0;
            _tablePanel.Visible = hasData;
            _emptyPanel.Visible = !hasData;

            if (!hasData) return;

            _table.Items.Clear();
            foreach (var d in deps.OrderByDescending(x => x.DeployedAt))
            {
                var size = d.FileSize > 0 ? $"{d.FileSize / 1024.0 / 1024.0:F1} MB" : "—";
                var item = _table.AddRow(
                    Theme.BgCard, Theme.TextPrimary,
                    d.FileName, d.InstallerType, size,
                    d.DeployedAt.ToString("MMM dd, yyyy HH:mm"),
                    d.TargetIds.Count.ToString(),
                    d.Status
                );
                item.Tag = d.Id;
                // Color status
                item.SubItems[5].ForeColor = d.Status == "Active" ? Theme.Green : Theme.TextSecond;
            }
        }

        private void NewDeployment_Click(object? s, EventArgs e)
        {
            using var wiz = new NewDeploymentWizard();
            wiz.ShowDialog(FindForm());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppState.DeploymentsChanged -= Refresh;
            base.Dispose(disposing);
        }
    }

    // ── New Deployment Wizard ─────────────────────────────────────────────────
    internal class NewDeploymentWizard : Form
    {
        private int _step = 0;
        private string? _filePath;
        private readonly Panel _stepHost;
        private readonly Label _stepIndicator;
        private readonly Button _backBtn;
        private readonly Button _nextBtn;

        // Step 2 fields
        private TextBox? _appNameBox;
        private ComboBox? _typeCombo;
        private ComboBox? _targetCombo;

        public NewDeploymentWizard()
        {
            Text = "New Deployment";
            Size = new Size(560, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;

            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.BgApp };
            hdr.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
                // Blue accent top bar
                using var accent = new SolidBrush(Theme.Blue);
                e.Graphics.FillRectangle(accent, 0, 0, hdr.Width, 3);
            };
            var hdrTitle = new Label { Text = "New Deployment", Font = Theme.FontLg, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 24, Top = 18, BackColor = Color.Transparent };
            _stepIndicator = new Label { Text = "Step 1 of 3 — Select File", Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 24, Top = 44, BackColor = Color.Transparent };
            hdr.Controls.AddRange(new Control[] { hdrTitle, _stepIndicator });

            // Footer
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.BgApp };
            footer.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
            };
            _backBtn = Theme.MakeOutlineBtn("← Back", Theme.TextSecond);
            _backBtn.Left = 24; _backBtn.Top = 11; _backBtn.Width = 100;
            _backBtn.Click += (_, _) => GoStep(_step - 1);

            _nextBtn = Theme.MakeBtn("Next →", Theme.Blue);
            _nextBtn.Left = 432; _nextBtn.Top = 11; _nextBtn.Width = 100;
            _nextBtn.Click += Next_Click;
            footer.Controls.AddRange(new Control[] { _backBtn, _nextBtn });

            _stepHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgCard };

            Controls.Add(_stepHost);
            Controls.Add(footer);
            Controls.Add(hdr);

            GoStep(0);
        }

        private void GoStep(int step)
        {
            _step = Math.Clamp(step, 0, 2);
            _backBtn.Enabled = _step > 0;
            _stepIndicator.Text = _step switch
            {
                0 => "Step 1 of 3 — Select File",
                1 => "Step 2 of 3 — Configure",
                _ => "Step 3 of 3 — Review & Deploy"
            };
            _nextBtn.Text = _step == 2 ? "Deploy" : "Next →";
            _nextBtn.BackColor = _step == 2 ? Theme.Green : Theme.Blue;

            _stepHost.Controls.Clear();
            switch (_step)
            {
                case 0: BuildStep1(); break;
                case 1: BuildStep2(); break;
                case 2: BuildStep3(); break;
            }
        }

        private void BuildStep1()
        {
            var p = StepPanel();
            var dropZone = new Panel
            {
                Width = 460, Height = 160,
                Left = 24, Top = 24,
                BackColor = Theme.BlueSoft,
                Cursor = Cursors.Hand,
                AllowDrop = true
            };
            dropZone.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
                e.Graphics.DrawRectangle(pen, 1, 1, dropZone.Width - 2, dropZone.Height - 2);
            };

            var iconLbl = new Label { Text = "⬆", Font = new Font("Segoe UI", 24f), ForeColor = Theme.Blue, AutoSize = true, Left = 200, Top = 28, BackColor = Color.Transparent };
            var mainLbl = new Label { Text = "Drag & drop any file here", Font = Theme.FontBold, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 130, Top = 78, BackColor = Color.Transparent };
            var subLbl  = new Label { Text = "or click to browse — supports all file types", Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 110, Top = 104, BackColor = Color.Transparent };
            dropZone.Controls.AddRange(new Control[] { iconLbl, mainLbl, subLbl });

            dropZone.DragEnter += (_, e) => { if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy; };
            dropZone.DragDrop  += (_, e) =>
            {
                var files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
                if (files?.Length > 0) { _filePath = files[0]; RefreshStep1Card(p, dropZone); }
            };
            dropZone.Click += (_, _) =>
            {
                using var dlg = new OpenFileDialog
                {
                    Title = "Select file to deploy",
                    Filter = "All Files (*.*)|*.*|Installers (*.exe;*.msi)|*.exe;*.msi"
                };
                if (dlg.ShowDialog() == DialogResult.OK) { _filePath = dlg.FileName; RefreshStep1Card(p, dropZone); }
            };

            p.Controls.Add(dropZone);
            _stepHost.Controls.Add(p);
        }

        private void RefreshStep1Card(Panel p, Panel dropZone)
        {
            if (_filePath == null) return;
            dropZone.Controls.Clear();
            var name = Path.GetFileName(_filePath);
            var size = new FileInfo(_filePath).Length / 1024;
            var ok = new Label { Text = $"✓  {name}  ({size} KB)", Font = Theme.FontBold, ForeColor = Theme.Green, AutoSize = true, Left = 20, Top = 68 };
            var change = new LinkLabel { Text = "Change file", Font = Theme.FontSm, LinkColor = Theme.Blue, AutoSize = true, Left = 20, Top = 96 };
            change.LinkClicked += (_, _) =>
            {
                using var dlg = new OpenFileDialog { Filter = "Installers|*.exe;*.msi|All files|*.*" };
                if (dlg.ShowDialog() == DialogResult.OK) { _filePath = dlg.FileName; RefreshStep1Card(p, dropZone); }
            };
            dropZone.Controls.AddRange(new Control[] { ok, change });
            _nextBtn.Enabled = true;
        }

        private void BuildStep2()
        {
            var p = StepPanel();
            int y = 24;
            bool isInstaller = FileManager.IsInstaller(_filePath ?? "");

            p.Controls.Add(FieldLabel("Application / File Name", y));
            _appNameBox = Theme.MakeInput();
            _appNameBox.Text = Path.GetFileNameWithoutExtension(_filePath ?? "");
            _appNameBox.Left = 24; _appNameBox.Top = y + 20; _appNameBox.Width = 460;
            p.Controls.Add(_appNameBox);
            y += 68;

            if (isInstaller)
            {
                p.Controls.Add(FieldLabel("Installer Type  (how was this packaged?)", y));
                _typeCombo = Theme.MakeCombo();
                _typeCombo.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
                _typeCombo.SelectedIndex = 0;
                _typeCombo.Left = 24; _typeCombo.Top = y + 20; _typeCombo.Width = 220;
                p.Controls.Add(_typeCombo);
                y += 68;
            }
            else
            {
                // Non-installer: show info label
                var infoLbl = new Label
                {
                    Text = "ℹ  This file will be downloaded to the client's Downloads folder (not installed).",
                    Font = Theme.FontSm, ForeColor = Theme.Blue,
                    Left = 24, Top = y, Width = 460, Height = 34,
                    BackColor = Theme.BlueSoft
                };
                p.Controls.Add(infoLbl);
                y += 44;
            }

            p.Controls.Add(FieldLabel("Deployment Target", y));
            _targetCombo = Theme.MakeCombo();
            _targetCombo.Items.Add("All Connected Clients");
            foreach (var c in ClientManager.GetOnline())
                _targetCombo.Items.Add($"{c.ComputerName} ({c.IpAddress})");
            _targetCombo.SelectedIndex = 0;
            _targetCombo.Left = 24; _targetCombo.Top = y + 20; _targetCombo.Width = 460;
            p.Controls.Add(_targetCombo);

            _stepHost.Controls.Add(p);
        }

        private void BuildStep3()
        {
            var p = StepPanel();
            bool isInstaller = FileManager.IsInstaller(_filePath ?? "");
            var name   = Path.GetFileName(_filePath ?? "");
            var size   = _filePath != null ? $"{new FileInfo(_filePath).Length / 1024.0 / 1024.0:F1} MB" : "—";
            var type   = isInstaller ? (_typeCombo?.SelectedItem?.ToString() ?? "NSIS") : "Send to Downloads";
            var action = isInstaller ? "Install silently" : "Download to client";
            var target = _targetCombo?.SelectedItem?.ToString() ?? "All Connected Clients";

            int y = 16;
            foreach (var (label, val) in new[] {
                ("File",    name),
                ("Size",    size),
                ("Action",  action),
                ("Type",    type),
                ("Target",  target)
            })
            {
                var row = new Panel { Left = 24, Top = y, Width = 460, Height = 40, BackColor = Theme.BgApp };
                row.Paint += (_, e) =>
                {
                    using var pen = new Pen(Theme.Border, 1);
                    e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
                };
                var lbl  = new Label { Text = label, Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 12, Top = 12, BackColor = Color.Transparent };
                var val2 = new Label { Text = val,   Font = Theme.FontBold, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 160, Top = 12, BackColor = Color.Transparent };
                row.Controls.AddRange(new Control[] { lbl, val2 });
                p.Controls.Add(row);
                y += 42;
            }

            _stepHost.Controls.Add(p);
        }

        private void Next_Click(object? s, EventArgs e)
        {
            if (_step == 0 && _filePath == null) { ToastManager.Show("Please select a file first.", ToastKind.Warning); return; }
            if (_step < 2) { GoStep(_step + 1); return; }

            _nextBtn.Enabled = false;
            _nextBtn.Text = "Deploying...";
            Task.Run(() =>
            {
                try
                {
                    bool isInstaller = FileManager.IsInstaller(_filePath!);
                    var type     = isInstaller ? (_typeCombo?.SelectedItem?.ToString() ?? "NSIS") : "Download";
                    var targets  = ClientManager.GetOnline().Select(c => c.Id).ToList();
                    FileManager.SaveFile(_filePath!, type);
                    var fileName = Path.GetFileName(_filePath!);
                    var fileSize = new FileInfo(_filePath!).Length;
                    var ip       = GetLocalIp();
                    var url      = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(fileName)}";

                    if (targets.Any())
                    {
                        if (isInstaller)
                            CommandDispatcher.IssueInstall(targets, fileName, type, url);
                        else
                            CommandDispatcher.IssueDownload(targets, fileName, url);
                    }

                    AppState.AddDeployment(new DeploymentRecord
                    {
                        FileName = fileName, InstallerType = type,
                        FileSize = fileSize, TargetIds = targets
                    });
                    AppState.Log($"Deployed '{fileName}' [{type}] → {targets.Count} client(s)", LogLevel.Success);
                    ToastManager.Show($"'{fileName}' sent to {targets.Count} client(s).", ToastKind.Success);
                    Invoke(Close);
                }
                catch (Exception ex)
                {
                    AppState.Log($"Deploy failed: {ex.Message}", LogLevel.Error);
                    ToastManager.Show("Deploy failed. See Activity for details.", ToastKind.Error);
                    Invoke(() => { _nextBtn.Enabled = true; _nextBtn.Text = "Deploy"; });
                }
            });
        }

        private static Panel StepPanel() => new() { Dock = DockStyle.Fill, BackColor = Theme.BgCard, Padding = new Padding(0) };
        private static Label FieldLabel(string text, int top) => new() { Text = text, Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 24, Top = top };

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
}
