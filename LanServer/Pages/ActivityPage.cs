using LanServer.Controls;

namespace LanServer.Pages
{
    public class ActivityPage : Panel
    {
        private readonly RichTextBox _logBox;
        private readonly ComboBox _levelFilter;
        private readonly TextBox _searchBox;
        private bool _paused;
        private readonly Button _pauseBtn;
        private bool _autoScroll = true;

        public ActivityPage()
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.BgApp;

            var header = new PageHeader("Activity Console", "Real-time server logs and activities.");

            // ── Toolbar ───────────────────────────────────────────────────────
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.BgCard, Padding = new Padding(24, 8, 24, 8) };
            toolbar.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };

            _levelFilter = Theme.MakeCombo();
            _levelFilter.Items.AddRange(new[] { "All Levels", "INFO", "SUCCESS", "WARNING", "ERROR", "DEBUG" });
            _levelFilter.SelectedIndex = 0;
            _levelFilter.Width = 130; _levelFilter.Top = 10; _levelFilter.Left = 24;
            _levelFilter.SelectedIndexChanged += (_, _) => RebuildLog();

            _searchBox = Theme.MakeInput();
            _searchBox.Width = 200; _searchBox.Top = 10; _searchBox.Left = 166;
            _searchBox.TextChanged += (_, _) => RebuildLog();

            _pauseBtn = Theme.MakeOutlineBtn("⏸  Pause", Theme.Amber);
            _pauseBtn.Width = 90; _pauseBtn.Top = 10; _pauseBtn.Left = 380;
            _pauseBtn.Click += (_, _) =>
            {
                _paused = !_paused;
                _pauseBtn.Text = _paused ? "▶  Resume" : "⏸  Pause";
                _pauseBtn.ForeColor = _paused ? Theme.Green : Theme.Amber;
                _pauseBtn.FlatAppearance.BorderColor = _paused ? Theme.Green : Theme.Amber;
            };

            var autoScrollChk = new CheckBox
            {
                Text = "Auto-scroll",
                Checked = true,
                ForeColor = Theme.TextSecond,
                Font = Theme.FontSm,
                BackColor = Color.Transparent,
                AutoSize = true,
                Left = 484, Top = 14
            };
            autoScrollChk.CheckedChanged += (_, _) => _autoScroll = autoScrollChk.Checked;

            var clearBtn = Theme.MakeBtn("Clear", Theme.RedMuted);
            clearBtn.Width = 80; clearBtn.Top = 10;
            clearBtn.Click += (_, _) =>
            {
                if (!ConfirmDialog.Ask(FindForm()!, "Clear Logs?", "This will remove all activity log entries.", "Clear Logs")) return;
                AppState.ClearLogs();
                _logBox?.Clear();
                ToastManager.Show("Logs cleared.", ToastKind.Info);
            };
            toolbar.SizeChanged += (_, _) => clearBtn.Left = toolbar.Width - clearBtn.Width - 24;
            toolbar.Controls.AddRange(new Control[] { _levelFilter, _searchBox, _pauseBtn, autoScrollChk, clearBtn });

            // ── Log box ───────────────────────────────────────────────────────
            var logPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.BgApp, Padding = new Padding(24, 16, 24, 24) };
            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgCard2,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontMono,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            logPanel.Controls.Add(_logBox);

            Controls.Add(logPanel);
            Controls.Add(toolbar);
            Controls.Add(header);

            // Seed existing logs
            RebuildLog();

            // Subscribe to new entries
            AppState.LogAdded += OnLogAdded;
        }

        private void RebuildLog()
        {
            if (InvokeRequired) { Invoke(RebuildLog); return; }
            _logBox.Clear();
            var q     = _searchBox.Text.Trim().ToLower();
            var level = _levelFilter.SelectedItem?.ToString() ?? "All Levels";

            foreach (var entry in AppState.GetLogs())
            {
                if (level != "All Levels" && !entry.Level.ToString().Equals(level, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(q) && !entry.Message.ToLower().Contains(q)) continue;
                AppendEntry(entry);
            }
        }

        private void OnLogAdded(LogEntry entry)
        {
            if (_paused) return;
            if (InvokeRequired) { Invoke(() => OnLogAdded(entry)); return; }

            var q     = _searchBox.Text.Trim().ToLower();
            var level = _levelFilter.SelectedItem?.ToString() ?? "All Levels";
            if (level != "All Levels" && !entry.Level.ToString().Equals(level, StringComparison.OrdinalIgnoreCase)) return;
            if (!string.IsNullOrEmpty(q) && !entry.Message.ToLower().Contains(q)) return;

            AppendEntry(entry);
        }

        private void AppendEntry(LogEntry entry)
        {
            _logBox.SelectionStart = _logBox.TextLength;

            // Timestamp
            _logBox.SelectionColor = Theme.TextMuted;
            _logBox.AppendText($"{entry.Time:HH:mm:ss}  ");

            // Level badge
            var (levelColor, levelText) = entry.Level switch
            {
                LogLevel.Success => (Theme.Green,  "SUCCESS"),
                LogLevel.Warning => (Theme.Amber,  "WARNING"),
                LogLevel.Error   => (Theme.Red,    "ERROR  "),
                LogLevel.Debug   => (Theme.Purple, "DEBUG  "),
                _                => (Theme.Blue,   "INFO   ")
            };
            _logBox.SelectionColor = levelColor;
            _logBox.AppendText($"{levelText}  ");

            // Message
            _logBox.SelectionColor = entry.Level == LogLevel.Error   ? Theme.Red
                                   : entry.Level == LogLevel.Warning ? Theme.Amber
                                   : Theme.TextPrimary;
            _logBox.AppendText(entry.Message + "\n");

            if (_autoScroll) _logBox.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AppState.LogAdded -= OnLogAdded;
            base.Dispose(disposing);
        }
    }
}
