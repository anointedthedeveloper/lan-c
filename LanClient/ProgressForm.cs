namespace LanClient
{
    public class ProgressForm : Form
    {
        private ProgressBar _bar = null!;
        private Label _statusLabel = null!;
        private Label _titleLabel = null!;
        private Button _closeBtn = null!;

        private static readonly Color BgDark = Color.FromArgb(18, 18, 18);
        private static readonly Color BgPanel = Color.FromArgb(28, 28, 28);
        private static readonly Color TextPrimary = Color.FromArgb(230, 230, 230);
        private static readonly Color TextSecondary = Color.FromArgb(150, 150, 150);

        public ProgressForm(string title)
        {
            Text = "LanC";
            Size = new Size(440, 200);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = BgDark;
            ForeColor = TextPrimary;
            TopMost = true;

            var titleBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(10, 10, 10) };
            _titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true, Top = 12, Left = 16
            };
            titleBar.Controls.Add(_titleLabel);

            var body = new Panel { Dock = DockStyle.Fill, BackColor = BgPanel, Padding = new Padding(16, 12, 16, 12) };

            _statusLabel = new Label
            {
                Text = "Starting...",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextSecondary,
                Dock = DockStyle.Top,
                Height = 24
            };

            _bar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 18,
                Style = ProgressBarStyle.Continuous,
                Margin = new Padding(0, 6, 0, 6)
            };

            _closeBtn = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 34,
                BackColor = Color.FromArgb(55, 55, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Enabled = false,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            _closeBtn.Click += (s, e) => Close();

            body.Controls.Add(_closeBtn);
            body.Controls.Add(_bar);
            body.Controls.Add(_statusLabel);

            Controls.Add(body);
            Controls.Add(titleBar);
        }

        public void SetStatus(string msg)
        {
            if (InvokeRequired) { Invoke(() => SetStatus(msg)); return; }
            _statusLabel.Text = msg;
        }

        public void SetProgress(int percent)
        {
            if (InvokeRequired) { Invoke(() => SetProgress(percent)); return; }
            _bar.Value = Math.Clamp(percent, 0, 100);
        }

        public void SetDone(bool success, string msg)
        {
            if (InvokeRequired) { Invoke(() => SetDone(success, msg)); return; }
            _statusLabel.Text = msg;
            _statusLabel.ForeColor = success ? Color.FromArgb(100, 220, 120) : Color.FromArgb(220, 100, 80);
            _bar.Value = 100;
            _closeBtn.Enabled = true;
        }
    }
}
