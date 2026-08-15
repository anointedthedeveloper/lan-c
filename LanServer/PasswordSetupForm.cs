namespace LanServer
{
    public class PasswordSetupForm : Form
    {
        private TextBox _passBox = null!;
        private TextBox _confirmBox = null!;
        public string Password { get; private set; } = "admin234";

        public PasswordSetupForm()
        {
            Text = "LanC - First Launch Setup";
            Size = new Size(360, 220);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            var lbl = new Label { Text = "Set Admin Password (default: admin234)", ForeColor = Color.White, Font = new Font("Segoe UI", 9), AutoSize = true, Top = 20, Left = 20 };
            _passBox = new TextBox { PasswordChar = '*', Top = 50, Left = 20, Width = 300, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var lbl2 = new Label { Text = "Confirm Password", ForeColor = Color.White, Font = new Font("Segoe UI", 9), AutoSize = true, Top = 85, Left = 20 };
            _confirmBox = new TextBox { PasswordChar = '*', Top = 108, Left = 20, Width = 300, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };

            var btn = new Button
            {
                Text = "Set Password & Start",
                Top = 145, Left = 20, Width = 300, Height = 34,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_passBox.Text))
                {
                    Password = "admin234";
                    DialogResult = DialogResult.OK;
                    return;
                }
                if (_passBox.Text != _confirmBox.Text)
                {
                    MessageBox.Show("Passwords do not match.");
                    return;
                }
                Password = _passBox.Text;
                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { lbl, _passBox, lbl2, _confirmBox, btn });
        }
    }
}
