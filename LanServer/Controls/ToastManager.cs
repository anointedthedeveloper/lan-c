namespace LanServer.Controls
{
    public enum ToastKind { Info, Success, Warning, Error }

    public static class ToastManager
    {
        private static Form? _owner;
        private static readonly List<ToastPopup> _active = new();
        private const int Margin = 16;
        private const int ToastH = 56;

        public static void Init(Form owner) => _owner = owner;

        public static void Show(string message, ToastKind kind = ToastKind.Info, int durationMs = 3500)
        {
            if (_owner == null || _owner.IsDisposed) return;
            if (_owner.InvokeRequired) { _owner.Invoke(() => Show(message, kind, durationMs)); return; }

            var toast = new ToastPopup(message, kind);
            _active.Add(toast);
            Restack();
            toast.Show(_owner);

            // Fade in
            toast.Opacity = 0;
            var fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            double targetOpacity = 0.97;
            fadeTimer.Tick += (_, _) =>
            {
                toast.Opacity += 0.08;
                if (toast.Opacity >= targetOpacity) { toast.Opacity = targetOpacity; fadeTimer.Stop(); fadeTimer.Dispose(); }
            };
            fadeTimer.Start();

            var holdTimer = new System.Windows.Forms.Timer { Interval = durationMs };
            holdTimer.Tick += (_, _) =>
            {
                holdTimer.Stop(); holdTimer.Dispose();
                // Fade out
                var fadeOut = new System.Windows.Forms.Timer { Interval = 16 };
                fadeOut.Tick += (_, _) =>
                {
                    if (toast.IsDisposed) { fadeOut.Stop(); fadeOut.Dispose(); return; }
                    toast.Opacity -= 0.08;
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
            holdTimer.Start();
        }

        private static void Restack()
        {
            if (_owner == null) return;
            int y = _owner.Bottom - Margin;
            foreach (var t in _active)
            {
                y -= ToastH + 8;
                t.Left = _owner.Right - t.Width - Margin;
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
            Width           = 360;
            Height          = 56;
            BackColor       = Theme.BgCard;

            var accent = kind switch
            {
                ToastKind.Success => Theme.Green,
                ToastKind.Warning => Theme.Amber,
                ToastKind.Error   => Theme.Red,
                _                 => Theme.Blue
            };

            var softBg = kind switch
            {
                ToastKind.Success => Theme.GreenSoft,
                ToastKind.Warning => Theme.AmberSoft,
                ToastKind.Error   => Theme.RedSoft,
                _                 => Theme.BlueSoft
            };

            var icon = kind switch
            {
                ToastKind.Success => "✓",
                ToastKind.Warning => "⚠",
                ToastKind.Error   => "✕",
                _                 => "ℹ"
            };

            BackColor = softBg;

            Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(60, accent.R, accent.G, accent.B), 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                using var bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 4, Height);
            };

            var iconLbl = new Label
            {
                Text = icon,
                ForeColor = accent,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Left = 16, Top = 14,
                BackColor = Color.Transparent
            };
            var msgLbl = new Label
            {
                Text = message,
                ForeColor = Theme.TextPrimary,
                Font = Theme.FontBase,
                Left = 40, Top = 18,
                Width = 304,
                AutoSize = false,
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { iconLbl, msgLbl });
        }
    }
}
