using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanServer.Controls
{
    public class PageHeader : Panel
    {
        public PageHeader(string title, string subtitle = "")
        {
            Dock = DockStyle.Top;
            Height = 68;
            BackColor = Theme.BgCard;

            Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            };

            // Left: title + subtitle
            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontLg,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 24, Top = 12,
                BackColor = Color.Transparent
            };
            var subLbl = new Label
            {
                Text = subtitle,
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 24, Top = 40,
                BackColor = Color.Transparent
            };

            // Right container — flows right-to-left using anchoring on SizeChanged
            var ip = GetLocalIp();
            var url = $"http://{ip}:{Config.Current.HttpPort}";

            // Status pill
            var statusPill = new Panel
            {
                Height = 26, Width = 110,
                BackColor = Theme.GreenSoft,
                Top = 21
            };
            statusPill.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(80, Theme.Green.R, Theme.Green.G, Theme.Green.B), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, statusPill.Width - 1, statusPill.Height - 1);
            };
            var statusDot = new Label
            {
                Text = "●",
                Font = new Font("Segoe UI", 7f),
                ForeColor = Theme.Green,
                AutoSize = true,
                Left = 8, Top = 6,
                BackColor = Color.Transparent
            };
            var statusLbl = new Label
            {
                Text = "Connected",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Theme.Green,
                AutoSize = true,
                Left = 22, Top = 5,
                BackColor = Color.Transparent
            };
            statusPill.Controls.AddRange(new Control[] { statusDot, statusLbl });

            // Copy button
            var copyBtn = new Button
            {
                Text = "⎘",
                Font = new Font("Segoe UI", 9f),
                BackColor = Theme.BgCard,
                ForeColor = Theme.TextSecond,
                FlatStyle = FlatStyle.Flat,
                Width = 30, Height = 26,
                Top = 21,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Theme.Border }
            };
            copyBtn.MouseEnter += (_, _) => { copyBtn.BackColor = Theme.BgHover; copyBtn.ForeColor = Theme.Blue; };
            copyBtn.MouseLeave += (_, _) => { copyBtn.BackColor = Theme.BgCard; copyBtn.ForeColor = Theme.TextSecond; };
            copyBtn.Click += (_, _) =>
            {
                Clipboard.SetText(url);
                copyBtn.Text = "✓";
                copyBtn.ForeColor = Theme.Green;
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (_, _) => { t.Stop(); t.Dispose(); copyBtn.Text = "⎘"; copyBtn.ForeColor = Theme.TextSecond; };
                t.Start();
            };

            // URL label (not a TextBox — avoids focus/border issues)
            var urlLbl = new Label
            {
                Text = url,
                Font = Theme.FontSm,
                ForeColor = Theme.Blue,
                AutoSize = true,
                Top = 26,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            urlLbl.Click += (_, _) => { Clipboard.SetText(url); };

            // Port info label
            var infoLbl = new Label
            {
                Text = $"WS:{Config.Current.WebSocketPort}  HTTP:{Config.Current.HttpPort}  UDP:{Config.Current.UdpPort}",
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Top = 44,
                BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { titleLbl, subLbl, statusPill, copyBtn, urlLbl, infoLbl });

            // Position right-aligned on every resize
            SizeChanged += (_, _) => LayoutRight();
            LayoutRight();

            void LayoutRight()
            {
                int right = Width - 20;
                statusPill.Left = right - statusPill.Width;
                copyBtn.Left    = statusPill.Left - copyBtn.Width - 8;
                urlLbl.Left     = copyBtn.Left - urlLbl.Width - 8;
                infoLbl.Left    = right - infoLbl.Width;
            }
        }

        private static string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                        return addr.Address.ToString();
            }
            return "127.0.0.1";
        }
    }
}
