using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LanServer.Controls
{
    public class PageHeader : Panel
    {
        private readonly Label _statusLabel;
        private readonly TextBox _urlBox;

        public PageHeader(string title, string subtitle = "")
        {
            Dock = DockStyle.Top;
            Height = 72;
            BackColor = Theme.BgCard;
            Padding = new Padding(24, 0, 24, 0);

            Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border, 1);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            };

            var titleLbl = new Label
            {
                Text = title,
                Font = Theme.FontLg,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Left = 24, Top = 14
            };

            var subLbl = new Label
            {
                Text = subtitle,
                Font = Theme.FontSm,
                ForeColor = Theme.TextSecond,
                AutoSize = true,
                Left = 24, Top = 40
            };

            // Right side: copyable URL box + copy button + status
            var ip = GetLocalIp();
            var url = $"http://{ip}:{Config.Current.HttpPort}";

            _urlBox = new TextBox
            {
                Text = url,
                Font = Theme.FontSm,
                BackColor = Theme.BgInput,
                ForeColor = Theme.TextSecond,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Width = 220,
                Height = 22,
                Top = 14,
                Cursor = Cursors.IBeam
            };
            _urlBox.Click += (_, _) => { _urlBox.SelectAll(); };

            var copyBtn = new Button
            {
                Text = "⎘",
                Font = new Font("Segoe UI", 9f),
                BackColor = Theme.BgCard2,
                ForeColor = Theme.TextSecond,
                FlatStyle = FlatStyle.Flat,
                Width = 28, Height = 22,
                Top = 14,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = Theme.Border }
            };
            copyBtn.Click += (_, _) =>
            {
                Clipboard.SetText(_urlBox.Text);
                copyBtn.Text = "✓";
                copyBtn.ForeColor = Theme.Green;
                var t = new System.Windows.Forms.Timer { Interval = 1500 };
                t.Tick += (_, _) => { t.Stop(); t.Dispose(); copyBtn.Text = "⎘"; copyBtn.ForeColor = Theme.TextSecond; };
                t.Start();
            };

            var infoLbl = new Label
            {
                Text = $"{ip}   WS:{Config.Current.WebSocketPort}   HTTP:{Config.Current.HttpPort}",
                Font = Theme.FontSm,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Top = 40
            };

            _statusLabel = new Label
            {
                Text = "● Connected",
                Font = Theme.FontSm,
                ForeColor = Theme.Green,
                AutoSize = true,
                Top = 40
            };

            SizeChanged += (_, _) => PositionRight(infoLbl, copyBtn);

            Controls.AddRange(new Control[] { titleLbl, subLbl, _urlBox, copyBtn, infoLbl, _statusLabel });
            PositionRight(infoLbl, copyBtn);
        }

        private void PositionRight(Label infoLbl, Button copyBtn)
        {
            // status dot far right
            _statusLabel.Left = Width - _statusLabel.Width - 24;
            // info label left of status
            infoLbl.Left = Width - infoLbl.Width - 24;
            // copy button
            copyBtn.Left = Width - copyBtn.Width - 24;
            // url box left of copy button
            _urlBox.Left = copyBtn.Left - _urlBox.Width - 4;
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
