using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Timers;
using Microsoft.VisualBasic.Devices; // 引用 Microsoft.VisualBasic 命名空間，用於獲取系統性能信息(記憶體)


namespace Chinchilla_Running
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;  // 用於顯示系統托盤圖標
        private PerformanceCounter cpuCounter;  // 用來監測 CPU 使用率
        private float cpuUsage;  // 目前的 CPU 使用率
        private System.Timers.Timer animationTimer;  // 用於控制動畫更新的定時器
        private int currentFrame = 0;  // 當前的動畫幀
        private Icon[] hamsterIcons;  // 存放絨鼠動畫幀的圖標陣列
        private volatile bool isDisposed;  // 標記是否已被釋放
        private PerformanceCounter memoryCounter; // 用於監測可用記憶體
        private float memoryUsage;                // 目前的記憶體使用率

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
                Text = "初始化中...", // 初始值
                Visible = true,
                Icon = hamsterIcons[0]  // 設置初始圖示為第一個動畫幀
            };

            // 設置右鍵菜單
            var contextMenu = new ContextMenuStrip();

            // 添加退出選項
            contextMenu.Items.Add("退出", null, (sender, e) =>
            {
                // 顯示確認框，讓使用者確認是否退出
                if (MessageBox.Show("要讓絨鼠休息了嗎?", "退出", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Exit(); // 退出程序
                }
            });

            // 設置菜單
            notifyIcon.ContextMenuStrip = contextMenu;

            // 設置鼠標雙擊事件
            notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

            // 啟動定時更新 NotifyIcon 的 Text
            StartNotifyIconUpdater();
        }

        // 啟動 NotifyIcon 的提示文字更新器
        private void StartNotifyIconUpdater()
        {
            var notifyIconUpdater = new System.Timers.Timer(1000); // 每秒更新一次
            notifyIconUpdater.Elapsed += (s, e) =>
            {
                try
                {
                    // 獲取 CPU 和記憶體使用率
                    float cpuUsage = GetCpuUsage();
                    float memoryUsage = GetMemoryUsage();

                    // 更新 NotifyIcon 的提示文字
                    notifyIcon.Text = $"CPU 使用率: {cpuUsage:F1}%\n記憶體使用率: {memoryUsage:F1}%";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新 NotifyIcon 提示文字失敗：{ex.Message}");
                }
            };

            notifyIconUpdater.Start();
        }

        // 初始化 CPU 性能計數器
        private void InitializeCpuCounter()
        {
            try
            {
                // 使用 PerformanceCounter 來監控 CPU 使用率
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                // 初始化記憶體計數器，獲取可用記憶體
                memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                // 初始讀取幾次來獲取準確的數據
                for (int i = 0; i < 3; i++)
                {
                    cpuCounter.NextValue();
                    memoryCounter.NextValue();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"無法初始化性能計數器：{ex.Message}", ex);
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

        // 獲取記憶體使用率
        private float GetMemoryUsage()
        {
            try
            {
                // 動態獲取系統的總內存大小（以 MB 為單位）
                ComputerInfo computerInfo = new ComputerInfo();
                float totalMemoryInMB = computerInfo.TotalPhysicalMemory / (1024 * 1024); // 將 byte 轉換為 MB

                // 獲取可用記憶體
                float availableMemory = memoryCounter.NextValue();

                // 計算使用率百分比
                return (1 - (availableMemory / totalMemoryInMB)) * 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"獲取記憶體使用率失敗：{ex.Message}");
                return 0.1f; // 若失敗則返回默認值
            }
        }


        // 加載絨鼠動畫圖示
        private void LoadHamsterIcons()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            hamsterIcons = new Icon[34]; // 34 幀（51 - 18 + 1）
            string defaultIconPath = Path.Combine(basePath, "Image", "16x16", "ico_pack_16x16_chinchilla (1).ico");

            // 如果找不到預設圖示，拋出異常
            if (!File.Exists(defaultIconPath))
            {
                throw new FileNotFoundException("找不到預設圖示檔案", defaultIconPath);
            }

            // 加載第 18 幀到第 51 幀的圖示
            for (int i = 0; i < 34; i++)  // 34 幀圖示
            {
                string iconPath = Path.Combine(basePath, "Image", "16x16", $"ico_pack_16x16_chinchilla ({i + 18}).ico");
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
                    memoryUsage = Math.Max(0.1f, GetMemoryUsage());

                    // 根據 CPU 和記憶體使用率計算動畫間隔
                    float avgUsage = (cpuUsage + memoryUsage) / 2;
                    animationTimer.Interval = Math.Max(20, Math.Min(1000 / Math.Max(avgUsage, 0.1f), 200));

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
            // 定義隨機句子
            string[] sentences = new string[]
            {
                "寫程式的同時，也要寫下快樂的時刻！",
                "代碼可以debug，身體要先維護！",
                "每個小時站起來走走，讓思路更清晰！",
                "照顧好手腕和肩膀，它們是你的得力助手！",
                "困難的bug終會解決，放輕鬆面對！",
                "不要對自己太嚴格，進步是漸進的！",
                "每解決一個問題，都是一次成長！",
                "coding之餘，別忘了跟同事聊聊天！",
                "專案難關時，深呼吸，慢慢來！",
                "記得享受生活，程式只是其中一部分！",
                "適時的休息，是提升效率的關鍵！",
                "工作再忙，也要記得享受陽光！",
                "你的每一行程式碼，都在改變世界！",
                "今天的努力，是明天的進步！",
                "相信自己，你比想像中更強大！",
                "困難是暫時的，但你的成長是永久的！",
                "來杯溫水，讓心情也暖暖的！",
                "打字之餘，記得活動手指喔！",
                "下班後，給自己一個獎勵吧！",
                "累了就休息，明天再戰！"
            };

            // 隨機選擇一句
            Random random = new Random();
            string randomSentence = sentences[random.Next(sentences.Length)];

            // 顯示訊息
            MessageBox.Show(randomSentence, "絨鼠正在努力跑步中！", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
