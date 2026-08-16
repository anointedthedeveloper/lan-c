namespace LanServer.Controls
{
    public class EmptyState : Panel
    {
        public EmptyState(string icon, string title, string subtitle, string? btnText = null, Action? btnAction = null)
        {
            Dock = DockStyle.Fill;
            BackColor = Color.Transparent;

            var inner = new Panel { AutoSize = true, BackColor = Color.Transparent };

            var iconLbl = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 32f),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var titleLbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var subLbl = new Label
            {
                Text = subtitle,
                Font = Theme.FontBase,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            inner.Controls.Add(iconLbl);
            inner.Controls.Add(titleLbl);
            inner.Controls.Add(subLbl);

            if (btnText != null && btnAction != null)
            {
                var btn = Theme.MakeBtn(btnText, Theme.Blue, 36);
                btn.AutoSize = true;
                btn.Padding = new Padding(20, 0, 20, 0);
                btn.Click += (_, _) => btnAction();
                inner.Controls.Add(btn);
            }

            SizeChanged += (_, _) => CenterInner(inner, iconLbl, titleLbl, subLbl);
            Controls.Add(inner);
        }

        private void CenterInner(Panel inner, Label icon, Label title, Label sub)
        {
            if (Width <= 0 || Height <= 0) return;
            int totalH = icon.Height + 10 + title.Height + 6 + sub.Height + (inner.Controls.Count > 3 ? 18 + 36 : 0);
            int y = Math.Max(0, (Height - totalH) / 2);
            int cx = Width / 2;

            icon.Left  = cx - icon.Width / 2;  icon.Top  = y;
            title.Left = cx - title.Width / 2; title.Top = y + icon.Height + 10;
            sub.Left   = cx - sub.Width / 2;   sub.Top   = title.Top + title.Height + 6;

            if (inner.Controls.Count > 3)
            {
                var btn = (Button)inner.Controls[3];
                btn.Left = cx - btn.Width / 2;
                btn.Top  = sub.Top + sub.Height + 18;
            }
        }
    }
}
