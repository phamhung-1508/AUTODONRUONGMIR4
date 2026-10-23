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

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const uint WM_KEYDOWN = 0x0100;
    public const uint WM_KEYUP = 0x0101;
    public const uint WM_SYSKEYDOWN = 0x0104; // Dành riêng cho phím F10
    public const uint WM_SYSKEYUP = 0x0105;
    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint WM_LBUTTONDOWN = 0x0201;
    public const uint WM_LBUTTONUP = 0x0202;
    public const uint WM_MOUSEWHEEL = 0x020A;

    public static IntPtr FindWindowByTitleKeyword(string keyword)
    {
        IntPtr foundHandle = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().Contains(keyword))
            {
                foundHandle = hWnd;
                return false; 
            }
            return true;
        }, IntPtr.Zero);
        return foundHandle;
    }

    // Gửi phím thông thường
    public static void SendKey(IntPtr hWnd, Keys key)
    {
        PostMessage(hWnd, WM_KEYDOWN, (IntPtr)(int)key, IntPtr.Zero);
        System.Threading.Thread.Sleep(60);
        PostMessage(hWnd, WM_KEYUP, (IntPtr)(int)key, IntPtr.Zero);
    }

    // Gửi phím F10 (Bắt buộc dùng SYSKEY)
    public static void SendF10Key(IntPtr hWnd)
    {
        PostMessage(hWnd, WM_SYSKEYDOWN, (IntPtr)(int)Keys.F10, (IntPtr)0x20000001);
        System.Threading.Thread.Sleep(80);
        PostMessage(hWnd, WM_SYSKEYUP, (IntPtr)(int)Keys.F10, (IntPtr)0x20000001);
    }

    public static void SendClick(IntPtr hWnd, int x, int y)
    {
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        PostMessage(hWnd, WM_LBUTTONDOWN, (IntPtr)0x0001, lParam);
        System.Threading.Thread.Sleep(60);
        PostMessage(hWnd, WM_LBUTTONUP, (IntPtr)0, lParam);
    }

    public static void SendMouseMove(IntPtr hWnd, int x, int y)
    {
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        PostMessage(hWnd, WM_MOUSEMOVE, IntPtr.Zero, lParam);
    }

    public static void SendScrollDown(IntPtr hWnd, int x, int y)
    {
        int wheelDelta = -120;
        IntPtr wParam = (IntPtr)((wheelDelta << 16) | 0);
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        PostMessage(hWnd, WM_MOUSEWHEEL, wParam, lParam);
    }
}
