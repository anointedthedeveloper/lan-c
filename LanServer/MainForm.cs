using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanServer
{
    public class MainForm : Form
    {
        private ListView _clientList = null!;
        private ListBox _logBox = null!;
        private ListBox _fileList = null!;
        private Button _uploadBtn = null!;
        private Button _deployBtn = null!;
        private Button _downloadBtn = null!;
        private Button _shutdownBtn = null!;
        private ComboBox _installerType = null!;
        private Label _serverInfoLabel = null!;
        private CheckBox _allClientsCheck = null!;

        private readonly WebServer _webServer = new();
        private readonly UdpBeacon _beacon = new();
        private List<ManagedFile> _files = new();

        private static readonly Color BgDark = Color.FromArgb(18, 18, 18);
        private static readonly Color BgPanel = Color.FromArgb(28, 28, 28);
        private static readonly Color BgControl = Color.FromArgb(42, 42, 42);
        private static readonly Color Accent = Color.FromArgb(0, 122, 204);
        private static readonly Color AccentGreen = Color.FromArgb(22, 160, 90);
        private static readonly Color AccentRed = Color.FromArgb(192, 57, 43);
        private static readonly Color AccentPurple = Color.FromArgb(90, 80, 180);
        private static readonly Color TextPrimary = Color.FromArgb(230, 230, 230);
        private static readonly Color TextSecondary = Color.FromArgb(150, 150, 150);

        public MainForm()
        {
            InitializeUI();
            ClientManager.ClientsChanged += RefreshClientList;
            _webServer.LogMessage += AppendLog;
            _webServer.Start();
            _beacon.Start();
            RefreshFileList();
            ShowServerInfo();
            AppendLog("LanC Server started successfully.");
        }

        private void ShowServerInfo()
        {
            var ip = GetLocalIp();
            _serverInfoLabel.Text =
                $"  Server IP: {ip}     WS Port: {Config.Current.WebSocketPort}     HTTP Port: {Config.Current.HttpPort}     Web Access: http://{ip}:{Config.Current.HttpPort}";
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
                var item = new ListViewItem(c.ComputerName);
                item.SubItems.Add(c.IpAddress);
                item.SubItems.Add(c.IsOnline ? "Online" : "Offline");
                item.SubItems.Add(c.LastSeen.ToString("HH:mm:ss"));
                item.Tag = c.Id;
                item.ForeColor = c.IsOnline ? Color.FromArgb(100, 220, 120) : TextSecondary;
                _clientList.Items.Add(item);
            }
        }

        private void RefreshFileList()
        {
            _files = FileManager.GetFiles();
            _fileList.Items.Clear();
            foreach (var f in _files)
                _fileList.Items.Add($"  {f.FileName}  ({f.FileSize / 1024} KB)");
        }

        private void AppendLog(string msg)
        {
            if (InvokeRequired) { Invoke(() => AppendLog(msg)); return; }
            _logBox.Items.Add($"[{DateTime.Now:HH:mm:ss}]  {msg}");
            _logBox.TopIndex = _logBox.Items.Count - 1;
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
            if (_fileList.SelectedIndex < 0) { MessageBox.Show("Select a file to deploy.", "LanC"); return; }
            var file = _files[_fileList.SelectedIndex];
            var type = _installerType.SelectedItem?.ToString() ?? "NSIS";
            var url = $"http://{GetLocalIp()}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            var targets = GetTargetIds().ToList();
            if (!targets.Any()) { MessageBox.Show("No clients selected or connected.", "LanC"); return; }
            CommandDispatcher.IssueInstall(targets, file.FileName, type, url);
            AppendLog($"Deploy & Install '{file.FileName}' [{type}] → {targets.Count} client(s)");
        }

        private void DownloadBtn_Click(object? s, EventArgs e)
        {
            if (_fileList.SelectedIndex < 0) { MessageBox.Show("Select a file.", "LanC"); return; }
            var file = _files[_fileList.SelectedIndex];
            var url = $"http://{GetLocalIp()}:{Config.Current.HttpPort}/{Uri.EscapeDataString(file.FileName)}";
            var targets = GetTargetIds().ToList();
            if (!targets.Any()) { MessageBox.Show("No clients selected or connected.", "LanC"); return; }
            CommandDispatcher.IssueDownload(targets, file.FileName, url);
            AppendLog($"Download '{file.FileName}' → {targets.Count} client(s)");
        }

        private void ShutdownBtn_Click(object? s, EventArgs e)
        {
            var targets = GetTargetIds().ToList();
            if (!targets.Any()) { MessageBox.Show("No clients selected or connected.", "LanC"); return; }
            if (MessageBox.Show($"Shutdown {targets.Count} machine(s)?", "Confirm Shutdown", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            CommandDispatcher.IssueShutdown(targets);
            AppendLog($"Shutdown issued → {targets.Count} client(s)");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _webServer.Stop();
            _beacon.Stop();
            base.OnFormClosing(e);
        }

        private void InitializeUI()
        {
            Text = "LanC Server";
            Size = new Size(1100, 700);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = TextPrimary;

            // Top info bar
            _serverInfoLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(100, 180, 255),
                BackColor = Color.FromArgb(10, 10, 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "  Starting server..."
            };

            // Title bar
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(10, 10, 10) };
            var titleLabel = new Label
            {
                Text = "LanC  Server",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Top = 10, Left = 16
            };
            var subTitle = new Label
            {
                Text = "LAN Command & Control",
                Font = new Font("Segoe UI", 8f),
                ForeColor = TextSecondary,
                AutoSize = true,
                Top = 32, Left = 18
            };
            titleBar.Controls.AddRange(new Control[] { titleLabel, subTitle });

            // Main split
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = BgDark,
                Padding = new Padding(8)
            };
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // ── LEFT: Clients ──
            var leftPanel = MakePanel();
            var clientsHeader = MakeSectionHeader("Connected Clients");

            _allClientsCheck = new CheckBox
            {
                Text = "Target All Connected Clients",
                Checked = true,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 6)
            };

            _clientList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = BgControl,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                MultiSelect = true,
                BorderStyle = BorderStyle.None,
                OwnerDraw = false
            };
            _clientList.Columns.Add("Computer Name", 148);
            _clientList.Columns.Add("IP Address", 108);
            _clientList.Columns.Add("Status", 65);
            _clientList.Columns.Add("Last Seen", 65);

            _shutdownBtn = MakeButton("⏻  Shutdown", AccentRed, 160);
            _shutdownBtn.Dock = DockStyle.Bottom;
            _shutdownBtn.Click += ShutdownBtn_Click;

            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 4,
                ColumnCount = 1,
                BackColor = BgPanel,
                Padding = new Padding(10)
            };
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            leftLayout.Controls.Add(clientsHeader, 0, 0);
            leftLayout.Controls.Add(_allClientsCheck, 0, 1);
            leftLayout.Controls.Add(_clientList, 0, 2);
            leftLayout.Controls.Add(_shutdownBtn, 0, 3);

            // ── RIGHT: Files + Log ──
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                BackColor = BgPanel,
                Padding = new Padding(10),
                Margin = new Padding(8, 0, 0, 0)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // files header
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150)); // file list
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); // type row
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // buttons
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); // log header
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // log

            var filesHeader = MakeSectionHeader("Uploaded Files");

            _fileList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = BgControl,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None
            };

            // Type row
            var typeRow = new Panel { Dock = DockStyle.Fill, BackColor = BgPanel };
            var typeLabel = new Label
            {
                Text = "Installer Type:",
                ForeColor = TextSecondary,
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                Top = 8, Left = 0
            };
            _installerType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = BgControl,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Left = 110, Top = 4, Width = 180
            };
            _installerType.Items.AddRange(new[] { "NSIS", "Inno Setup", "MSI", "InstallShield" });
            _installerType.SelectedIndex = 0;
            typeRow.Controls.AddRange(new Control[] { typeLabel, _installerType });

            // Buttons row
            var btnRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 4, 0, 0)
            };
            _uploadBtn = MakeButton("↑  Upload File", Accent, 148);
            _deployBtn = MakeButton("▶  Deploy & Install", AccentGreen, 160);
            _downloadBtn = MakeButton("↓  Download Only", AccentPurple, 152);
            _uploadBtn.Click += UploadBtn_Click;
            _deployBtn.Click += DeployBtn_Click;
            _downloadBtn.Click += DownloadBtn_Click;
            btnRow.Controls.AddRange(new Control[] { _uploadBtn, _deployBtn, _downloadBtn });

            var logHeader = MakeSectionHeader("Command Log");

            _logBox = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12),
                ForeColor = Color.FromArgb(100, 220, 120),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None
            };

            rightLayout.Controls.Add(filesHeader, 0, 0);
            rightLayout.Controls.Add(_fileList, 0, 1);
            rightLayout.Controls.Add(typeRow, 0, 2);
            rightLayout.Controls.Add(btnRow, 0, 3);
            rightLayout.Controls.Add(logHeader, 0, 4);
            rightLayout.Controls.Add(_logBox, 0, 5);

            mainTable.Controls.Add(leftLayout, 0, 0);
            mainTable.Controls.Add(rightLayout, 1, 0);

            Controls.Add(mainTable);
            Controls.Add(_serverInfoLabel);
            Controls.Add(titleBar);
        }

        private static Panel MakePanel() => new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 28, 28), Padding = new Padding(10) };

        private static Label MakeSectionHeader(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 180, 255),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static Button MakeButton(string text, Color back, int width) => new()
        {
            Text = text,
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f),
            Height = 34,
            Width = width,
            Margin = new Padding(0, 0, 6, 0),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };
    }
}
