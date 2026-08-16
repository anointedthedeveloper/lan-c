namespace LanServer.Controls
{
    public class ConfirmDialog : Form
    {
        public ConfirmDialog(string title, string body, string confirmText = "Confirm", bool danger = true)
        {
            Text = title;
            Size = new Size(440, 210);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;

            // Top accent bar
            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 4,
                BackColor = danger ? Theme.Red : Theme.Blue
            };

            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontLg,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 24, Top = 28,
                BackColor = Color.Transparent
            };
            var bodyLbl = new Label
            {
                Text = body,
                Font = Theme.FontBase,
                ForeColor = Theme.TextSecond,
                Left = 24, Top = 60, Width = 392,
                AutoSize = false, Height = 48,
                BackColor = Color.Transparent
            };

            var sep = new Panel { Left = 0, Top = 118, Width = 440, Height = 1, BackColor = Theme.Border };

            var cancelBtn = Theme.MakeOutlineBtn("Cancel", Theme.TextSecond);
            cancelBtn.Left = 24; cancelBtn.Top = 130; cancelBtn.Width = 120;
            cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var confirmBtn = Theme.MakeBtn(confirmText, danger ? Theme.Red : Theme.Blue);
            confirmBtn.Left = 160; confirmBtn.Top = 130; confirmBtn.Width = 256;
            confirmBtn.Click += (_, _) => { DialogResult = DialogResult.Yes; Close(); };

            Controls.AddRange(new Control[] { titleLbl, bodyLbl, sep, cancelBtn, confirmBtn, accentBar });
        }

        public static bool Ask(IWin32Window owner, string title, string body,
                               string confirmText = "Confirm", bool danger = true)
        {
            using var dlg = new ConfirmDialog(title, body, confirmText, danger);
            return dlg.ShowDialog(owner) == DialogResult.Yes;
        }
    }
}
