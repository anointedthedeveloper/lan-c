namespace LanServer.Controls
{
    public class StatCard : Panel
    {
        private readonly Label _valueLabel;
        private readonly Label _supportLabel;
        private readonly Color _accent;
        private bool _hovered;

        public StatCard(string title, string value, string support, Color accent)
        {
            _accent = accent;

            Dock = DockStyle.Fill;
            Height = 110;
            BackColor = Theme.BgCard;
            Margin = new Padding(0, 0, 12, 0);
            Cursor = Cursors.Default;

            Paint += OnPaint;
            MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
            MouseLeave += (_, _) => { _hovered = false; Invalidate(); };

            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 20, Top = 20,
                BackColor = Color.Transparent
            };

            _valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 20, Top = 38,
                BackColor = Color.Transparent
            };

            _supportLabel = new Label
            {
                Text = support,
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Left = 20, Top = 80,
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { titleLbl, _valueLabel, _supportLabel });
        }

        private void OnPaint(object? s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Hover tint background
            if (_hovered)
            {
                using var hoverBrush = new SolidBrush(Color.FromArgb(8, _accent.R, _accent.G, _accent.B));
                g.FillRectangle(hoverBrush, 0, 0, Width, Height);
            }

            // Card border — glows on hover
            using var pen = new Pen(_hovered ? Color.FromArgb(160, _accent.R, _accent.G, _accent.B) : Theme.Border, 1.5f);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            // Top accent bar
            using var accentBrush = new SolidBrush(_accent);
            g.FillRectangle(accentBrush, 0, 0, Width, 3);
        }

        public void Update(string value, string support)
        {
            _valueLabel.Text   = value;
            _supportLabel.Text = support;
        }
    }
}
