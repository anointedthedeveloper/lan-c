// Run with: dotnet script GenerateIcons.cs  OR  csc GenerateIcons.cs && GenerateIcons.exe
// Actually: compile inline via dotnet-csi or just run as a standalone .NET app
// We'll use it as a simple console app - compile and run manually, or use the bat file.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

class GenerateIcons
{
    static void Main()
    {
        // Server: blue theme
        CreateIco(
            Path.Combine(AppContext.BaseDirectory, "server.ico"),
            Color.FromArgb(30, 100, 200),   // dark blue bg
            Color.FromArgb(100, 180, 255),  // light blue accent
            isServer: true
        );

        // Client: teal/green theme
        CreateIco(
            Path.Combine(AppContext.BaseDirectory, "client.ico"),
            Color.FromArgb(20, 140, 100),   // dark green bg
            Color.FromArgb(80, 220, 160),   // light green accent
            isServer: false
        );

        Console.WriteLine("Icons generated: server.ico, client.ico");
    }

    static void CreateIco(string path, Color bgColor, Color accentColor, bool isServer)
    {
        int[] sizes = { 256, 48, 32, 16 };
        var frames = new Bitmap[sizes.Length];

        for (int i = 0; i < sizes.Length; i++)
            frames[i] = DrawIcon(sizes[i], bgColor, accentColor, isServer);

        WriteIco(path, frames, sizes);

        foreach (var f in frames) f.Dispose();
    }

    static Bitmap DrawIcon(int size, Color bgColor, Color accentColor, bool isServer)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        float s = size;
        float pad = s * 0.08f;
        var rect = new RectangleF(pad, pad, s - pad * 2, s - pad * 2);

        // Rounded square background
        float r = s * 0.22f;
        using var bgBrush = new SolidBrush(bgColor);
        FillRoundedRect(g, bgBrush, rect, r);

        if (isServer)
            DrawServerSymbol(g, s, accentColor);
        else
            DrawClientSymbol(g, s, accentColor);

        return bmp;
    }

    // Server: stack of horizontal bars (server rack look)
    static void DrawServerSymbol(Graphics g, float s, Color accent)
    {
        using var brush = new SolidBrush(accent);
        using var pen = new Pen(Color.White, s * 0.03f);

        float barH = s * 0.14f;
        float barW = s * 0.62f;
        float x = (s - barW) / 2f;
        float gap = s * 0.05f;
        float totalH = barH * 3 + gap * 2;
        float startY = (s - totalH) / 2f;

        for (int i = 0; i < 3; i++)
        {
            float y = startY + i * (barH + gap);
            float rr = barH * 0.35f;
            var bar = new RectangleF(x, y, barW, barH);
            FillRoundedRect(g, brush, bar, rr);
            // small dot on right side
            float dotR = barH * 0.22f;
            float dotX = x + barW - dotR * 2.5f;
            float dotY = y + (barH - dotR * 2) / 2f;
            using var dotBrush = new SolidBrush(Color.FromArgb(180, Color.White));
            g.FillEllipse(dotBrush, dotX, dotY, dotR * 2, dotR * 2);
        }
    }

    // Client: monitor/screen symbol
    static void DrawClientSymbol(Graphics g, float s, Color accent)
    {
        using var brush = new SolidBrush(accent);
        using var whiteBrush = new SolidBrush(Color.FromArgb(200, Color.White));

        float mW = s * 0.62f;
        float mH = s * 0.42f;
        float mX = (s - mW) / 2f;
        float mY = s * 0.18f;
        float rr = s * 0.06f;

        // Monitor body
        FillRoundedRect(g, brush, new RectangleF(mX, mY, mW, mH), rr);

        // Screen inner (white/light)
        float spad = s * 0.05f;
        FillRoundedRect(g, whiteBrush, new RectangleF(mX + spad, mY + spad, mW - spad * 2, mH - spad * 2), rr * 0.5f);

        // Stand
        float stW = s * 0.12f;
        float stH = s * 0.14f;
        float stX = (s - stW) / 2f;
        float stY = mY + mH;
        using var standBrush = new SolidBrush(Color.FromArgb(180, accent));
        g.FillRectangle(standBrush, stX, stY, stW, stH);

        // Base
        float baseW = s * 0.36f;
        float baseH = s * 0.06f;
        float baseX = (s - baseW) / 2f;
        float baseY = stY + stH;
        FillRoundedRect(g, standBrush, new RectangleF(baseX, baseY, baseW, baseH), baseH * 0.4f);
    }

    static void FillRoundedRect(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }

    static void WriteIco(string path, Bitmap[] frames, int[] sizes)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        int count = frames.Length;
        // ICO header
        bw.Write((short)0);     // reserved
        bw.Write((short)1);     // type: icon
        bw.Write((short)count);

        // Collect PNG data for each frame
        var pngDatas = new byte[count][];
        for (int i = 0; i < count; i++)
        {
            using var ms = new MemoryStream();
            frames[i].Save(ms, ImageFormat.Png);
            pngDatas[i] = ms.ToArray();
        }

        // Directory entries (16 bytes each)
        int offset = 6 + count * 16;
        for (int i = 0; i < count; i++)
        {
            int sz = sizes[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz));  // width (0 = 256)
            bw.Write((byte)(sz >= 256 ? 0 : sz));  // height
            bw.Write((byte)0);   // color count
            bw.Write((byte)0);   // reserved
            bw.Write((short)1);  // planes
            bw.Write((short)32); // bit count
            bw.Write(pngDatas[i].Length);
            bw.Write(offset);
            offset += pngDatas[i].Length;
        }

        // Image data
        foreach (var data in pngDatas)
            bw.Write(data);
    }
}
