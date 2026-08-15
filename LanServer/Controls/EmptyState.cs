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
                Font = new Font("Segoe UI", 28f),
                ForeColor = Theme.TextMuted,
                AutoSize = true
            };
            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontBold,
                ForeColor = Theme.TextSecond,
                AutoSize = true
            };
            var subLbl = new Label
            {
                Text = subtitle,
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                AutoSize = true
            };

            inner.Controls.Add(iconLbl);
            inner.Controls.Add(titleLbl);
            inner.Controls.Add(subLbl);

            if (btnText != null && btnAction != null)
            {
                var btn = Theme.MakeBtn(btnText, Theme.Blue);
                btn.AutoSize = true;
                btn.Padding = new Padding(16, 0, 16, 0);
                btn.Click += (_, _) => btnAction();
                inner.Controls.Add(btn);
            }

            // Stack vertically centered
            SizeChanged += (_, _) => CenterInner(inner, iconLbl, titleLbl, subLbl);
            Controls.Add(inner);
        }

        private void CenterInner(Panel inner, Label icon, Label title, Label sub)
        {
            int totalH = icon.Height + 8 + title.Height + 4 + sub.Height + (inner.Controls.Count > 3 ? 16 + 34 : 0);
            int y = (Height - totalH) / 2;
            int cx = Width / 2;

            icon.Left  = cx - icon.Width / 2;  icon.Top  = y;
            title.Left = cx - title.Width / 2; title.Top = y + icon.Height + 8;
            sub.Left   = cx - sub.Width / 2;   sub.Top   = title.Top + title.Height + 4;

            if (inner.Controls.Count > 3)
            {
                var btn = (Button)inner.Controls[3];
                btn.Left = cx - btn.Width / 2;
                btn.Top  = sub.Top + sub.Height + 16;
            }
        }
    }
}
