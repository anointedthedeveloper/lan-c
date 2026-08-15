namespace LanServer.Controls
{
    public class ConfirmDialog : Form
    {
        public ConfirmDialog(string title, string body, string confirmText = "Confirm", bool danger = true)
        {
            Text = title;
            Size = new Size(420, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;

            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontBold,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 24, Top = 24
            };
            var bodyLbl = new Label
            {
                Text = body,
                Font = Theme.FontBase,
                ForeColor = Theme.TextSecond,
                Left = 24, Top = 52, Width = 372,
                AutoSize = false, Height = 48
            };

            var cancelBtn = Theme.MakeOutlineBtn("Cancel", Theme.TextSecond);
            cancelBtn.Left = 24; cancelBtn.Top = 120; cancelBtn.Width = 120;
            cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            var confirmBtn = Theme.MakeBtn(confirmText, danger ? Theme.Red : Theme.Blue);
            confirmBtn.Left = 156; confirmBtn.Top = 120; confirmBtn.Width = 240;
            confirmBtn.Click += (_, _) => { DialogResult = DialogResult.Yes; Close(); };

            Controls.AddRange(new Control[] { titleLbl, bodyLbl, cancelBtn, confirmBtn });
        }

        public static bool Ask(IWin32Window owner, string title, string body,
                               string confirmText = "Confirm", bool danger = true)
        {
            using var dlg = new ConfirmDialog(title, body, confirmText, danger);
            return dlg.ShowDialog(owner) == DialogResult.Yes;
        }
    }
}
