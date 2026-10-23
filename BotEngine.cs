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

    public void Start() 
    { 
        if (_targetHWnd == IntPtr.Zero) return;
        lock (_lockObj) 
        { 
            if (_currentState == State.Running) return; 
            _currentState = State.Running; 
            _cts = new CancellationTokenSource(); 
        }
        Task.Run(() => RunBotLoop(_cts.Token));
    }

    public void Stop() { lock (_lockObj) { _cts?.Cancel(); _currentState = State.Stopped; } }
    public void Pause() { lock (_lockObj) { if (_currentState == State.Running) _currentState = State.Paused; } }
    public void Resume() { lock (_lockObj) { if (_currentState == State.Paused) _currentState = State.Running; } }

    private void CheckPauseResume(CancellationToken token) 
    { 
        while (true) 
        { 
            token.ThrowIfCancellationRequested(); 
            bool isPaused; 
            lock (_lockObj) { isPaused = (_currentState == State.Paused); } 
            if (!isPaused) break; 
            Thread.Sleep(200); 
        }
    }

    // Chờ một ảnh xuất hiện trong tối đa timeoutMs
    private async Task<Point?> WaitForImageAsync(string imageName, CancellationToken token, int timeoutMs = 3500)
    {
        string path = "Images\\" + imageName;
        long start = Environment.TickCount;
        while (Environment.TickCount - start < timeoutMs)
        {
            CheckPauseResume(token);
            Point? loc = ImageFinder.FindImageLocation(_targetHWnd, path, 0.68);
            if (loc.HasValue) return loc;
            await Task.Delay(250, token);
        }
        return null;
    }

    // Thực hiện thao tác của một bước cụ thể
    private async Task ExecuteStepAction(int step, Point? cachedLoc, CancellationToken token)
    {
        switch (step)
        {
            case 1:
                // Mở bản đồ bằng F10
                Win32API.SendKey(_targetHWnd, Keys.F10);
                break;
            case 2:
            case 3:
            case 4:
            case 5:
                // Click vào tọa độ icon đã tìm thấy ở bước đó
                if (cachedLoc.HasValue)
                {
                    Win32API.SendClick(_targetHWnd, cachedLoc.Value.X, cachedLoc.Value.Y);
                }
                break;
        }
        // Chờ 2 - 3 giây sau mỗi lần tương tác để game kịp load UI
        await Task.Delay(2500, token);
    }

    private async Task RunBotLoop(CancellationToken token)
    {
        try
        {
            while (true)
            {
                CheckPauseResume(token);

                // --- BƯỚC 0: RESET GIAO DIỆN BẰNG ESC 2 LẦN ---
                Win32API.SendKey(_targetHWnd, Keys.Escape);
                await Task.Delay(500, token);
                Win32API.SendKey(_targetHWnd, Keys.Escape);
                await Task.Delay(1000, token); // Chờ 1 giây ổn định màn hình

                int currentStep = 1;
                int retryCount = 0;
                bool needFullReset = false;

                Point? lastStepClickLoc = null;

                while (currentStep <= 6 && !needFullReset)
                {
                    CheckPauseResume(token);

                    if (currentStep == 1)
                    {
                        // Bước 1: Mở F10 và kiểm tra xem có thấy thuyen_nho_icon không
                        await ExecuteStepAction(1, null, token);
                        Point? nextImg = await WaitForImageAsync("thuyen_nho_icon.png", token);
                        
                        if (nextImg.HasValue)
                        {
                            lastStepClickLoc = nextImg.Value; // Lưu lại để bước 2 click
                            currentStep = 2;
                            retryCount = 0;
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= 3)
                            {
                                needFullReset = true; // Thử lại quá 3 lần -> quay lại ESC 2 lần
                            }
                        }
                    }
                    else if (currentStep == 2)
                    {
                        // Bước 2: Click thuyen_nho_icon -> Chờ thuyen_lon_icon
                        await ExecuteStepAction(2, lastStepClickLoc, token);
                        Point? nextImg = await WaitForImageAsync("thuyen_lon_icon.png", token);

                        if (nextImg.HasValue)
                        {
                            lastStepClickLoc = nextImg.Value;
                            currentStep = 3;
                            retryCount = 0;
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= 3) needFullReset = true;
                            // Nếu chưa quá 3 lần, vòng lặp chạy lại sẽ click lại thuyen_nho_icon
                        }
                    }
                    else if (currentStep == 3)
                    {
                        // Bước 3: Click thuyen_lon_icon -> Chờ bản đồ thế giới & bicheon_icon
                        await ExecuteStepAction(3, lastStepClickLoc, token);
                        Point? nextImg = await WaitForImageAsync("bicheon_icon.png", token, 4500);

                        if (nextImg.HasValue)
                        {
                            lastStepClickLoc = nextImg.Value;
                            currentStep = 4;
                            retryCount = 0;
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= 3) needFullReset = true;
                        }
                    }
                    else if (currentStep == 4)
                    {
                        // Bước 4: Click bicheon_icon -> Chờ thanh_bicheon_icon
                        await ExecuteStepAction(4, lastStepClickLoc, token);
                        Point? nextImg = await WaitForImageAsync("thanh_bicheon_icon.png", token, 4000);

                        if (nextImg.HasValue)
                        {
                            lastStepClickLoc = nextImg.Value;
                            currentStep = 5;
                            retryCount = 0;
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= 3) needFullReset = true;
                        }
                    }
                    else if (currentStep == 5)
                    {
                        // Bước 5: Click thanh_bicheon_icon -> Chờ tab danh sách NPC (npc_bicheon_icon)
                        await ExecuteStepAction(5, lastStepClickLoc, token);
                        Point? npcLoc = await WaitForImageAsync("npc_bicheon_icon.png", token, 4000);

                        if (npcLoc.HasValue)
                        {
                            // Đã hiện bảng NPC: Rê chuột ảo vào danh sách NPC
                            Win32API.SendMouseMove(_targetHWnd, npcLoc.Value.X, npcLoc.Value.Y);
                            await Task.Delay(300, token);

                            // Lăn chuột tìm kho đồ trong 2.5 giây
                            bool foundKho = false;
                            long startScroll = Environment.TickCount;
                            while (Environment.TickCount - startScroll < 2500)
                            {
                                CheckPauseResume(token);
                                Point? khoLoc = ImageFinder.FindImageLocation(_targetHWnd, "Images\\kho_do_bicheon_icon.png", 0.68);
                                if (khoLoc.HasValue)
                                {
                                    foundKho = true;
                                    break;
                                }
                                Win32API.SendScrollDown(_targetHWnd, npcLoc.Value.X, npcLoc.Value.Y);
                                await Task.Delay(150, token);
                            }

                            if (foundKho)
                            {
                                currentStep = 6;
                                retryCount = 0;
                            }
                            else
                            {
                                retryCount++;
                                if (retryCount >= 3) needFullReset = true;
                            }
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= 3) needFullReset = true;
                        }
                    }
                    else if (currentStep == 6)
                    {
                        // Bước 6: Click nút Tele đến kho
                        Point? teleLoc = await WaitForImageAsync("tele_kho.png", token, 3000);
                        if (teleLoc.HasValue)
                        {
                            Win32API.SendClick(_targetHWnd, teleLoc.Value.X, teleLoc.Value.Y);
                            await Task.Delay(3000, token);
                            
                            // Hoàn thành chu trình tele kho thành công! Dừng bot tại đây.
                            return;
                        }
                        else
                        {
                            retryCount++;
                            if (retryCount >= 3) needFullReset = true;
                        }
                    }
                }

                // Nếu vòng lặp con thoát ra vì needFullReset == true, 
                // vòng lặp while(true) sẽ tiếp tục chạy từ đầu (ấn ESC 2 lần).
            }
        }
        catch (OperationCanceledException)
        {
            // Bắt sự kiện khi người dùng bấm nút Dừng
        }
        finally
        {
            lock (_lockObj)
            {
                _currentState = State.Stopped;
            }
        }
    }
}
