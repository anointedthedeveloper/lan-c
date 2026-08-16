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

            // ── Root layout: header top, rest fills ───────────────────────────
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Theme.BgApp,
                Padding = new Padding(20, 16, 20, 20)
            };
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 118)); // stat cards
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // mid row (fills)
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));  // service cards

            // ── Row 0: Stat cards ─────────────────────────────────────────────
            var statsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 0)
            };
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            var statServer = new StatCard("Server Status", "ONLINE", "All systems operational", Theme.Green);
            statServer.Margin = new Padding(0, 0, 10, 0);
            _statClients = new StatCard("Connected Clients", "0", "0 online / 0 total", Theme.Blue);
            _statClients.Margin = new Padding(0, 0, 10, 0);
            _statDeploys = new StatCard("Deployments", "0", "No deployments yet", Theme.Purple);
            _statDeploys.Margin = new Padding(0, 0, 10, 0);
            _statUptime = new StatCard("Uptime", "00:00", "Since last server start", Theme.Amber);
            _statUptime.Margin = new Padding(0, 0, 0, 0);

            statsRow.Controls.Add(statServer,   0, 0);
            statsRow.Controls.Add(_statClients, 1, 0);
            statsRow.Controls.Add(_statDeploys, 2, 0);
            statsRow.Controls.Add(_statUptime,  3, 0);

            // ── Row 1: Middle row (Quick Deploy + Recent Activity) ────────────
            var midRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 12, 0, 12)
            };
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            midRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            // ── Quick Deploy card ─────────────────────────────────────────────
            var deployCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 10, 0)
            };
            deployCard.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, deployCard.Width - 1, deployCard.Height - 1);
            };

            var deployHdr = MakeCardHeader("Quick Deploy", "Deploy an application to your LanC environment.");

            var deployBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(16, 12, 16, 16)
            };
            deployBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // type row
            deployBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // drop zone
            deployBody.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));  // deploy button

            // Installer type row
            var typeRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };
            var typeLbl = new Label
            {
                Text = "Installer Type",
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 0, Top = 8
            };
            _typeCombo = Theme.MakeCombo();
            _typeCombo.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
            _typeCombo.SelectedIndex = 0;
            _typeCombo.Left = 100; _typeCombo.Top = 2; _typeCombo.Width = 160; _typeCombo.Height = 30;
            typeRow.Controls.AddRange(new Control[] { typeLbl, _typeCombo });

            // Drop zone
            _dropZone = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BlueSoft,
                Margin = new Padding(0, 0, 0, 10),
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

            // Deploy button
            _deployNowBtn = Theme.MakeBtn("▶  Deploy Now", Theme.Green);
            _deployNowBtn.Dock = DockStyle.Fill;
            _deployNowBtn.Enabled = false;
            _deployNowBtn.Margin = new Padding(0);
            _deployNowBtn.Click += DeployNow_Click;

            deployBody.Controls.Add(typeRow,         0, 0);
            deployBody.Controls.Add(_dropZone,       0, 1);
            deployBody.Controls.Add(_deployNowBtn,   0, 2);

            deployCard.Controls.Add(deployBody);
            deployCard.Controls.Add(deployHdr);

            // ── Recent Activity card ──────────────────────────────────────────
            var actCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard,
                Margin = new Padding(0, 0, 0, 0)
            };
            actCard.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, actCard.Width - 1, actCard.Height - 1);
            };

            var actHdr = MakeCardHeader("Recent Activity", null, viewAllAction: () => NavigateToActivity?.Invoke());

            var actBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 10, 12, 12)
            };
            _recentLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard2,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontMono,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            actBody.Controls.Add(_recentLog);
            actCard.Controls.Add(actBody);
            actCard.Controls.Add(actHdr);
            actCard.Controls.Add(actHdr);

            midRow.Controls.Add(deployCard, 0, 0);
            midRow.Controls.Add(actCard,    1, 0);

            // ── Row 2: Service cards ──────────────────────────────────────────
            var svcRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 1,
                ColumnCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 0)
            };
            svcRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            svcRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            svcRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            svcRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            var svc1 = MakeServiceCard("HTTP Server",      $"Port: {Config.Current.HttpPort}",      "Running",    Theme.Green);
            var svc2 = MakeServiceCard("WebSocket Server", $"Port: {Config.Current.WebSocketPort}", "Running",    Theme.Green);
            var svc3 = MakeServiceCard("UDP Beacon",       $"Port: {Config.Current.UdpPort}",       "Running",    Theme.Green);
            var svc4 = MakeServiceCard("Server Info",      "Version: 1.0.0",                        "Production", Theme.Blue);
            svc1.Margin = new Padding(0, 0, 10, 0);
            svc2.Margin = new Padding(0, 0, 10, 0);
            svc3.Margin = new Padding(0, 0, 10, 0);
            svc4.Margin = new Padding(0, 0, 0,  0);

            svcRow.Controls.Add(svc1, 0, 0);
            svcRow.Controls.Add(svc2, 1, 0);
            svcRow.Controls.Add(svc3, 2, 0);
            svcRow.Controls.Add(svc4, 3, 0);

            body.Controls.Add(statsRow, 0, 0);
            body.Controls.Add(midRow,   0, 1);
            body.Controls.Add(svcRow,   0, 2);

            Controls.Add(body);
            Controls.Add(header);

            // ── Wire up live data ─────────────────────────────────────────────
            ClientManager.ClientsChanged += RefreshClients;
            AppState.DeploymentsChanged  += RefreshDeploys;
            AppState.LogAdded            += OnLogAdded;

            _uptimeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uptimeTimer.Tick += (_, _) =>
            {
                if (!IsDisposed) _statUptime.Update(AppState.UptimeString, "Since last server start");
            };
            _uptimeTimer.Start();

            RefreshClients();
            RefreshDeploys();

            foreach (var e in AppState.GetLogs().TakeLast(30))
                AppendRecentLog(e);
        }

        // ── Card header factory ───────────────────────────────────────────────

        private static Panel MakeCardHeader(string title, string? subtitle, Action? viewAllAction = null)
        {
            int h = subtitle != null ? 54 : 42;
            var hdr = new Panel
            {
                Dock = DockStyle.Top,
                Height = h,
                BackColor = Color.Transparent,
                Padding = new Padding(16, 10, 16, 0)
            };
            hdr.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
            };

            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontBold,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 0, Top = 8
            };
            hdr.Controls.Add(titleLbl);

            if (subtitle != null)
            {
                var subLbl = new Label
                {
                    Text = subtitle,
                    Font = Theme.FontSm,
                    ForeColor = Theme.TextSecond,
                    AutoSize = true,
                    Left = 0, Top = 28
                };
                hdr.Controls.Add(subLbl);
            }

            if (viewAllAction != null)
            {
                var viewAll = new LinkLabel
                {
                    Text = "View All",
                    Font = Theme.FontSm,
                    LinkColor = Theme.Blue,
                    AutoSize = true,
                    Top = 12
                };
                viewAll.LinkClicked += (_, _) => viewAllAction();
                hdr.SizeChanged += (_, _) => viewAll.Left = hdr.Width - viewAll.Width - 0;
                hdr.Controls.Add(viewAll);
            }

            return hdr;
        }

        private static Panel MakeServiceCard(string name, string detail, string status, Color statusColor)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard
            };

            bool hovered = false;
            p.Paint += (_, e) =>
            {
                using var pen = new Pen(hovered ? Color.FromArgb(160, Theme.Blue.R, Theme.Blue.G, Theme.Blue.B) : Theme.Border, 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                if (hovered)
                {
                    using var hb = new SolidBrush(Theme.BgHover);
                    e.Graphics.FillRectangle(hb, 1, 1, p.Width - 2, p.Height - 2);
                }
            };
            p.MouseEnter += (_, _) => { hovered = true; p.Invalidate(); };
            p.MouseLeave += (_, _) => { hovered = false; p.Invalidate(); };

            // Colored left accent
            var accentBar = new Panel { Width = 3, Dock = DockStyle.Left, BackColor = statusColor };

            var nameLbl   = new Label { Text = name,          Font = Theme.FontBold, ForeColor = Theme.TextPrimary, AutoSize = true, Left = 20, Top = 14, BackColor = Color.Transparent };
            var detailLbl = new Label { Text = detail,         Font = Theme.FontSm,   ForeColor = Theme.TextSecond,  AutoSize = true, Left = 20, Top = 36, BackColor = Color.Transparent };
            var statusLbl = new Label { Text = $"● {status}", Font = Theme.FontSm,   ForeColor = statusColor,       AutoSize = true, Left = 20, Top = 56, BackColor = Color.Transparent };
            p.Controls.AddRange(new Control[] { accentBar, nameLbl, detailLbl, statusLbl });
            return p;
        }

        // ── Drop zone ─────────────────────────────────────────────────────────

        private void PaintDropZone(object? s, PaintEventArgs e)
        {
            var r = new Rectangle(1, 1, _dropZone.Width - 2, _dropZone.Height - 2);
            using var pen = new Pen(Theme.Blue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            e.Graphics.DrawRectangle(pen, r);
        }

        private void CenterDropLabels()
        {
            if (_dropZone.Width <= 0 || _dropZone.Height <= 0) return;
            int totalH = _dropLabel.Height + 6 + _dropSub.Height;
            int startY = (_dropZone.Height - totalH) / 2;
            _dropLabel.Left = (_dropZone.Width - _dropLabel.Width) / 2;
            _dropLabel.Top  = startY;
            _dropSub.Left   = (_dropZone.Width - _dropSub.Width) / 2;
            _dropSub.Top    = startY + _dropLabel.Height + 6;
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
            _dropSub.Text = $"{size} KB  ·  click to change";
            _dropSub.ForeColor = Theme.TextSecond;
            _deployNowBtn.Enabled = true;
            CenterDropLabels();
        }

        // ── Deploy ────────────────────────────────────────────────────────────

        private void DeployNow_Click(object? s, EventArgs e)
        {
            if (_pendingFilePath == null) return;
            var type = _typeCombo.SelectedItem?.ToString() ?? "NSIS";
            _deployNowBtn.Enabled = false;
            _deployNowBtn.Text = "Deploying...";

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

        // ── Live data refresh ─────────────────────────────────────────────────

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
            while (_recentLog.Lines.Length > 60)
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
