using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public static class Win32API
{
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_LBUTTONDOWN = 0x0201;
    public const uint WM_LBUTTONUP = 0x0202;

    // Tìm Handle cửa sổ dựa theo tên tiêu đề chứa số cửa sổ (ví dụ: Min4G(5))
    public static IntPtr FindWindowByTitleKeyword(string keyword)
    {
        IntPtr foundHandle = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();
            if (title.Contains(keyword))
            {
                foundHandle = hWnd;
                return false; // Dừng tìm kiếm khi thấy
            }
            return true;
        }, IntPtr.Zero);
        return foundHandle;
    }

    // Gửi phím ngầm (Không cần focus hay chiếm chuột phím)
    public static void SendKey(IntPtr hWnd, Keys key)
    {
        PostMessage(hWnd, WM_KEYDOWN, (IntPtr)(int)key, IntPtr.Zero);
        System.Threading.Thread.Sleep(50);
        PostMessage(hWnd, WM_KEYUP, (IntPtr)(int)key, IntPtr.Zero);
    }

    // Gửi click chuột ngầm vào tọa độ (x, y) của client game
    public static void SendClick(IntPtr hWnd, int x, int y)
    {
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)0x0001, lParam);
        System.Threading.Thread.Sleep(50);
        PostMessage(hWnd, WM_LBUTTONUP, (IntPtr)0, lParam);
    }
}
