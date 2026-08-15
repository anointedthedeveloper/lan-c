namespace LanServer.Controls
{
    public enum ToastKind { Info, Success, Warning, Error }

    public static class ToastManager
    {
        private static Form? _owner;
        private static readonly List<ToastPopup> _active = new();
        private const int Margin = 12;
        private const int ToastH = 52;

        public static void Init(Form owner) => _owner = owner;

        public static void Show(string message, ToastKind kind = ToastKind.Info, int durationMs = 3500)
        {
            if (_owner == null || _owner.IsDisposed) return;
            if (_owner.InvokeRequired) { _owner.Invoke(() => Show(message, kind, durationMs)); return; }

            var toast = new ToastPopup(message, kind);
            _active.Add(toast);
            Restack();
            toast.Show(_owner);
            var timer = new System.Windows.Forms.Timer { Interval = durationMs };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                timer.Dispose();
                if (!toast.IsDisposed) toast.Close();
                _active.Remove(toast);
                Restack();
            };
            timer.Start();
        }

        private static void Restack()
        {
            if (_owner == null) return;
            int y = _owner.Bottom - Margin;
            foreach (var t in _active)
            {
                y -= ToastH + 6;
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
            Width           = 340;
            Height          = 52;
            BackColor       = Theme.BgCard2;
            Opacity         = 0.96;

            var accent = kind switch
            {
                ToastKind.Success => Theme.Green,
                ToastKind.Warning => Theme.Amber,
                ToastKind.Error   => Theme.Red,
                _                 => Theme.Blue
            };

            var icon = kind switch
            {
                ToastKind.Success => "✓",
                ToastKind.Warning => "⚠",
                ToastKind.Error   => "✕",
                _                 => "ℹ"
            };

            Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                using var bar = new SolidBrush(accent);
                e.Graphics.FillRectangle(bar, 0, 0, 4, Height);
            };

            var iconLbl = new Label
            {
                Text = icon, ForeColor = accent,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Left = 14, Top = 14
            };
            var msgLbl = new Label
            {
                Text = message, ForeColor = Theme.TextPrimary,
                Font = Theme.FontBase,
                Left = 36, Top = 16, Width = 290, AutoSize = false
            };

            Controls.AddRange(new Control[] { iconLbl, msgLbl });
        }
    }
}
