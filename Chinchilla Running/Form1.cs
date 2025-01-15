using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Timers;


namespace Chinchilla_Running
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private PerformanceCounter cpuCounter;
        private System.Timers.Timer animationTimer;
        private int currentFrame = 0;
        private Icon[] hamsterIcons; // 存放倉鼠動畫幀的圖標

        public Form1()
        {
            InitializeComponent();

            // 隱藏主窗口
            this.Hide();

            // 初始化 NotifyIcon
            InitializeNotifyIcon();

            // 初始化 CPU 性能計數器
            InitializeCpuCounter();

            // 加載倉鼠動畫圖標
            LoadHamsterIcons();

            // 啟動動畫定時器
            StartAnimation();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon()
            {
                Icon = hamsterIcons[0], // 預設顯示第一幀
                Text = "倉鼠奔跑動畫",
                Visible = true
            };

            // 設置右鍵菜單
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("退出", null, (sender, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = contextMenu;

            // 可以設置鼠標雙擊的事件來做一些操作
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
        }

        private void InitializeCpuCounter()
        {
            // 取得 CPU 使用率
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        private float GetCpuUsage()
        {
            return cpuCounter.NextValue();
        }

        private void LoadHamsterIcons()
        {
            // 假設有 5 幀動畫，命名為 hamster1.ico, hamster2.ico...
            hamsterIcons = new Icon[5];
            for (int i = 0; i < 5; i++)
            {
                hamsterIcons[i] = new Icon($"hamster{i + 1}.ico"); // 記得將你的動畫幀保存為 .ico 格式
            }
        }

        private void StartAnimation()
        {
            animationTimer = new System.Timers.Timer();
            animationTimer.Interval = 100; // 設置初始定時器間隔時間（100 毫秒）
            animationTimer.Elapsed += (s, e) =>
            {
                float cpuUsage = GetCpuUsage();
                animationTimer.Interval = Math.Max(50, 500 / cpuUsage); // 根據 CPU 使用率調整動畫速度

                // 更新顯示的動畫幀
                currentFrame = (currentFrame + 1) % hamsterIcons.Length;
                notifyIcon.Icon = hamsterIcons[currentFrame];
            };
            animationTimer.Start();
        }

        // 鼠標雙擊事件，可以執行自定義的行為（如顯示應用的設置界面等）
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("鼠標雙擊托盤圖標!");
        }

        // 程序結束時需要清理托盤圖標
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            notifyIcon.Visible = false; // 退出時隱藏托盤圖標
        }
    }
}
