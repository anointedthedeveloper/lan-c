namespace LanServer
{
    public class PasswordSetupForm : Form
    {
        private TextBox _passBox = null!;
        private TextBox _confirmBox = null!;
        public string Password { get; private set; } = "admin234";

        private static readonly Color BgBase     = Color.FromArgb(10, 14, 26);
        private static readonly Color BgCard     = Color.FromArgb(16, 22, 40);
        private static readonly Color BgInput    = Color.FromArgb(22, 30, 54);
        private static readonly Color AccentBlue = Color.FromArgb(30, 100, 200);
        private static readonly Color AccentLight = Color.FromArgb(100, 180, 255);
        private static readonly Color TextPrimary = Color.FromArgb(220, 230, 255);
        private static readonly Color TextMuted   = Color.FromArgb(100, 120, 170);
        private static readonly Color Border      = Color.FromArgb(30, 50, 90);

        public PasswordSetupForm()
        {
            Text = "LanC Server — First Launch";
            Size = new Size(400, 310);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = BgBase;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9f);

            // Header strip
            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = BgCard };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(AccentBlue, 2);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };
            var hTitle = new Label
            {
                Text = "LanC",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Top = 10, Left = 16
            };
            var hSub = new Label
            {
                Text = "Set your admin password to get started",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = AccentLight,
                AutoSize = true,
                Top = 38, Left = 18
            };
            header.Controls.AddRange(new Control[] { hTitle, hSub });

            // Body
            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 16), BackColor = BgBase };

            var lbl1 = MakeLabel("Admin Password", 0);
            _passBox = MakeInput(28);
            _passBox.PasswordChar = '●';

            var lbl2 = MakeLabel("Confirm Password", 72);
            _confirmBox = MakeInput(100);
            _confirmBox.PasswordChar = '●';

            var hint = new Label
            {
                Text = "Leave blank to use default: admin234",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                AutoSize = true,
                Top = 148, Left = 0
            };

            var btn = new Button
            {
                Text = "Set Password & Launch Server",
                Top = 172, Left = 0, Width = 352, Height = 38,
                BackColor = AccentBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(AccentBlue, 0.15f);
            btn.MouseLeave += (s, e) => btn.BackColor = AccentBlue;
            btn.Click += Btn_Click;

            body.Controls.AddRange(new Control[] { lbl1, _passBox, lbl2, _confirmBox, hint, btn });

            Controls.Add(body);
            Controls.Add(header);
        }

        private void Btn_Click(object? s, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_passBox.Text))
            {
                Password = "admin234";
                DialogResult = DialogResult.OK;
                return;
            }
            if (_passBox.Text != _confirmBox.Text)
            {
                MessageBox.Show("Passwords do not match.", "LanC", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Password = _passBox.Text;
            DialogResult = DialogResult.OK;
        }

        private static Label MakeLabel(string text, int top) => new()
        {
            Text = text,
            ForeColor = Color.FromArgb(100, 120, 170),
            Font = new Font("Segoe UI", 8f),
            AutoSize = true,
            Top = top, Left = 0
        };

        private TextBox MakeInput(int top) => new()
        {
            Top = top, Left = 0, Width = 352, Height = 32,
            BackColor = BgInput,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f)
        };
    }
}
