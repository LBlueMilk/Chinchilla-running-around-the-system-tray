using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Timers;


namespace Chinchilla_Running
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;  // 用於顯示系統托盤圖標
        private PerformanceCounter cpuCounter;  // 用來監測 CPU 使用率
        private float cpuUsage;  // 目前的 CPU 使用率
        private System.Timers.Timer animationTimer;  // 用於控制動畫更新的定時器
        private int currentFrame = 0;  // 當前的動畫幀
        private Icon[] hamsterIcons;  // 存放倉鼠動畫幀的圖標陣列
        private volatile bool isDisposed;  // 標記是否已被釋放

        public Form1()
        {
            InitializeComponent();

            try
            {
                // 初始化圖示陣列
                LoadHamsterIcons();

                // 初始化 NotifyIcon
                notifyIcon = new NotifyIcon
                {
                    Text = "絨鼠跑步動畫",
                    Visible = true,
                    Icon = hamsterIcons[0]  // 設置初始圖示為第一個動畫幀
                };

                // 初始化 NotifyIcon
                InitializeNotifyIcon();

                // 初始化 CPU 性能計數器
                InitializeCpuCounter();

                // 啟動動畫定時器
                StartAnimation();

                // 隱藏主窗口
                this.Hide();
            }
            catch (Exception ex)
            {
                // 捕獲初始化過程中的錯誤，並顯示錯誤訊息
                MessageBox.Show($"初始化失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        // 初始化 NotifyIcon，包括設置右鍵菜單和雙擊事件
        private void InitializeNotifyIcon()
        {           
            notifyIcon = new NotifyIcon()
            {
                Text = "絨鼠奔跑動畫",
                Visible = true,
                Icon = hamsterIcons[0]  // 設置初始圖示為第一個動畫幀
            };

            // 設置右鍵菜單
            var contextMenu = new ContextMenuStrip();

            // 添加退出選項
            contextMenu.Items.Add("退出", null, (sender, e) =>
            {
                // 顯示確認框，讓使用者確認是否退出
                if (MessageBox.Show("確定退出應用程式?", "退出", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Exit(); // 退出程序
                }
            });

            // 設置菜單
            notifyIcon.ContextMenuStrip = contextMenu;

            // 設置鼠標雙擊事件
            notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
        }

        // 初始化 CPU 性能計數器
        private void InitializeCpuCounter()
        {
            try
            {
                // 使用 PerformanceCounter 來監控 CPU 使用率
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                // 初始讀取幾次來獲取準確的數據
                for (int i = 0; i < 3; i++)
                {
                    cpuCounter.NextValue();
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"無法初始化 CPU 計數器：{ex.Message}", ex);
            }
        }

        // 獲取 CPU 使用率
        private float GetCpuUsage()
        {
            try
            {
                return cpuCounter?.NextValue() ?? 0.1f; // 如果讀取失敗，返回 0.1
            }
            catch (Exception ex)
            {
                Console.WriteLine($"獲取 CPU 使用率失敗：{ex.Message}");
                return 0.1f;
            }
        }

        // 加載絨鼠動畫圖示
        private void LoadHamsterIcons()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            hamsterIcons = new Icon[76];
            string defaultIconPath = Path.Combine(basePath, "Image", "16x16", "ico_pack_16x16_chinchilla (1).ico");

            // 如果找不到預設圖示，拋出異常
            if (!File.Exists(defaultIconPath))
            {
                throw new FileNotFoundException("找不到預設圖示檔案", defaultIconPath);
            }

            // 加載每一幀圖示
            for (int i = 0; i < 76; i++)
            {
                string iconPath = Path.Combine(basePath, "Image", "16x16", $"ico_pack_16x16_chinchilla ({i + 1}).ico");
                try
                {
                    hamsterIcons[i] = File.Exists(iconPath) ? new Icon(iconPath) : new Icon(defaultIconPath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"載入圖示失敗：{ex.Message}", ex);
                }
            }

            // 設定初始圖示
            if (notifyIcon != null && hamsterIcons[0] != null)
            {
                notifyIcon.Icon = hamsterIcons[0];
            }
        }

        // 開始動畫定時器，更新圖示
        private void StartAnimation()
        {
            animationTimer = new System.Timers.Timer();
            animationTimer.Elapsed += (s, e) =>
            {
                if (isDisposed) return;

                try
                {
                    cpuUsage = Math.Max(0.1f, GetCpuUsage());

                    // 根據 CPU 使用率調整動畫更新間隔
                    animationTimer.Interval = Math.Max(50, Math.Min(500 / Math.Max(cpuUsage, 0.1f), 200));

                    // 計算當前動畫幀
                    currentFrame = (currentFrame + 1) % hamsterIcons.Length;

                    // 使用 Invoke 確保在 UI 執行緒上更新圖示
                    if (!IsDisposed && notifyIcon != null)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            notifyIcon.Icon = hamsterIcons[currentFrame];
                        });
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 忽略已釋放物件的例外
                }
                catch (Exception ex)
                {
                    // 顯示錯誤通知
                    this.Invoke((MethodInvoker)delegate
                    {
                        notifyIcon.ShowBalloonTip(3000, "錯誤", $"動畫更新錯誤：{ex.Message}", ToolTipIcon.Error);
                    });
                }
            };
            animationTimer.Start();
        }

        // 鼠標雙擊事件，可以執行自定義的行為（如顯示應用的設置界面等）
        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("倉鼠正在努力跑步中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 程序結束時需要清理托盤圖標
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            notifyIcon.Visible = false; // 退出時隱藏托盤圖標
        }

        // 釋放資源
        protected override void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                isDisposed = true;
                notifyIcon?.Dispose();
                cpuCounter?.Dispose();
                animationTimer?.Stop(); // 停止計時器
                animationTimer?.Dispose();
                foreach (var icon in hamsterIcons)
                {
                    icon?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
