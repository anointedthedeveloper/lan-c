namespace LanServer.Controls
{
    public class DarkListView : ListView
    {
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
            DrawItem        += (_, e) => e.DrawDefault = true;
            DrawSubItem     += (_, e) => e.DrawDefault = true;
        }

        private static void OnDrawHeader(object? s, DrawListViewColumnHeaderEventArgs e)
        {
            using var bg = new SolidBrush(Theme.BgCard2);
            e.Graphics.FillRectangle(bg, e.Bounds);
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            using var txt = new SolidBrush(Theme.TextSecond);
            e.Graphics.DrawString(e.Header?.Text ?? "",
                new Font("Segoe UI", 8f, FontStyle.Bold), txt, e.Bounds.X + 8, e.Bounds.Y + 6);
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
