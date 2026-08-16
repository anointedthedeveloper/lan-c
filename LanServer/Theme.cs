namespace LanServer
{
    public static class Theme
    {
        // ── Backgrounds ───────────────────────────────────────────────────────
        public static readonly Color BgApp      = Color.FromArgb(245, 247, 250);
        public static readonly Color BgSidebar  = Color.FromArgb(255, 255, 255);
        public static readonly Color BgCard     = Color.FromArgb(255, 255, 255);
        public static readonly Color BgCard2    = Color.FromArgb(248, 250, 253);
        public static readonly Color BgInput    = Color.FromArgb(255, 255, 255);
        public static readonly Color BgHover    = Color.FromArgb(239, 244, 255);
        public static readonly Color BgActive   = Color.FromArgb(37, 99, 235, 18);

        // ── Borders ───────────────────────────────────────────────────────────
        public static readonly Color Border     = Color.FromArgb(226, 232, 240);
        public static readonly Color BorderFocus= Color.FromArgb(37, 99, 235);

        // ── Accents ───────────────────────────────────────────────────────────
        public static readonly Color Blue       = Color.FromArgb(37,  99, 235);
        public static readonly Color BlueHover  = Color.FromArgb(29,  78, 216);
        public static readonly Color BlueSoft   = Color.FromArgb(239, 246, 255);
        public static readonly Color Green      = Color.FromArgb(5,  150, 105);
        public static readonly Color GreenSoft  = Color.FromArgb(236, 253, 245);
        public static readonly Color Purple     = Color.FromArgb(99, 102, 241);
        public static readonly Color PurpleSoft = Color.FromArgb(238, 242, 255);
        public static readonly Color Amber      = Color.FromArgb(217, 119,  6);
        public static readonly Color AmberSoft  = Color.FromArgb(255, 251, 235);
        public static readonly Color Red        = Color.FromArgb(220,  38,  38);
        public static readonly Color RedMuted   = Color.FromArgb(185,  28,  28);
        public static readonly Color RedSoft    = Color.FromArgb(254, 242, 242);

        // ── Text ──────────────────────────────────────────────────────────────
        public static readonly Color TextPrimary = Color.FromArgb(15,  23,  42);
        public static readonly Color TextSecond  = Color.FromArgb(71,  85, 105);
        public static readonly Color TextMuted   = Color.FromArgb(148, 163, 184);

        // ── Fonts ─────────────────────────────────────────────────────────────
        public static readonly Font FontBase    = new("Segoe UI", 9f);
        public static readonly Font FontSm      = new("Segoe UI", 8f);
        public static readonly Font FontMd      = new("Segoe UI", 10f);
        public static readonly Font FontLg      = new("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font FontXl      = new("Segoe UI", 22f, FontStyle.Bold);
        public static readonly Font FontBold    = new("Segoe UI", 9f,  FontStyle.Bold);
        public static readonly Font FontMono    = new("Consolas",  8.5f);
        public static readonly Font FontMonoSm  = new("Consolas",  8f);

        // ── Helpers ───────────────────────────────────────────────────────────
        public static Button MakeBtn(string text, Color back, int h = 34)
        {
            var b = new Button
            {
                Text = text,
                BackColor = back,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = FontBold,
                Height = h,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            b.MouseEnter += (_, _) => AnimateButtonHover(b, back, true);
            b.MouseLeave += (_, _) => AnimateButtonHover(b, back, false);
            return b;
        }

        public static Button MakeOutlineBtn(string text, Color accent, int h = 34)
        {
            var b = new Button
            {
                Text = text,
                BackColor = BgCard,
                ForeColor = accent,
                FlatStyle = FlatStyle.Flat,
                Font = FontBold,
                Height = h,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 1, BorderColor = accent }
            };
            b.MouseEnter += (_, _) => { b.BackColor = BgHover; };
            b.MouseLeave += (_, _) => { b.BackColor = BgCard; };
            return b;
        }

        public static TextBox MakeInput(string placeholder = "")
        {
            var t = new TextBox
            {
                BackColor = BgInput,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = FontBase,
                Height = 32
            };
            return t;
        }

        public static ComboBox MakeCombo()
        {
            var c = new ComboBox
            {
                BackColor = BgInput,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = FontBase,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            return c;
        }

        public static Label MakeLabel(string text, Color? color = null, Font? font = null) => new()
        {
            Text = text,
            ForeColor = color ?? TextSecond,
            Font = font ?? FontBase,
            AutoSize = true,
            BackColor = Color.Transparent
        };

        public static Panel MakeSeparator() => new()
        {
            Height = 1,
            Dock = DockStyle.Top,
            BackColor = Border
        };

        // ── Shadow simulation via Paint ────────────────────────────────────────
        public static void PaintCardShadow(PaintEventArgs e, int width, int height)
        {
            var r = new Rectangle(0, 0, width - 1, height - 1);
            using var pen = new Pen(Color.FromArgb(18, 0, 0, 0), 1);
            e.Graphics.DrawRectangle(pen, r);
        }

        // ── Smooth hover tint ──────────────────────────────────────────────────
        private static void AnimateButtonHover(Button b, Color baseColor, bool enter)
        {
            if (enter)
            {
                int r = Math.Max(0, baseColor.R - 20);
                int g = Math.Max(0, baseColor.G - 20);
                int bv = Math.Max(0, baseColor.B - 20);
                b.BackColor = Color.FromArgb(baseColor.A, r, g, bv);
            }
            else
            {
                b.BackColor = baseColor;
            }
        }
    }
}
