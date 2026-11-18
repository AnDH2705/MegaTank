using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public sealed class KitCellInspection
{
    public int index, x, y, width, height;
    public double transparentFraction;
    public bool touchesCellBoundary;
}
public sealed class KitPngInspection
{
    public string file;
    public int width, height;
    public List<KitCellInspection> cells = new List<KitCellInspection>();
}
public static class KitPngInspector
{
    // Read-only inspection: this tool never changes or saves a raster image.
    public static KitPngInspection Inspect(string file, int columns, int rows)
    {
        using (var bitmap = new Bitmap(file))
        {
            var report = new KitPngInspection { file = file, width = bitmap.Width, height = bitmap.Height };
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var bytes = new byte[Math.Abs(data.Stride) * bitmap.Height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                for (int index = 0; index < columns * rows; index++)
                {
                    int left = index % columns * bitmap.Width / columns;
                    int right = (index % columns + 1) * bitmap.Width / columns;
                    int top = index / columns * bitmap.Height / rows;
                    int bottom = (index / columns + 1) * bitmap.Height / rows;
                    int xmin = right, ymin = bottom, xmax = -1, ymax = -1, transparent = 0;
                    for (int y = top; y < bottom; y++)
                        for (int x = left; x < right; x++)
                        {
                            int row = data.Stride > 0 ? y : bitmap.Height - 1 - y;
                            int alpha = bytes[row * Math.Abs(data.Stride) + x * 4 + 3];
                            if (alpha <= 8) { transparent++; continue; }
                            xmin = Math.Min(xmin, x); ymin = Math.Min(ymin, y);
                            xmax = Math.Max(xmax, x); ymax = Math.Max(ymax, y);
                        }
                    bool blank = xmax < xmin || ymax < ymin;
                    report.cells.Add(new KitCellInspection
                    {
                        index = index,
                        x = blank ? 0 : xmin,
                        y = blank ? 0 : ymin,
                        width = blank ? 0 : xmax - xmin + 1,
                        height = blank ? 0 : ymax - ymin + 1,
                        transparentFraction = (double)transparent / ((right - left) * (bottom - top)),
                        touchesCellBoundary = !blank && (xmin <= left || ymin <= top || xmax >= right - 1 || ymax >= bottom - 1)
                    });
                }
                return report;
            }
            finally { bitmap.UnlockBits(data); }
        }
    }
}
