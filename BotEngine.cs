using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

public class BotEngine
{
    private IntPtr _targetHWnd = IntPtr.Zero;
    private CancellationTokenSource? _cts;
    private enum State { Stopped, Running, Paused }
    private State _currentState = State.Stopped;
    private readonly object _lockObj = new object();

    public void SetTarget(IntPtr hWnd) { _targetHWnd = hWnd; }

    public void Start() { 
        if (_targetHWnd == IntPtr.Zero) return;
        lock (_lockObj) { if (_currentState == State.Running) return; _currentState = State.Running; _cts = new CancellationTokenSource(); }
        Task.Run(() => RunBotLoop(_cts.Token));
    }
    public void Stop() { lock (_lockObj) { _cts?.Cancel(); _currentState = State.Stopped; } }
    public void Pause() { lock (_lockObj) { if (_currentState == State.Running) _currentState = State.Paused; } }
    public void Resume() { lock (_lockObj) { if (_currentState == State.Paused) _currentState = State.Running; } }

    private void CheckPauseResume(CancellationToken token) { 
        while (true) { token.ThrowIfCancellationRequested(); bool isPaused; lock (_lockObj) { isPaused = (_currentState == State.Paused); } if (!isPaused) break; Thread.Sleep(200); }
    }

    // Hàm đợi ảnh xuất hiện trong tối đa X giây và tự động Click. Trả về True nếu thành công, False nếu không thấy.
    private async Task<bool> WaitAndClickImage(string imageName, CancellationToken token, int timeoutMs = 3000)
    {
        string path = "Images\\" + imageName;
        long start = Environment.TickCount;
        while (Environment.TickCount - start < timeoutMs)
        {
            CheckPauseResume(token);
            Point? loc = ImageFinder.FindImageLocation(_targetHWnd, path, 0.8);
            if (loc.HasValue)
            {
                Win32API.SendClick(_targetHWnd, loc.Value.X, loc.Value.Y);
                await Task.Delay(500, token); // Đợi UI phản hồi sau khi click
                return true;
            }
            await Task.Delay(200, token); // Quét lại mỗi 0.2s
        }
        return false; // Hết thời gian mà không thấy ảnh
    }

    // Hàm chỉ tìm ảnh (không click) dùng để lấy tọa độ
    private async Task<Point?> WaitForImage(string imageName, CancellationToken token, int timeoutMs = 3000)
    {
        string path = "Images\\" + imageName;
        long start = Environment.TickCount;
        while (Environment.TickCount - start < timeoutMs)
        {
            CheckPauseResume(token);
            Point? loc = ImageFinder.FindImageLocation(_targetHWnd, path, 0.8);
            if (loc.HasValue) return loc;
            await Task.Delay(200, token);
        }
        return null;
    }

    private async Task RunBotLoop(CancellationToken token)
    {
        try
        {
            // Vòng lặp vô tận: Nếu bất kỳ bước nào = false (fail), lệnh 'continue' sẽ đưa bot quay lại đầu vòng lặp.
            while (true)
            {
                CheckPauseResume(token);

                // 1. Ấn ESC 2 lần
                Win32API.SendKey(_targetHWnd, Keys.Escape);
                await Task.Delay(500, token);
                Win32API.SendKey(_targetHWnd, Keys.Escape);
                await Task.Delay(1000, token); // Đợi 1 giây như yêu cầu

                // 2. Ấn F10 mở bản đồ
                Win32API.SendKey(_targetHWnd, Keys.F10);
                await Task.Delay(1000, token);

                // 3. Tìm và click Thuyền Nhỏ
                if (!await WaitAndClickImage("thuyen_nho_icon.png", token)) continue;

                // 4. Tìm và click Thuyền Lớn
                if (!await WaitAndClickImage("thuyen_lon_icon.png", token)) continue;

                // 5. Tìm Map Thế Giới và click Bicheon Icon
                // (Bot sẽ đợi đến khi load xong map thế giới và bicheon_icon hiện ra)
                if (!await WaitAndClickImage("bicheon_icon.png", token)) continue;

                // 6. Tìm và click Thành Bicheon
                if (!await WaitAndClickImage("thanh_bicheon_icon.png", token)) continue;

                // 7. Chỏ chuột vào NPC Bicheon (Tìm tọa độ khu vực NPC)
                Point? npcLoc = await WaitForImage("npc_bicheon_icon.png", token);
                if (npcLoc == null) continue;
                
                // Di chuyển chuột ảo vào khu vực NPC để chuẩn bị cuộn
                Win32API.SendMouseMove(_targetHWnd, npcLoc.Value.X, npcLoc.Value.Y);
                await Task.Delay(200, token);

                // 8. Lăn chuột liên tục trong 2 giây để tìm kho đồ
                bool foundKho = false;
                long startScroll = Environment.TickCount;
                while (Environment.TickCount - startScroll < 2000) // Vòng lặp 2 giây
                {
                    CheckPauseResume(token);
                    
                    // Nếu thấy chữ kho đồ xuất hiện thì ngưng lăn
                    Point? khoLoc = ImageFinder.FindImageLocation(_targetHWnd, "Images\\kho_do_bicheon_icon.png", 0.8);
                    if (khoLoc.HasValue)
                    {
                        foundKho = true;
                        break;
                    }
                    
                    // Nếu chưa thấy thì gửi lệnh lăn chuột xuống
                    Win32API.SendScrollDown(_targetHWnd, npcLoc.Value.X, npcLoc.Value.Y);
                    await Task.Delay(150, token); 
                }

                if (!foundKho) continue; // Nếu cuộn 2s vẫn không thấy -> quay lại từ đầu (bấm ESC)

                // 9. Bấm vào nút Tele kho
                if (!await WaitAndClickImage("tele_kho.png", token)) continue;

                // --- HOÀN THÀNH 1 CHU KỲ THÀNH CÔNG ---
                // Bot có thể đợi 15-20s cho nhân vật chạy tới kho rồi mới tiếp tục vòng lặp (hoặc dừng tùy bạn)
                await Task.Delay(15000, token); 
            }
        }
        catch (OperationCanceledException) { /* Xử lý khi nhấn nút Dừng */ }
        finally { lock (_lockObj) { _currentState = State.Stopped; } }
    }
}
