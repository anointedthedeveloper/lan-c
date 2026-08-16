namespace LanServer.Controls
{
    public enum ToastKind { Info, Success, Warning, Error }

    public static class ToastManager
    {
        private static Form? _owner;
        private static readonly List<ToastPopup> _active = new();
        private const int Margin   = 20;
        private const int ToastH   = 64;
        private const int ToastW   = 380;
        private const int Spacing  = 10;

        public static void Init(Form owner) => _owner = owner;

        public static void Show(string message, ToastKind kind = ToastKind.Info, int durationMs = 3800)
        {
            if (_owner == null || _owner.IsDisposed) return;
            if (_owner.InvokeRequired) { _owner.Invoke(() => Show(message, kind, durationMs)); return; }

            var toast = new ToastPopup(message, kind);
            _active.Add(toast);
            Restack();
            toast.Show(_owner);

            // Fade in
            toast.Opacity = 0;
            var fadeIn = new System.Windows.Forms.Timer { Interval = 14 };
            fadeIn.Tick += (_, _) =>
            {
                if (toast.IsDisposed) { fadeIn.Stop(); fadeIn.Dispose(); return; }
                toast.Opacity += 0.1;
                if (toast.Opacity >= 0.97) { toast.Opacity = 0.97; fadeIn.Stop(); fadeIn.Dispose(); }
            };
            fadeIn.Start();

            // Hold then fade out
            var hold = new System.Windows.Forms.Timer { Interval = durationMs };
            hold.Tick += (_, _) =>
            {
                hold.Stop(); hold.Dispose();
                var fadeOut = new System.Windows.Forms.Timer { Interval = 14 };
                fadeOut.Tick += (_, _) =>
                {
                    if (toast.IsDisposed) { fadeOut.Stop(); fadeOut.Dispose(); return; }
                    toast.Opacity -= 0.07;
                    if (toast.Opacity <= 0)
                    {
                        fadeOut.Stop(); fadeOut.Dispose();
                        if (!toast.IsDisposed) toast.Close();
                        _active.Remove(toast);
                        Restack();
                    }
                };
                fadeOut.Start();
            };
            hold.Start();
        }

        private static void Restack()
        {
            if (_owner == null) return;
            int y = _owner.Bottom - Margin;
            foreach (var t in _active)
            {
                if (t.IsDisposed) continue;
                y -= ToastH + Spacing;
                t.Left = _owner.Right - ToastW - Margin;
                t.Top  = y;
            }
        }
    }

    internal class ToastPopup : Form
    {
        public ToastPopup(string message, ToastKind kind)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar   = false;
            TopMost         = true;
            Width           = 380;
            Height          = 64;

            var (accent, softBg, iconChar, iconFont) = kind switch
            {
                ToastKind.Success => (Theme.Green,   Theme.GreenSoft,  "✓", new Font("Segoe UI", 13f, FontStyle.Bold)),
                ToastKind.Warning => (Theme.Amber,   Theme.AmberSoft,  "!", new Font("Segoe UI", 13f, FontStyle.Bold)),
                ToastKind.Error   => (Theme.Red,     Theme.RedSoft,    "✕", new Font("Segoe UI", 13f, FontStyle.Bold)),
                _                 => (Theme.Blue,    Theme.BlueSoft,   "i", new Font("Segoe UI", 13f, FontStyle.Italic))
            };

            BackColor = softBg;

            // Drop shadow via layered window trick
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Border
                using var borderPen = new Pen(Color.FromArgb(80, accent.R, accent.G, accent.B), 1.5f);
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

                // Left accent stripe
                using var stripeB = new SolidBrush(accent);
                g.FillRectangle(stripeB, 0, 0, 5, Height);

                // Icon circle background
                float cx = 34f, cy = Height / 2f, r = 14f;
                using var circleBg = new SolidBrush(Color.FromArgb(40, accent.R, accent.G, accent.B));
                g.FillEllipse(circleBg, cx - r, cy - r, r * 2, r * 2);
            };

            // Icon label (centered in circle at x=34)
            var iconLbl = new Label
            {
                Text      = iconChar,
                ForeColor = accent,
                Font      = iconFont,
                AutoSize  = true,
                BackColor = Color.Transparent
            };
            iconLbl.Left = 34 - (iconLbl.Width > 0 ? iconLbl.Width / 2 : 8);
            iconLbl.Top  = (64 - iconLbl.Height) / 2;

            // Re-center after layout
            void CenterIcon() {
                iconLbl.Left = 34 - iconLbl.Width / 2;
                iconLbl.Top  = (Height - iconLbl.Height) / 2;
            }
            iconLbl.SizeChanged += (_, _) => CenterIcon();

            // Message label
            var msgLbl = new Label
            {
                Text      = message,
                ForeColor = Theme.TextPrimary,
                Font      = Theme.FontBase,
                Left      = 60,
                Top       = 14,
                Width     = 300,
                Height    = 36,
                AutoSize  = false,
                BackColor = Color.Transparent
            };

            // Close button
            var closeBtn = new Label
            {
                Text      = "×",
                Font      = new Font("Segoe UI", 14f),
                ForeColor = Theme.TextMuted,
                AutoSize  = true,
                Left      = 352,
                Top       = 4,
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand
            };
            closeBtn.MouseEnter += (_, _) => closeBtn.ForeColor = Theme.TextPrimary;
            closeBtn.MouseLeave += (_, _) => closeBtn.ForeColor = Theme.TextMuted;
            closeBtn.Click += (_, _) => { Opacity = 0; Close(); };

            Controls.AddRange(new Control[] { iconLbl, msgLbl, closeBtn });
        }
    }
}
