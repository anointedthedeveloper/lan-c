namespace LanClient
{
    public class ProgressForm : Form
    {
        private ProgressBar _bar    = null!;
        private Label _statusLabel  = null!;
        private Button _closeBtn    = null!;

        private static readonly Color BgApp   = Color.FromArgb(245, 247, 250);
        private static readonly Color BgCard  = Color.FromArgb(255, 255, 255);
        private static readonly Color BgCard2 = Color.FromArgb(248, 250, 253);
        private static readonly Color Border  = Color.FromArgb(226, 232, 240);
        private static readonly Color Blue    = Color.FromArgb(37,  99,  235);
        private static readonly Color Green   = Color.FromArgb(5,  150, 105);
        private static readonly Color Red     = Color.FromArgb(220,  38,  38);
        private static readonly Color TxtPri  = Color.FromArgb(15,  23,  42);
        private static readonly Color TxtSec  = Color.FromArgb(71,  85, 105);

        public ProgressForm(string title)
        {
            Text = "LanC";
            Size = new Size(440, 190);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = BgApp;
            ForeColor = TxtPri;
            TopMost = true;
            Font = new Font("Segoe UI", 9f);

            // Top accent bar
            var accentBar = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Blue };

            // Title bar
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = BgCard };
            titleBar.Paint += (_, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, titleBar.Height - 1, titleBar.Width, titleBar.Height - 1);
            };
            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TxtPri,
                AutoSize = true,
                Left = 20, Top = 14,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(titleLbl);

            // Body
            var body = new Panel { Dock = DockStyle.Fill, BackColor = BgCard, Padding = new Padding(20, 14, 20, 14) };
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
                Height = 24,
                BackColor = Color.Transparent
            };

            _bar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 8,
                Style = ProgressBarStyle.Continuous,
                BackColor = BgCard2,
                ForeColor = Blue,
                Margin = new Padding(0, 8, 0, 8)
            };

            _closeBtn = new Button
            {
                Text = "Close",
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = BgCard2,
                ForeColor = TxtSec,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Enabled = false,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Border }
            };
            _closeBtn.MouseEnter += (_, _) => { if (_closeBtn.Enabled) { _closeBtn.BackColor = Color.FromArgb(230, 230, 240); } };
            _closeBtn.MouseLeave += (_, _) => { _closeBtn.BackColor = BgCard2; };
            _closeBtn.Click += (_, _) => Close();

            body.Controls.Add(_closeBtn);
            body.Controls.Add(_bar);
            body.Controls.Add(_statusLabel);

            Controls.Add(body);
            Controls.Add(titleBar);
            Controls.Add(accentBar);
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
            _closeBtn.BackColor    = success ? Color.FromArgb(236, 253, 245) : Color.FromArgb(254, 242, 242);
            _closeBtn.FlatAppearance.BorderColor = success
                ? Color.FromArgb(167, 243, 208)
                : Color.FromArgb(254, 202, 202);
        }
    }
}
