namespace LanServer
{
    public static class Theme
    {
        // ── Backgrounds ───────────────────────────────────────────────────────
        public static readonly Color BgApp      = Color.FromArgb(8,  13, 24);
        public static readonly Color BgSidebar  = Color.FromArgb(11, 18, 32);
        public static readonly Color BgCard     = Color.FromArgb(17, 26, 43);
        public static readonly Color BgCard2    = Color.FromArgb(22, 34, 56);
        public static readonly Color BgInput    = Color.FromArgb(15, 23, 40);
        public static readonly Color BgHover    = Color.FromArgb(25, 38, 62);
        public static readonly Color BgActive   = Color.FromArgb(37, 99, 235, 30);

        // ── Borders ───────────────────────────────────────────────────────────
        public static readonly Color Border     = Color.FromArgb(36, 51, 77);
        public static readonly Color BorderFocus= Color.FromArgb(37, 99, 235);

        // ── Accents ───────────────────────────────────────────────────────────
        public static readonly Color Blue       = Color.FromArgb(37,  99, 235);
        public static readonly Color BlueHover  = Color.FromArgb(59, 130, 246);
        public static readonly Color Green      = Color.FromArgb(16, 185, 129);
        public static readonly Color Purple     = Color.FromArgb(99, 102, 241);
        public static readonly Color Amber      = Color.FromArgb(245, 158, 11);
        public static readonly Color Red        = Color.FromArgb(225, 29,  72);
        public static readonly Color RedMuted   = Color.FromArgb(159, 18,  57);

        // ── Text ──────────────────────────────────────────────────────────────
        public static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);
        public static readonly Color TextSecond  = Color.FromArgb(148, 163, 184);
        public static readonly Color TextMuted   = Color.FromArgb(71,  85, 105);

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
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = FontBold,
                Height = h,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            b.MouseEnter += (_, _) => b.BackColor = ControlPaint.Light(back, 0.12f);
            b.MouseLeave += (_, _) => b.BackColor = back;
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
            b.MouseEnter += (_, _) => { b.BackColor = BgCard2; };
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
    }
}
