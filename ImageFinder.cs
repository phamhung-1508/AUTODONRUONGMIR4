using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Point = OpenCvSharp.Point;

public static class ImageFinder
{
    // Chụp trực tiếp tọa độ cửa sổ game (Không bị màn hình đen như PrintWindow)
    public static Bitmap CaptureWindowArea(IntPtr hWnd)
    {
        Win32API.GetWindowRect(hWnd, out Win32API.RECT rect);
        int width = Math.Max(1, rect.Right - rect.Left);
        int height = Math.Max(1, rect.Bottom - rect.Top);

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            // Chụp đúng khung chữ nhật của cửa sổ game Mir4G
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
        }

        // Tự động lưu ảnh để kiểm tra bot có chụp rõ nét hay không
        try { bmp.Save("debug_screen.png", ImageFormat.Png); } catch { }

        return bmp;
    }

    public static System.Drawing.Point? FindImageLocation(IntPtr hWnd, string templatePath, double threshold = 0.65)
    {
        if (!System.IO.File.Exists(templatePath)) return null;

        using (Bitmap screenBmp = CaptureWindowArea(hWnd))
        using (Mat screenMat = BitmapConverter.ToMat(screenBmp))
        using (Mat templateMat = new Mat(templatePath))
        using (Mat grayScreen = new Mat())
        using (Mat grayTemplate = new Mat())
        using (Mat result = new Mat())
        {
            Cv2.CvtColor(screenMat, grayScreen, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(templateMat, grayTemplate, ColorConversionCodes.BGR2GRAY);

            Cv2.MatchTemplate(grayScreen, grayTemplate, result, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

            // Ngưỡng 0.65 để bắt dính kể cả khi chữ hơi răng cưa
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
