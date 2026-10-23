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
        int width = Math.Max(1, rect.Right - rect.Left);
        int height = Math.Max(1, rect.Bottom - rect.Top);

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics gfxBmp = Graphics.FromImage(bmp))
        {
            IntPtr hdcBitmap = gfxBmp.GetHdc();
            PrintWindow(hWnd, hdcBitmap, 3); // Chụp ngầm cửa sổ
            gfxBmp.ReleaseHdc(hdcBitmap);
        }
        return bmp;
    }

    /// <summary>
    /// Tìm vị trí ảnh mẫu trong cửa sổ game.
    /// Ngưỡng mặc định hạ xuống 0.68 để dễ nhận biết hơn khi ảnh hơi mờ/vỡ hạt.
    /// </summary>
    public static System.Drawing.Point? FindImageLocation(IntPtr hWnd, string templatePath, double threshold = 0.68)
    {
        if (!System.IO.File.Exists(templatePath)) return null;

        using (Bitmap screenBmp = CaptureBackgroundWindow(hWnd))
        using (Mat screenMat = BitmapConverter.ToMat(screenBmp))
        using (Mat templateMat = new Mat(templatePath))
        using (Mat grayScreen = new Mat())
        using (Mat grayTemplate = new Mat())
        using (Mat result = new Mat())
        {
            // Chuyển sang ảnh thang độ xám (Grayscale) để loại bỏ ảnh hưởng của màu sắc / hiệu ứng đổ bóng
            Cv2.CvtColor(screenMat, grayScreen, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(templateMat, grayTemplate, ColorConversionCodes.BGR2GRAY);

            // So khớp ảnh
            Cv2.MatchTemplate(grayScreen, grayTemplate, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            // Kiểm tra ngưỡng nhận diện
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
