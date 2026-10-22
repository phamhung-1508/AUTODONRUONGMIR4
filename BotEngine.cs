using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class BotEngine
{
    private IntPtr _targetHWnd = IntPtr.Zero;
    private CancellationTokenSource? _cts;
    private enum State { Stopped, Running, Paused }
    private State _currentState = State.Stopped;
    private readonly object _lockObj = new object();

    public void SetTarget(IntPtr hWnd)
    {
        _targetHWnd = hWnd;
    }

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

    public void Stop()
    {
        lock (_lockObj)
        {
            _cts?.Cancel();
            _currentState = State.Stopped;
        }
    }

    public void Pause()
    {
        lock (_lockObj)
        {
            if (_currentState == State.Running)
            {
                _currentState = State.Paused;
            }
        }
    }

    public void Resume()
    {
        lock (_lockObj)
        {
            if (_currentState == State.Paused)
            {
                _currentState = State.Running;
            }
        }
    }

    private async Task RunBotLoop(CancellationToken token)
    {
        try
        {
            // Bước 1: Ấn ESC 2 lần để đảm bảo màn hình về chế độ mặc định
            Win32API.SendKey(_targetHWnd, Keys.Escape);
            await Task.Delay(400, token);
            CheckPauseResume(token);

            Win32API.SendKey(_targetHWnd, Keys.Escape);
            await Task.Delay(800, token);
            CheckPauseResume(token);

            // Bước 2: Mô phỏng hành trình di chuyển đến kho đồ (Dựa theo video mẫu)
            // Click vào icon menu bản đồ góc trên phải cửa sổ (Tọa độ ví dụ mẫu 811x475: x=750, y=40)
            Win32API.SendClick(_targetHWnd, 750, 40);
            await Task.Delay(1000, token);
            CheckPauseResume(token);

            // Bạn có thể bổ sung tiếp các tọa độ click mở bản đồ thế giới, chọn thành, tìm NPC kho đồ ở đây...
        }
        catch (OperationCanceledException)
        {
            // Xử lý khi nhấn nút Dừng (Reset về mốc xuất phát)
        }
        finally
        {
            lock (_lockObj)
            {
                _currentState = State.Stopped;
            }
        }
    }

    private void CheckPauseResume(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            bool isPaused;
            lock (_lockObj)
            {
                isPaused = (_currentState == State.Paused);
            }
            if (!isPaused) break;
            Thread.Sleep(200);
        }
    }
}
