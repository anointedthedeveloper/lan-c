using LanServer.Controls;

namespace LanServer.Pages
{
    public class DashboardPage : Panel
    {
        private readonly StatCard _statClients;
        private readonly StatCard _statDeploys;
        private readonly StatCard _statUptime;
        private readonly RichTextBox _recentLog;
        private readonly System.Windows.Forms.Timer _uptimeTimer;

        // Quick deploy state
        private string? _pendingFilePath;
        private readonly Label _dropLabel;
        private readonly Label _dropSub;
        private readonly Panel _dropZone;
        private readonly Button _deployNowBtn;
        private readonly ComboBox _typeCombo;

        public event Action? NavigateToActivity;
        public event Action? NavigateToFileManager { add { } remove { } }

        public DashboardPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Dashboard", "Overview of your LanC server");

            // ── Scroll container ──────────────────────────────────────────────
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.BgApp };
            var content = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = Theme.BgApp,
                Padding = new Padding(24, 20, 24, 24)
            };

            // ── Stat cards row ────────────────────────────────────────────────
            var statsRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Height = 118,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            statsRow.SizeChanged += (_, _) => statsRow.Width = content.Width - 48;

            var statServer = new StatCard("Server Status", "ONLINE", "All systems operational", Theme.Green);
            _statClients   = new StatCard("Connected Clients", "0", "0 online / 0 total", Theme.Blue);
            _statDeploys   = new StatCard("Deployments", "0", "No deployments yet", Theme.Purple);
            _statUptime    = new StatCard("Uptime", "00:00", "Since last server start", Theme.Amber);

            statsRow.Controls.AddRange(new Control[] { statServer, _statClients, _statDeploys, _statUptime });

            // ── Middle row: Quick Deploy + Recent Activity ────────────────────
            var midRow = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            midRow.SizeChanged += (_, _) => midRow.Width = content.Width - 48;

            // Quick Deploy card
            var deployCard = MakeCard("Quick Deploy", "Deploy an application to your LanC environment.");
            var deployBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(16, 0, 16, 16) };

            var typeRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 12) };
            var typeLbl = Theme.MakeLabel("Installer Type", Theme.TextSecond, Theme.FontSm);
            typeLbl.Margin = new Padding(0, 6, 8, 0);
            _typeCombo = Theme.MakeCombo();
            _typeCombo.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
            _typeCombo.SelectedIndex = 0;
            _typeCombo.Width = 160; _typeCombo.Height = 30;
            typeRow.Controls.AddRange(new Control[] { typeLbl, _typeCombo });

            // Drop zone
            _dropZone = new Panel
            {
                Height = 130,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(12, 37, 99, 235),
                Margin = new Padding(0, 0, 0, 12),
                Cursor = Cursors.Hand
            };
            _dropZone.Paint += PaintDropZone;
            _dropZone.AllowDrop = true;
            _dropZone.DragEnter += DropZone_DragEnter;
            _dropZone.DragDrop  += DropZone_DragDrop;
            _dropZone.Click     += DropZone_Click;

            _dropLabel = new Label
            {
                Text = "⬆  Drag & drop your installer here",
                Font = Theme.FontBold,
                ForeColor = Theme.Blue,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _dropSub = new Label
            {
                Text = "or click to browse",
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            _dropZone.Controls.AddRange(new Control[] { _dropLabel, _dropSub });
            _dropZone.SizeChanged += (_, _) => CenterDropLabels();

            _deployNowBtn = Theme.MakeBtn("▶  Deploy Now", Theme.Green);
            _deployNowBtn.Dock = DockStyle.Top;
            _deployNowBtn.Enabled = false;
            _deployNowBtn.Click += DeployNow_Click;

            deployBody.Controls.Add(_deployNowBtn);
            deployBody.Controls.Add(_dropZone);
            deployBody.Controls.Add(typeRow);
            deployCard.Controls.Add(deployBody);

            // Recent Activity card
            var actCard = MakeCard("Recent Activity", null, viewAllAction: () => NavigateToActivity?.Invoke());
            var actBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 8) };
            _recentLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(9, 13, 22),
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontMono,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            actBody.Controls.Add(_recentLog);
            actCard.Controls.Add(actBody);

            midRow.Controls.Add(deployCard, 0, 0);
            midRow.Controls.Add(actCard, 1, 0);

            // ── Services row ──────────────────────────────────────────────────
            var svcRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Height = 90,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 0)
            };
            svcRow.SizeChanged += (_, _) => svcRow.Width = content.Width - 48;

            svcRow.Controls.Add(MakeServiceCard("HTTP Server",      $"Port: {Config.Current.HttpPort}",      "Running", Theme.Green));
            svcRow.Controls.Add(MakeServiceCard("WebSocket Server", $"Port: {Config.Current.WebSocketPort}", "Running", Theme.Green));
            svcRow.Controls.Add(MakeServiceCard("UDP Beacon",       $"Port: {Config.Current.UdpPort}",       "Running", Theme.Green));
            svcRow.Controls.Add(MakeServiceCard("Server Info",      "Version: 1.0.0",                        "Production", Theme.Blue));

            content.Controls.AddRange(new Control[] { statsRow, midRow, svcRow });
            scroll.Controls.Add(content);

            Controls.Add(scroll);
            Controls.Add(header);

            // ── Wire up live data ─────────────────────────────────────────────
            ClientManager.ClientsChanged += RefreshClients;
            AppState.DeploymentsChanged  += RefreshDeploys;
            AppState.LogAdded            += OnLogAdded;

            _uptimeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uptimeTimer.Tick += (_, _) => _statUptime.Update(AppState.UptimeString, "Since last server start");
            _uptimeTimer.Start();

            RefreshClients();
            RefreshDeploys();

            // Seed existing logs
            foreach (var e in AppState.GetLogs().TakeLast(20))
                AppendRecentLog(e);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Panel MakeCard(string title, string? subtitle, Action? viewAllAction = null)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 12, 0)
            };
            card.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = subtitle != null ? 52 : 40, BackColor = Color.Transparent, Padding = new Padding(16, 10, 16, 0) };
            hdr.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            };

            var titleLbl = new Label { Text = title, Font = Theme.FontBold, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 16, Top = 10 };
            hdr.Controls.Add(titleLbl);

            if (subtitle != null)
            {
                var subLbl = new Label { Text = subtitle, Font = Theme.FontSm, ForeColor = Theme.TextSecond, AutoSize = true, Left = 16, Top = 30 };
                hdr.Controls.Add(subLbl);
            }

            if (viewAllAction != null)
            {
                var viewAll = new LinkLabel { Text = "View All", Font = Theme.FontSm, LinkColor = Theme.Blue, AutoSize = true, Top = 12 };
                viewAll.LinkClicked += (_, _) => viewAllAction();
                hdr.SizeChanged += (_, _) => viewAll.Left = hdr.Width - viewAll.Width - 16;
                hdr.Controls.Add(viewAll);
            }

            card.Controls.Add(hdr);
            return card;
        }

        private static Panel MakeServiceCard(string name, string detail, string status, Color statusColor)
        {
            var p = new Panel
            {
                Width = 220, Height = 80,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 12, 0)
            };
            p.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };

            var nameLbl   = new Label { Text = name,   Font = Theme.FontBold, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 16, Top = 12 };
            var detailLbl = new Label { Text = detail,  Font = Theme.FontSm,   ForeColor = Theme.TextSecond,  AutoSize = true, Left = 16, Top = 32 };
            var statusLbl = new Label { Text = $"● {status}", Font = Theme.FontSm, ForeColor = statusColor, AutoSize = true, Left = 16, Top = 52 };
            p.Controls.AddRange(new Control[] { nameLbl, detailLbl, statusLbl });
            return p;
        }

        private void PaintDropZone(object? s, PaintEventArgs e)
        {
            var r = new Rectangle(1, 1, _dropZone.Width - 2, _dropZone.Height - 2);
            using var pen = new Pen(Theme.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, r);
        }

        private void CenterDropLabels()
        {
            _dropLabel.Left = (_dropZone.Width - _dropLabel.Width) / 2;
            _dropLabel.Top  = (_dropZone.Height - _dropLabel.Height - _dropSub.Height - 4) / 2;
            _dropSub.Left   = (_dropZone.Width - _dropSub.Width) / 2;
            _dropSub.Top    = _dropLabel.Top + _dropLabel.Height + 4;
        }

        private void DropZone_DragEnter(object? s, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void DropZone_DragDrop(object? s, DragEventArgs e)
        {
            var files = (string[]?)e.Data?.GetData(DataFormats.FileDrop);
            if (files?.Length > 0) SetPendingFile(files[0]);
        }

        private void DropZone_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "Installers|*.exe;*.msi|All files|*.*" };
            if (dlg.ShowDialog() == DialogResult.OK) SetPendingFile(dlg.FileName);
        }

        private void SetPendingFile(string path)
        {
            _pendingFilePath = path;
            var name = Path.GetFileName(path);
            var size = new FileInfo(path).Length / 1024;
            _dropLabel.Text = $"✓  {name}";
            _dropLabel.ForeColor = Theme.Green;
            _dropSub.Text = $"{size} KB — click to change";
            _dropSub.ForeColor = Theme.TextSecond;
            _deployNowBtn.Enabled = true;
            CenterDropLabels();
        }

        private void DeployNow_Click(object? s, EventArgs e)
        {
            if (_pendingFilePath == null) return;
            var type = _typeCombo.SelectedItem?.ToString() ?? "NSIS";
            _deployNowBtn.Enabled = false;
            _deployNowBtn.Text = "Uploading...";

            Task.Run(() =>
            {
                try
                {
                    FileManager.SaveFile(_pendingFilePath, type);
                    var fileName = Path.GetFileName(_pendingFilePath);
                    var fileSize = new FileInfo(_pendingFilePath).Length;
                    var targets  = ClientManager.GetOnline().Select(c => c.Id).ToList();
                    var ip       = GetLocalIp();
                    var url      = $"http://{ip}:{Config.Current.HttpPort}/{Uri.EscapeDataString(fileName)}";

                    if (targets.Any())
                        CommandDispatcher.IssueInstall(targets, fileName, type, url);

                    AppState.AddDeployment(new DeploymentRecord
                    {
                        FileName = fileName, InstallerType = type,
                        FileSize = fileSize, TargetIds = targets
                    });
                    AppState.Log($"Deployed '{fileName}' [{type}] → {targets.Count} client(s)", LogLevel.Success);
                    ToastManager.Show($"'{fileName}' deployed successfully.", ToastKind.Success);
                }
                catch (Exception ex)
                {
                    AppState.Log($"Deploy failed: {ex.Message}", LogLevel.Error);
                    ToastManager.Show("Deploy failed. See Activity for details.", ToastKind.Error);
                }
                finally
                {
                    if (!IsDisposed) Invoke(() =>
                    {
                        _deployNowBtn.Text = "▶  Deploy Now";
                        _deployNowBtn.Enabled = false;
                        _pendingFilePath = null;
                        _dropLabel.Text = "⬆  Drag & drop your installer here";
                        _dropLabel.ForeColor = Theme.Blue;
                        _dropSub.Text = "or click to browse";
                        _dropSub.ForeColor = Theme.TextSecond;
                        CenterDropLabels();
                    });
                }
            });
        }

        private void RefreshClients()
        {
            if (InvokeRequired) { Invoke(RefreshClients); return; }
            int online = ClientManager.GetOnline().Count();
            int total  = ClientManager.GetAll().Count();
            _statClients.Update(online.ToString(), $"{online} online / {total} total");
        }

        private void RefreshDeploys()
        {
            if (InvokeRequired) { Invoke(RefreshDeploys); return; }
            int count = AppState.GetDeployments().Count;
            _statDeploys.Update(count.ToString(), count == 0 ? "No deployments yet" : $"{count} deployment(s)");
        }

        private void OnLogAdded(LogEntry entry)
        {
            if (InvokeRequired) { Invoke(() => OnLogAdded(entry)); return; }
            AppendRecentLog(entry);
            // Keep only last 50 lines
            while (_recentLog.Lines.Length > 50)
            {
                _recentLog.Select(0, _recentLog.Lines[0].Length + 1);
                _recentLog.SelectedText = "";
            }
        }

        private void AppendRecentLog(LogEntry entry)
        {
            _recentLog.SelectionStart = _recentLog.TextLength;
            _recentLog.SelectionColor = Theme.TextMuted;
            _recentLog.AppendText($"[{entry.Time:HH:mm:ss}] ");
            _recentLog.SelectionColor = entry.Level switch
            {
                LogLevel.Success => Theme.Green,
                LogLevel.Error   => Theme.Red,
                LogLevel.Warning => Theme.Amber,
                _                => Theme.TextPrimary
            };
            _recentLog.AppendText(entry.Message + "\n");
            _recentLog.ScrollToCaret();
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uptimeTimer.Stop();
                _uptimeTimer.Dispose();
                ClientManager.ClientsChanged -= RefreshClients;
                AppState.DeploymentsChanged  -= RefreshDeploys;
                AppState.LogAdded            -= OnLogAdded;
            }
            base.Dispose(disposing);
        }
    }
}
