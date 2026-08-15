using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanServer
{
    public class MainForm : Form
    {
        private ListView _clientList = null!;
        private RichTextBox _logBox = null!;
        private ListView _fileList = null!;
        private Button _uploadBtn = null!;
        private Button _deployBtn = null!;
        private Button _downloadBtn = null!;
        private Button _shutdownBtn = null!;
        private Button _deleteFileBtn = null!;
        private ComboBox _installerType = null!;
        private Label _serverInfoLabel = null!;
        private CheckBox _allClientsCheck = null!;
        private Label _clientCountLabel = null!;

        private readonly WebServer _webServer = new();
        private readonly UdpBeacon _beacon = new();
        private List<ManagedFile> _files = new();

        // Colour palette — pulled from server.ico blue theme
        private static readonly Color BgBase      = Color.FromArgb(10, 14, 26);
        private static readonly Color BgCard      = Color.FromArgb(16, 22, 40);
        private static readonly Color BgInput     = Color.FromArgb(22, 30, 54);
        private static readonly Color BgRow       = Color.FromArgb(20, 28, 50);
        private static readonly Color AccentBlue  = Color.FromArgb(30, 100, 200);
        private static readonly Color AccentLight = Color.FromArgb(100, 180, 255);
        private static readonly Color AccentGreen = Color.FromArgb(22, 190, 110);
        private static readonly Color AccentRed   = Color.FromArgb(210, 60, 60);
        private static readonly Color AccentAmber = Color.FromArgb(200, 140, 30);
        private static readonly Color TextPrimary = Color.FromArgb(220, 230, 255);
        private static readonly Color TextMuted   = Color.FromArgb(100, 120, 170);
        private static readonly Color Border      = Color.FromArgb(30, 50, 90);

        public MainForm()
        {
            InitializeUI();
            ClientManager.ClientsChanged += RefreshClientList;
            _webServer.LogMessage += AppendLog;
            _webServer.Start();
            _beacon.Start();
            RefreshFileList();
            ShowServerInfo();
            AppendLog("LanC Server started.");
        }

        private void ShowServerInfo()
        {
            var ip = GetLocalIp();
            _serverInfoLabel.Text =
                $"  ●  {ip}   │   WS :{Config.Current.WebSocketPort}   │   HTTP :{Config.Current.HttpPort}   │   Web: http://{ip}:{Config.Current.HttpPort}";
        }

        private string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                        return addr.Address.ToString();
                }
            }
            return "127.0.0.1";
        }

        private void RefreshClientList()
        {
            if (InvokeRequired) { Invoke(RefreshClientList); return; }
            _clientList.Items.Clear();
            foreach (var c in ClientManager.GetAll())
            {
                var item = new ListViewItem(c.ComputerName) { Tag = c.Id };
                item.SubItems.Add(c.IpAddress);
                item.SubItems.Add(c.IsOnline ? "● Online" : "○ Offline");
                item.SubItems.Add(c.LastSeen.ToString("HH:mm:ss"));
                item.ForeColor = c.IsOnline ? AccentGreen : TextMuted;
                item.BackColor = BgRow;
                _clientList.Items.Add(item);
            }
            int online = ClientManager.GetOnline().Count();
            _clientCountLabel.Text = $"{online} online  /  {ClientManager.GetAll().Count()} total";
        }

        private void RefreshFileList()
        {
            _files = FileManager.GetFiles();
            _fileList.Items.Clear();
            foreach (var f in _files)
            {
                var item = new ListViewItem(f.FileName);
                item.SubItems.Add($"{f.FileSize / 1024} KB");
                item.BackColor = BgRow;
                item.ForeColor = TextPrimary;
                _fileList.Items.Add(item);
            }
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired) { Invoke(() => AppendLog(msg)); return; }
            var time = $"[{DateTime.Now:HH:mm:ss}] ";
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = TextMuted;
            _logBox.AppendText(time);
            _logBox.SelectionColor = TextPrimary;
            _logBox.AppendText(msg + "\n");
            _logBox.ScrollToCaret();
        }

        private IEnumerable<string> GetTargetIds()
        {
            if (_allClientsCheck.Checked)
                return ClientManager.GetOnline().Select(c => c.Id);
            return _clientList.SelectedItems.Cast<ListViewItem>()
                .Select(i => i.Tag?.ToString() ?? "")
                .Where(id => !string.IsNullOrEmpty(id));
        }

        private void UploadBtn_Click(object? s, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Filter = "Installers|*.exe;*.msi|All files|*.*" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var type = _installerType.SelectedItem?.ToString() ?? "NSIS";
            FileManager.SaveFile(dlg.FileName, type);
            RefreshFileList();
            AppendLog($"Uploaded: {Path.GetFileName(dlg.FileName)}");
        }

        private void DeployBtn_Click(object? s, EventArgs e)
        {
            if (_fileList.SelectedItems.Count == 0) { ShowMsg("Select a file to deploy."); return; }
            var file = _files[_fileList.SelectedIndices[0]];
            var type = _installerType.SelectedItem?.ToString() ?? "NSIS";
            var url = $"http://{GetLocalIp()}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            var targets = GetTargetIds().ToList();
            if (!targets.Any()) { ShowMsg("No clients selected or connected."); return; }
            CommandDispatcher.IssueInstall(targets, file.FileName, type, url);
            AppendLog($"Deploy & Install '{file.FileName}' [{type}] → {targets.Count} client(s)");
        }

        private void DownloadBtn_Click(object? s, EventArgs e)
        {
            if (_fileList.SelectedItems.Count == 0) { ShowMsg("Select a file."); return; }
            var file = _files[_fileList.SelectedIndices[0]];
            var url = $"http://{GetLocalIp()}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            var targets = GetTargetIds().ToList();
            if (!targets.Any()) { ShowMsg("No clients selected or connected."); return; }
            CommandDispatcher.IssueDownload(targets, file.FileName, url);
            AppendLog($"Download '{file.FileName}' → {targets.Count} client(s)");
        }

        private void ShutdownBtn_Click(object? s, EventArgs e)
        {
            var targets = GetTargetIds().ToList();
            if (!targets.Any()) { ShowMsg("No clients selected or connected."); return; }
            if (MessageBox.Show($"Shutdown {targets.Count} machine(s)?", "Confirm Shutdown",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            CommandDispatcher.IssueShutdown(targets);
            AppendLog($"Shutdown issued → {targets.Count} client(s)");
        }

        private void DeleteFileBtn_Click(object? s, EventArgs e)
        {
            if (_fileList.SelectedItems.Count == 0) return;
            var file = _files[_fileList.SelectedIndices[0]];
            if (MessageBox.Show($"Delete '{file.FileName}'?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            FileManager.DeleteFile(file.FileName);
            RefreshFileList();
            AppendLog($"Deleted: {file.FileName}");
        }

        private static void ShowMsg(string msg) =>
            MessageBox.Show(msg, "LanC Server", MessageBoxButtons.OK, MessageBoxIcon.Information);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _webServer.Stop();
            _beacon.Stop();
            base.OnFormClosing(e);
        }

        private void InitializeUI()
        {
            Text = "LanC Server";
            Size = new Size(1160, 740);
            MinimumSize = new Size(960, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgBase;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9f);

            // ── Title bar ──
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = BgCard,
                Padding = new Padding(16, 0, 16, 0)
            };
            titleBar.Paint += (s, e) =>
            {
                // bottom border line
                using var pen = new Pen(AccentBlue, 2);
                e.Graphics.DrawLine(pen, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };
            var titleLabel = new Label
            {
                Text = "LanC",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Top = 10, Left = 16
            };
            var titleSub = new Label
            {
                Text = "Server Control Panel",
                Font = new Font("Segoe UI", 9f),
                ForeColor = AccentLight,
                AutoSize = true,
                Top = 14, Left = 72
            };
            titleBar.Controls.AddRange(new Control[] { titleLabel, titleSub });

            // ── Info bar ──
            _serverInfoLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AccentLight,
                BackColor = Color.FromArgb(12, 18, 34),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "  Starting..."
            };

            // ── Main layout ──
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgBase,
                Padding = new Padding(12, 10, 12, 12)
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ── LEFT CARD: Clients ──
            var leftCard = MakeCard();
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(14)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // header
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // count
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));  // checkbox
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // list
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));  // shutdown

            var clientsHeader = MakeSectionHeader("Connected Clients", "●");

            _clientCountLabel = new Label
            {
                Text = "0 online  /  0 total",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _allClientsCheck = new CheckBox
            {
                Text = "Target all connected clients",
                Checked = true,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            StyleCheckBox(_allClientsCheck);

            _clientList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = BgInput,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f),
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _clientList.Columns.Add("Computer", 130);
            _clientList.Columns.Add("IP", 105);
            _clientList.Columns.Add("Status", 80);
            _clientList.Columns.Add("Seen", 55);
            StyleListView(_clientList);

            _shutdownBtn = MakeButton("⏻  Shutdown Selected", AccentRed);
            _shutdownBtn.Dock = DockStyle.Fill;
            _shutdownBtn.Click += ShutdownBtn_Click;

            leftLayout.Controls.Add(clientsHeader, 0, 0);
            leftLayout.Controls.Add(_clientCountLabel, 0, 1);
            leftLayout.Controls.Add(_allClientsCheck, 0, 2);
            leftLayout.Controls.Add(_clientList, 0, 3);
            leftLayout.Controls.Add(_shutdownBtn, 0, 4);
            leftCard.Controls.Add(leftLayout);

            // ── RIGHT CARD ──
            var rightCard = MakeCard();
            rightCard.Margin = new Padding(10, 0, 0, 0);
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 7,
                ColumnCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(14)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // files header
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));  // file list
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // type row
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // action buttons
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));   // spacer
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // log header
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // log

            var filesHeader = MakeSectionHeader("Uploaded Files", "▤");

            _fileList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = BgInput,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 8.5f),
                MultiSelect = false,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _fileList.Columns.Add("File Name", -2);
            _fileList.Columns.Add("Size", 70);
            StyleListView(_fileList);

            // Type + upload row
            var typeRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            typeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            typeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            typeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            typeRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

            var typeLabel = new Label
            {
                Text = "Installer Type",
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5f),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _installerType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = BgInput,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 4)
            };
            _installerType.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
            _installerType.SelectedIndex = 0;

            _deleteFileBtn = MakeButton("✕  Delete", AccentRed);
            _deleteFileBtn.Dock = DockStyle.Fill;
            _deleteFileBtn.Margin = new Padding(0, 4, 0, 4);
            _deleteFileBtn.Click += DeleteFileBtn_Click;

            _uploadBtn = MakeButton("↑  Upload File", AccentBlue);
            _uploadBtn.Dock = DockStyle.Fill;
            _uploadBtn.Margin = new Padding(0, 4, 0, 4);
            _uploadBtn.Click += UploadBtn_Click;

            typeRow.Controls.Add(typeLabel, 0, 0);
            typeRow.Controls.Add(_installerType, 1, 0);
            typeRow.Controls.Add(_deleteFileBtn, 2, 0);
            typeRow.Controls.Add(_uploadBtn, 3, 0);

            // Action buttons row
            var btnRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            btnRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _deployBtn = MakeButton("▶  Deploy & Install", AccentGreen);
            _deployBtn.Dock = DockStyle.Fill;
            _deployBtn.Margin = new Padding(0, 0, 6, 0);
            _deployBtn.Click += DeployBtn_Click;

            _downloadBtn = MakeButton("↓  Download Only", AccentAmber);
            _downloadBtn.Dock = DockStyle.Fill;
            _downloadBtn.Margin = new Padding(6, 0, 0, 0);
            _downloadBtn.Click += DownloadBtn_Click;

            btnRow.Controls.Add(_deployBtn, 0, 0);
            btnRow.Controls.Add(_downloadBtn, 1, 0);

            var spacer = new Panel { BackColor = Color.Transparent, Dock = DockStyle.Fill };

            var logHeader = MakeSectionHeader("Activity Log", "≡");

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(8, 12, 22),
                ForeColor = TextPrimary,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            rightLayout.Controls.Add(filesHeader, 0, 0);
            rightLayout.Controls.Add(_fileList, 0, 1);
            rightLayout.Controls.Add(typeRow, 0, 2);
            rightLayout.Controls.Add(btnRow, 0, 3);
            rightLayout.Controls.Add(spacer, 0, 4);
            rightLayout.Controls.Add(logHeader, 0, 5);
            rightLayout.Controls.Add(_logBox, 0, 6);
            rightCard.Controls.Add(rightLayout);

            mainTable.Controls.Add(leftCard, 0, 0);
            mainTable.Controls.Add(rightCard, 1, 0);

            Controls.Add(mainTable);
            Controls.Add(_serverInfoLabel);
            Controls.Add(titleBar);
        }

        private static Panel MakeCard()
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgCard,
                Margin = new Padding(0)
            };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private static Panel MakeSectionHeader(string text, string icon)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var accent = new Panel
            {
                Width = 3,
                Dock = DockStyle.Left,
                BackColor = AccentBlue
            };
            var lbl = new Label
            {
                Text = $"  {icon}  {text}",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = AccentLight,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            p.Controls.Add(lbl);
            p.Controls.Add(accent);
            return p;
        }

        private static Button MakeButton(string text, Color back)
        {
            var b = new Button
            {
                Text = text,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Height = 34,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Light(back, 0.15f);
            b.MouseLeave += (s, e) => b.BackColor = back;
            return b;
        }

        private static void StyleListView(ListView lv)
        {
            lv.OwnerDraw = true;
            lv.DrawColumnHeader += (s, e) =>
            {
                using var bg = new SolidBrush(BgCard);
                e.Graphics.FillRectangle(bg, e.Bounds);
                using var pen = new Pen(Border);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                using var txt = new SolidBrush(TextMuted);
                e.Graphics.DrawString(e.Header?.Text ?? "", new Font("Segoe UI", 8f, FontStyle.Bold),
                    txt, e.Bounds.X + 6, e.Bounds.Y + 5);
            };
            lv.DrawItem += (s, e) => e.DrawDefault = true;
            lv.DrawSubItem += (s, e) => e.DrawDefault = true;
        }

        private static void StyleCheckBox(CheckBox cb)
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.FlatAppearance.BorderColor = AccentBlue;
            cb.FlatAppearance.CheckedBackColor = AccentBlue;
        }
    }
}
