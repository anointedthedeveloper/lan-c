namespace LanClient
{
    public class ProgressForm : Form
    {
        private ProgressBar _bar    = null!;
        private Label _statusLabel  = null!;
        private Button _closeBtn    = null!;

        private static readonly Color BgApp   = Color.FromArgb(8,  13, 24);
        private static readonly Color BgCard  = Color.FromArgb(17, 26, 43);
        private static readonly Color BgCard2 = Color.FromArgb(22, 34, 56);
        private static readonly Color Border  = Color.FromArgb(36, 51, 77);
        private static readonly Color Blue    = Color.FromArgb(37,  99, 235);
        private static readonly Color Green   = Color.FromArgb(16, 185, 129);
        private static readonly Color Red     = Color.FromArgb(225, 29,  72);
        private static readonly Color TxtPri  = Color.FromArgb(248, 250, 252);
        private static readonly Color TxtSec  = Color.FromArgb(148, 163, 184);
        private static readonly Color TxtMuted= Color.FromArgb(71,  85, 105);

        public ProgressForm(string title)
        {
            Text = "LanC";
            Size = new Size(420, 180);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = BgApp;
            ForeColor = TxtPri;
            TopMost = true;
            Font = new Font("Segoe UI", 9f);

            // Title bar
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(6, 10, 18) };
            titleBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Blue, 1);
                e.Graphics.DrawLine(pen, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };
            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TxtPri,
                AutoSize = true, Left = 16, Top = 14
            };
            titleBar.Controls.Add(titleLbl);

            // Body
            var body = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Padding = new Padding(20, 12, 20, 12) };
            body.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, body.Width - 1, body.Height - 1);
            };

            _statusLabel = new Label
            {
                Text = "Starting...",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = TxtSec,
                Dock = DockStyle.Top,
                Height = 22
            };

            _bar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 6,
                Style = ProgressBarStyle.Continuous,
                BackColor = BgCard2,
                ForeColor = Blue,
                Margin = new Padding(0, 8, 0, 8)
            };

            _closeBtn = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = BgCard2,
                ForeColor = TxtSec,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Border }
            };
            _closeBtn.Click += (_, _) => Close();

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
            _statusLabel.Text      = msg;
            _statusLabel.ForeColor = success ? Green : Red;
            _bar.Value             = 100;
            _bar.ForeColor         = success ? Green : Red;
            _closeBtn.Enabled      = true;
            _closeBtn.ForeColor    = success ? Green : Red;
            _closeBtn.FlatAppearance.BorderColor = success ? Green : Red;
        }
    }
}
