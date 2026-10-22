using System;
using System.Drawing;
using System.Windows.Forms;

public partial class MainForm : Form
{
    private TextBox txtWindowIndex;
    private Button btnLock;
    private Button btnStart;
    private Button btnStop;
    private Button btnPause;
    private Button btnResume;
    private Label lblStatus;
    
    private BotEngine botEngine;
    private IntPtr currentHWnd = IntPtr.Zero;

    public MainForm()
    {
        InitializeComponentCustom();
        botEngine = new BotEngine();
    }

    private void InitializeComponentCustom()
    {
        this.Text = "MIR4 Inventory Bot (Background Control)";
        this.Width = 420;
        this.Height = 260;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        // Ô nhập số cửa sổ và nút Khóa
        Label lblIndex = new Label() { Text = "Số cửa sổ (1-100):", Left = 20, Top = 20, Width = 110, TextAlign = ContentAlignment.MiddleLeft };
        txtWindowIndex = new TextBox() { Text = "1", Left = 135, Top = 22, Width = 60 };
        
        btnLock = new Button() { Text = "Khoá", Left = 205, Top = 20, Width = 80, Height = 25 };
        btnLock.Click += BtnLock_Click;

        // Các nút điều khiển
        btnStart = new Button() { Text = "Bắt đầu", Left = 20, Top = 70, Width = 120, Height = 40, Enabled = false };
        btnStart.Click += (s, e) => { botEngine.Start(); lblStatus.Text = "Trạng thái: Đang chạy bot..."; };

        btnStop = new Button() { Text = "Dừng", Left = 165, Top = 70, Width = 120, Height = 40 };
        btnStop.Click += (s, e) => { botEngine.Stop(); lblStatus.Text = "Trạng thái: Đã dừng (Về vạch xuất phát)"; };

        btnPause = new Button() { Text = "Tạm dừng", Left = 20, Top = 125, Width = 120, Height = 40 };
        btnPause.Click += (s, e) => { botEngine.Pause(); lblStatus.Text = "Trạng thái: Đang tạm dừng"; };

        btnResume = new Button() { Text = "Tiếp tục", Left = 165, Top = 125, Width = 120, Height = 40 };
        btnResume.Click += (s, e) => { botEngine.Resume(); lblStatus.Text = "Trạng thái: Tiếp tục chạy..."; };

        // Nhãn trạng thái
        lblStatus = new Label() { Text = "Trạng thái: Chưa khoá cửa sổ nào", Left = 20, Top = 180, Width = 360 };

        this.Controls.Add(lblIndex);
        this.Controls.Add(txtWindowIndex);
        this.Controls.Add(btnLock);
        this.Controls.Add(btnStart);
        this.Controls.Add(btnStop);
        this.Controls.Add(btnPause);
        this.Controls.Add(btnResume);
        this.Controls.Add(lblStatus);
    }

    private void BtnLock_Click(object? sender, EventArgs e)
    {
        if (int.TryParse(txtWindowIndex.Text, out int index))
        {
            // Tên định dạng cửa sổ game MIR4 (ví dụ: Min4G(5))
            string keyword = $"Min4G({index})";
            currentHWnd = Win32API.FindWindowByTitleKeyword(keyword);

            if (currentHWnd != IntPtr.Zero)
            {
                // Điều chỉnh kích thước cửa sổ về mặc định yêu cầu: 811x475
                Win32API.MoveWindow(currentHWnd, 100, 100, 811, 475, true);
                botEngine.SetTarget(currentHWnd);
                btnStart.Enabled = true;
                lblStatus.Text = $"Trạng thái: Đã khoá acc số {index} (Size: 811x475)";
                MessageBox.Show($"Đã khóa thành công cửa sổ số {index}!\nKích thước đã ép về chuẩn: 811x475", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Không tìm thấy cửa sổ game nào chứa tên '{keyword}'!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            MessageBox.Show("Vui lòng nhập một số nguyên hợp lệ từ 1 đến 100!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
