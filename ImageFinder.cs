using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;

public static class ImageFinder
{
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    
    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static Bitmap CaptureBackgroundWindow(IntPtr hWnd)
    {
        GetClientRect(hWnd, out RECT rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics gfxBmp = Graphics.FromImage(bmp))
        {
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            PrintWindow(hWnd, hdcBitmap, 3); // Chụp ngầm
            gfxBmp.ReleaseHdc(hdcBitmap);
        }
        return bmp;
    }

    // Trả về tọa độ X, Y nếu tìm thấy
    public static System.Drawing.Point? FindImageLocation(IntPtr hWnd, string templatePath, double threshold = 0.8)
    {
        if (!System.IO.File.Exists(templatePath)) return null;

        using (Bitmap screenBmp = CaptureBackgroundWindow(hWnd))
        using (Mat screenMat = BitmapConverter.ToMat(screenBmp))
        using (Mat templateMat = new Mat(templatePath))
        using (Mat result = new Mat())
        {
            Cv2.MatchTemplate(screenMat, templateMat, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            if (maxVal >= threshold)
            {
                return new System.Drawing.Point(
                    maxLoc.X + (templateMat.Width / 2), 
                    maxLoc.Y + (templateMat.Height / 2)
                );
            }
        }
        return null;
    }
}
