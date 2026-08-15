namespace LanServer.Controls
{
    public class StatCard : Panel
    {
        private readonly Label _valueLabel;
        private readonly Label _supportLabel;

        public StatCard(string title, string value, string support, Color accent)
        {
            Width = 220;
            Height = 110;
            BackColor = Theme.BgCard;
            Margin = new Padding(0, 0, 12, 0);
            Padding = new Padding(20, 16, 20, 16);
            Cursor = Cursors.Default;

            Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                // top accent line
                using var accentBrush = new SolidBrush(accent);
                e.Graphics.FillRectangle(accentBrush, 0, 0, Width, 3);
            };

            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 20, Top = 20
            };

            _valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 20, Top = 40
            };

            _supportLabel = new Label
            {
                Text = support,
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 20, Top = 80
            };

            Controls.AddRange(new Control[] { titleLbl, _valueLabel, _supportLabel });
        }

        public void Update(string value, string support)
        {
            _valueLabel.Text   = value;
            _supportLabel.Text = support;
        }
    }
}
