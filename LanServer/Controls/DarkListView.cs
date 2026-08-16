namespace LanServer.Controls
{
    public class DarkListView : ListView
    {
        private int _hoveredIndex = -1;

        public DarkListView()
        {
            View = View.Details;
            FullRowSelect = true;
            GridLines = false;
            BackColor = Theme.BgCard;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontBase;
            MultiSelect = false;
            BorderStyle = BorderStyle.None;
            HeaderStyle = ColumnHeaderStyle.Nonclickable;
            OwnerDraw = true;

            DrawColumnHeader += OnDrawHeader;
            DrawItem        += OnDrawItem;
            DrawSubItem     += OnDrawSubItem;

            MouseMove += (_, e) =>
            {
                var item = GetItemAt(e.X, e.Y);
                int idx = item?.Index ?? -1;
                if (idx != _hoveredIndex)
                {
                    _hoveredIndex = idx;
                    Invalidate();
                }
            };
            MouseLeave += (_, _) =>
            {
                if (_hoveredIndex != -1) { _hoveredIndex = -1; Invalidate(); }
            };
        }

        private static void OnDrawHeader(object? s, DrawListViewColumnHeaderEventArgs e)
        {
            using var bg = new SolidBrush(Theme.BgCard2);
            e.Graphics.FillRectangle(bg, e.Bounds);
            using var borderPen = new Pen(Theme.Border);
            e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            // Bottom separator line for each column
            if (e.ColumnIndex > 0)
                e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Top + 6, e.Bounds.Left, e.Bounds.Bottom - 6);

            using var txt = new SolidBrush(Theme.TextSecond);
            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            var textRect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 7, e.Bounds.Width - 14, e.Bounds.Height);
            e.Graphics.DrawString(e.Header?.Text ?? "", font, txt, textRect);
        }

        private void OnDrawItem(object? s, DrawListViewItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            bool hovered  = e.ItemIndex == _hoveredIndex;

            Color bg = selected ? Theme.BlueSoft
                     : hovered  ? Theme.BgHover
                     : (e.ItemIndex % 2 == 0 ? Theme.BgCard : Theme.BgCard2);

            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, e.Bounds);

            if (selected)
            {
                using var borderPen = new Pen(Color.FromArgb(120, Theme.Blue.R, Theme.Blue.G, Theme.Blue.B), 1);
                e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            }

            // Row separator
            using var sepPen = new Pen(Theme.Border, 1);
            e.Graphics.DrawLine(sepPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        private static void OnDrawSubItem(object? s, DrawListViewSubItemEventArgs e)
        {
            if (e.Item == null || e.SubItem == null) return;
            e.DrawDefault = false;

            var fg = e.SubItem.ForeColor == Color.Empty || e.SubItem.ForeColor == SystemColors.WindowText
                ? Theme.TextPrimary
                : e.SubItem.ForeColor;

            using var txt = new SolidBrush(fg);
            var r = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 2, e.Bounds.Width - 14, e.Bounds.Height - 4);
            e.Graphics.DrawString(e.SubItem.Text, Theme.FontBase, txt, r,
                new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter });
        }

        public ListViewItem AddRow(Color rowBg, Color rowFg, params string[] cells)
        {
            var item = new ListViewItem(cells[0]) { BackColor = rowBg, ForeColor = rowFg };
            for (int i = 1; i < cells.Length; i++) item.SubItems.Add(cells[i]);
            Items.Add(item);
            return item;
        }
    }
}
