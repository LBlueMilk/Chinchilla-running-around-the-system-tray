using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;  // 引用 Microsoft.VisualBasic 命名空間，用於獲取系統性能信息(記憶體)

namespace Chinchilla_Running
{
    internal static class Program
    {
        private static NotifyIcon notifyIcon;  // 用於顯示系統托盤圖示
        private static PerformanceCounter cpuCounter;  // 用來監控 CPU 使用率
        private static PerformanceCounter memoryCounter;  // 用來監控記憶體使用率
        private static float cpuUsage;  // 當前的 CPU 使用率
        private static float memoryUsage;  // 當前的記憶體使用率
        private static System.Timers.Timer animationTimer;  // 用於控制動畫更新的定時器
        private static int currentFrame = 0;  // 當前的動畫幀
        private static Icon[] hamsterIcons;  // 存放絨鼠動畫幀的圖示數組
        private static volatile bool isDisposed = false;  // 記錄是否已被釋放

        // 用於控制通知頻率
        private static DateTime lastCpuNotification = DateTime.MinValue;
        private static DateTime lastMemoryNotification = DateTime.MinValue;
        private static DateTime lastCombinedNotification = DateTime.MinValue;
        private static readonly TimeSpan NotificationCooldown = TimeSpan.FromMinutes(5); // 通知冷卻時間為5分鐘

        private static bool isNotificationEnabled = true;  // 是否啟用通知功能
        private static readonly object lockObj = new object();  // 用於保護 NotifyIcon.Text 的訪問

        // 用於記錄閾值超過的開始時間
        private static DateTime cpuThresholdStart = DateTime.MinValue;
        private static DateTime memoryThresholdStart = DateTime.MinValue;
        private static DateTime combinedThresholdStart = DateTime.MinValue;

        private static readonly TimeSpan ThresholdDuration = TimeSpan.FromSeconds(5);  // 閾值持續時間為5秒

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            try
            {
                // 初始化系統托盤圖示和相關功能
                InitializeNotifyIcon();

                // 初始化性能計數器
                InitializePerformanceCounters();

                // 啟動動畫
                StartAnimation();

                // 啟動性能監控
                StartUnifiedTimer(); 

                // 註冊應用程式退出處理
                Application.ApplicationExit += OnApplicationExit;

                // 執行主程序迴圈
                Application.Run();
            }
            catch (Exception ex)
            {
                // 處理初始化過程中的錯誤，並顯示錯誤訊息
                MessageBox.Show($"初始化失敗：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        // 初始化系統托盤圖示
        private static void InitializeNotifyIcon()
        {
            try
            {
                // 加載動畫圖示
                LoadHamsterIcons();

                // 初始化 NotifyIcon
                notifyIcon = new NotifyIcon
                {
                    Text = "絨鼠跑步動畫",
                    Icon = hamsterIcons[0],
                    Visible = true
                };

                // 設置右鍵選單
                var contextMenu = new ContextMenuStrip();

                // 添加開啟/關閉通知選項
                var toggleNotificationItem = new ToolStripMenuItem("關閉預警");
                toggleNotificationItem.Click += (sender, e) =>
                {
                    isNotificationEnabled = !isNotificationEnabled;
                    toggleNotificationItem.Text = isNotificationEnabled ? "關閉預警" : "開啟預警";
                };
                contextMenu.Items.Add(toggleNotificationItem);

                // 添加釘選到系統匣圖示的說明
                var pinToTrayMenuItem = new ToolStripMenuItem("想一直看著絨鼠奔跑嗎?(ゝ∀･)b");
                pinToTrayMenuItem.Click += (sender, e) =>
                {
                    MessageBox.Show(
                        "若要釘選本程式圖示，請按以下步驟操作：\n\n" +
                        "1. 右鍵點擊工具列，選擇「工具列設定」。\n" +
                        "2. 找到「其他系統匣圖示」，將其展開。\n" +
                        "3. 在列表中找到本程式Chinchilla Running，將其設為「開啟」。",
                        "如何釘選通知圖示",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                };
                contextMenu.Items.Add(pinToTrayMenuItem);

                // 添加退出選項
                contextMenu.Items.Add("退出", null, (sender, e) =>
                {
                    if (MessageBox.Show("要讓絨鼠休息了嗎?(´,,•ω•,,)♡", "退出", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        // 自訂訊息窗
                        var thankYouMessage = new Form
                        {
                            Size = new Size(200, 50), // 調整大小
                            StartPosition = FormStartPosition.CenterScreen,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            ControlBox = false, // 禁用關閉按鈕
                            ShowInTaskbar = false,
                            BackColor = Color.LightYellow, // 背景顏色
                        };

                        // 添加標籤
                        var label = new Label
                        {
                            Text = "都辛苦了(*´-`*)ﾉ",
                            Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Font = new Font("Microsoft YaHei", 12, FontStyle.Regular)
                        };
                        thankYouMessage.Controls.Add(label);

                        // 啟動計時器，3秒後自動關閉
                        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop();
                            thankYouMessage.Close();
                            Application.Exit(); // 確保應用程序完全退出
                        };
                        timer.Start();

                        thankYouMessage.ShowDialog(); // 顯示訊息窗
                    }
                });

                // 設置 NotifyIcon 的右鍵選單
                notifyIcon.ContextMenuStrip = contextMenu;

                // 設置滑鼠雙擊處理
                notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"無法初始化系統托盤圖示：{ex.Message}", ex);
            }
        }

        // 加載絨鼠動畫圖示
        private static void LoadHamsterIcons()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;  // 獲取應用程式的基礎目錄
            string iconFolderPath = Path.Combine(basePath, "Image", "16x16");  // 圖示資料夾路徑

            hamsterIcons = new Icon[34]; // 34 幀(51 - 18 + 1)
            string defaultIconPath = Path.Combine(iconFolderPath, "ico_pack_16x16_chinchilla (1).ico");  // 預設圖示

            if (!File.Exists(defaultIconPath))
            {
                throw new FileNotFoundException("找不到預設圖示檔案", defaultIconPath);
            }

            for (int i = 0; i < 34; i++)
            {
                string iconPath = Path.Combine(iconFolderPath, $"ico_pack_16x16_chinchilla ({i + 18}).ico");
                hamsterIcons[i] = File.Exists(iconPath) ? new Icon(iconPath) : new Icon(defaultIconPath);
            }
        }

        // 初始化性能計數器
        private static void InitializePerformanceCounters()
        {
            try
            {
                // 使用 PerformanceCounter 來監測 CPU 使用率
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
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
                Console.WriteLine($"無法初始化性能計數器：{ex.Message}");
                cpuCounter = null;
                memoryCounter = null;
            }
        }

        // 啟動動畫
        private static void StartAnimation()
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

                    if (!isDisposed && notifyIcon != null)
                    {
                        notifyIcon.Icon = hamsterIcons[currentFrame];
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 忽略已釋放物件的例外
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"動畫更新錯誤：{ex.Message}");
                }
            };
            animationTimer.Start();
        }

        // 啟動性能監控
        private static void StartUnifiedTimer()
        {
            var unifiedTimer = new System.Timers.Timer(1000); // 每秒觸發一次
            unifiedTimer.Elapsed += (s, e) =>
            {
                try
                {
                    // 獲取 CPU 和記憶體使用率
                    float cpuUsage = GetCpuUsage();
                    float memoryUsage = GetMemoryUsage();

                    // 使用 lock 保護多線程訪問
                    lock (lockObj)
                    {
                        notifyIcon.Text = $"CPU: {cpuUsage:F1}% | 記憶體: {memoryUsage:F1}%";
                    }

                    // 檢查通知是否關閉
                    if (isDisposed || !isNotificationEnabled) return;

                    // 每 5 秒檢查一次系統效能
                    if (DateTime.Now.Second % 5 == 0) // 每 5 秒執行一次
                    {
                        CheckSystemPerformance();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新系統狀態時發生錯誤：{ex.Message}");
                }
            };
            unifiedTimer.Start();
        }

        // 獲取 CPU 使用率
        private static float GetCpuUsage()
        {
            try
            {
                if (cpuCounter == null) return 0.1f;
                return cpuCounter.NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"獲取 CPU 使用率失敗：{ex.Message}");
                return 0.1f;
            }
        }

        // 獲取記憶體使用率
        private static float GetMemoryUsage()
        {
            try
            {
                if (memoryCounter == null) return 0.1f;
                ComputerInfo computerInfo = new ComputerInfo();
                float totalMemoryInMB = computerInfo.TotalPhysicalMemory / (1024 * 1024);
                float availableMemory = memoryCounter.NextValue();
                return (1 - (availableMemory / totalMemoryInMB)) * 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"獲取記憶體使用率失敗：{ex.Message}");
                return 0.1f;
            }
        }

        // 檢查系統效能，符合則氣泡通知
        private static void CheckSystemPerformance()
        {
            if (isDisposed) return;

            try
            {
                float currentCpuUsage = GetCpuUsage();
                float currentMemoryUsage = GetMemoryUsage();
                DateTime now = DateTime.Now;

                // 檢查 CPU 使用率是否持續超過閾值
                if (currentCpuUsage >= 90)
                {
                    if (cpuThresholdStart == DateTime.MinValue)
                        cpuThresholdStart = now;

                    if ((now - cpuThresholdStart) >= ThresholdDuration && (now - lastCpuNotification) > NotificationCooldown)
                    {
                        ShowBalloonTip(
                            "CPU 使用率警告",
                            $"CPU 使用率已達 {currentCpuUsage:F1}%，建議檢查系統運行狀況。",
                            ToolTipIcon.Warning);
                        lastCpuNotification = now;
                        cpuThresholdStart = DateTime.MinValue; // 重置開始時間
                    }
                }
                else
                {
                    cpuThresholdStart = DateTime.MinValue; // 重置開始時間
                }

                // 檢查記憶體使用率是否持續超過閾值
                if (currentMemoryUsage >= 90)
                {
                    if (memoryThresholdStart == DateTime.MinValue)
                        memoryThresholdStart = now;

                    if ((now - memoryThresholdStart) >= ThresholdDuration && (now - lastMemoryNotification) > NotificationCooldown)
                    {
                        ShowBalloonTip(
                            "記憶體使用率警告",
                            $"記憶體使用率已達 {currentMemoryUsage:F1}%，建議關閉一些應用程式。",
                            ToolTipIcon.Warning);
                        lastMemoryNotification = now;
                        memoryThresholdStart = DateTime.MinValue; // 重置開始時間
                    }
                }
                else
                {
                    memoryThresholdStart = DateTime.MinValue; // 重置開始時間
                }

                // 檢查綜合使用率是否持續超過閾值
                if (currentCpuUsage >= 85 && currentMemoryUsage >= 85)
                {
                    if (combinedThresholdStart == DateTime.MinValue)
                        combinedThresholdStart = now;

                    if ((now - combinedThresholdStart) >= ThresholdDuration && (now - lastCombinedNotification) > NotificationCooldown)
                    {
                        ShowBalloonTip(
                            "系統資源警告",
                            $"CPU ({currentCpuUsage:F1}%) 和記憶體 ({currentMemoryUsage:F1}%) 使用率都很高！\n請注意系統效能。",
                            ToolTipIcon.Warning);
                        lastCombinedNotification = now;
                        combinedThresholdStart = DateTime.MinValue; // 重置開始時間
                    }
                }
                else
                {
                    combinedThresholdStart = DateTime.MinValue; // 重置開始時間
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"監控系統效能時發生錯誤：{ex.Message}");
            }
        }

        // 顯示通知
        private static void ShowBalloonTip(string title, string message, ToolTipIcon icon)
        {
            try
            {
                if (!isDisposed && notifyIcon != null)
                {
                    notifyIcon.ShowBalloonTip(5000, title, message, icon); // 顯示氣泡通知
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"顯示氣泡通知時發生錯誤：{ex.Message}");
            }
        }

        // 滑鼠雙擊事件處理
        private static void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string[] sentences = new string[]
            {
                "不要畏懼錯誤，錯誤是成功的一部分！ (ง •̀_•́)ง",
                "即使遇到挑戰，也要記住每一個小步伐都是進步！ ✧٩(•́⌄•́๑)و ✧",
                "保持學習的熱情，每天都能成為更好的自己！ (｡♥‿♥｡)",
                "程序是冷冰冰的，但你卻是它背後的靈魂！ ( •̀ ω •́ )✧",
                "有時候停下來想一想，才能找到最好的解決方案！ ( ´･ω･)ﾉ(._.`)",
                "當你覺得困難時，想想那些已經解決的問題，你也做到了！ (｀・ω・´)b",
                "無論多忙，都要給自己一點時間喘息，這樣才能更好地前進！ (っ˘ω˘ς )",
                "面對難題，先深呼吸，再開始攻克它！ (๑•̀ㅂ•́)و✧",
                "每次 debug 完畢，都是一次成功的慶祝！ ヽ(✿ﾟ▽ﾟ)ノ",
                "放輕鬆，繼續做你喜歡的事，成功就會來臨！ (￣︶￣)↗",
                "回顧今天的代碼，不斷反思，會讓明天的自己更強大！ (´･ᴗ･ ` )",
                "改進每一行程式碼，就是不斷進化的過程！ （*'∀'人）♥",
                "程序世界無窮無盡，別怕開始，邁出第一步吧！ ╰(°▽°)╯",
                "進步是日積月累，別著急，堅持下去一定能看到成果！ ✨٩(ˊᗜˋ*)و✨",
                "保持耐心，成功從來不會在一夜之間到來！ (*￣▽￣)d",
                "每次解決 bug，都是你智慧的結晶！ (๑•̀ㅂ•́)و✧",
                "挑戰自己，不斷提升，成為最強的工程師！ o(≧∇≦o)",
                "工作中遇到的難題，正是你成長的契機！ (* • ̀ω•́ )b",
                "休息過後，回來看問題，會發現解決的方式更清晰了！ ( ˘ω˘ ) zzZ",
                "從錯誤中學習，讓它們成為你前進的力量！ (ง •̀_•́)ง",
                "專注於解決問題的過程，而不僅僅是結果，這樣你會不斷進步！ ( •̀ ω •́ )✧",
                "挑戰是成長的動力，保持積極的心態，解決每一個難題！ (★ω★)/",
                "每當你完成一個功能，都是一個小小的勝利，為自己慶祝！ ヾ(＾∇＾)",
                "別放棄，最好的程式設計往往在你想放棄的時候誕生！ (ง •̀_•́)ง✧",
                "多思考、多試驗、多改進，這是成為頂尖工程師的關鍵！ ( •̀ .̫ •́ )✧",
                "過程比結果更重要，因為它塑造了你成為一位更強的開發者！ ⊂(･ω･*⊂)",
                "與其擔心失敗，不如專注於每一次實踐，這樣你會更接近成功！ ＼(≧▽≦)／",
                "別害怕未知，勇於探索每一個新的技術，才能開創無限可能！ (*≧ω≦)",
                "當問題看似無解時，轉換視角，新的解決方法就會出現！ (・∀・)",
                "成功的關鍵在於持之以恆，不放棄，每次都向前邁出一小步！ (ง •̀_•́)ง",
                "每次優化代碼，都是一次對自己的挑戰，也是進步的證明！ ヽ(✿ﾟ▽ﾟ)ノ",
                "不怕犯錯，怕的是不從錯誤中學習，勇敢改正，繼續前行！ (๑•̀ㅂ•́)و✧",
                "解決每一個技術難題，都是對自己能力的升級，繼續努力！ o(〃＾▽＾〃)o"
            };

            Random random = new Random();
            string randomSentence = sentences[random.Next(sentences.Length)];
            MessageBox.Show(randomSentence, "絨鼠正在努力奔跑中！", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 釋放資源
        private static void OnApplicationExit(object sender, EventArgs e)
        {
            isDisposed = true;

            // 釋放系統托盤圖示資源
            notifyIcon?.Dispose();

            // 釋放性能計數器資源
            cpuCounter?.Dispose();
            memoryCounter?.Dispose();

            // 停止動畫計時器並釋放其資源
            animationTimer?.Stop();
            animationTimer?.Dispose();
        }
    }
}