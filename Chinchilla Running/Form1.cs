using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Timers;


namespace Chinchilla_Running
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;  // �Ω���ܨt�Φ��L�ϼ�
        private PerformanceCounter cpuCounter;  // �ΨӺʴ� CPU �ϥβv
        private float cpuUsage;  // �ثe�� CPU �ϥβv
        private System.Timers.Timer animationTimer;  // �Ω󱱨�ʵe��s���w�ɾ�
        private int currentFrame = 0;  // ��e���ʵe�V
        private Icon[] hamsterIcons;  // �s��ܹ��ʵe�V���ϼа}�C
        private volatile bool isDisposed;  // �аO�O�_�w�Q����

        public Form1()
        {
            InitializeComponent();

            try
            {
                // ��l�ƹϥܰ}�C
                LoadHamsterIcons();

                // ��l�� NotifyIcon
                notifyIcon = new NotifyIcon
                {
                    Text = "�����]�B�ʵe",
                    Visible = true,
                    Icon = hamsterIcons[0]  // �]�m��l�ϥܬ��Ĥ@�Ӱʵe�V
                };

                // ��l�� NotifyIcon
                InitializeNotifyIcon();

                // ��l�� CPU �ʯ�p�ƾ�
                InitializeCpuCounter();

                // �Ұʰʵe�w�ɾ�
                StartAnimation();

                // ���åD���f
                this.Hide();
            }
            catch (Exception ex)
            {
                // �����l�ƹL�{�������~�A����ܿ��~�T��
                MessageBox.Show($"��l�ƥ��ѡG{ex.Message}", "���~", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        // ��l�� NotifyIcon�A�]�A�]�m�k����M�����ƥ�
        private void InitializeNotifyIcon()
        {           
            notifyIcon = new NotifyIcon()
            {
                Text = "�����b�]�ʵe",
                Visible = true,
                Icon = hamsterIcons[0]  // �]�m��l�ϥܬ��Ĥ@�Ӱʵe�V
            };

            // �]�m�k����
            var contextMenu = new ContextMenuStrip();

            // �K�[�h�X�ﶵ
            contextMenu.Items.Add("�h�X", null, (sender, e) =>
            {
                // ��ܽT�{�ءA���ϥΪ̽T�{�O�_�h�X
                if (MessageBox.Show("�T�w�h�X���ε{��?", "�h�X", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Exit(); // �h�X�{��
                }
            });

            // �]�m���
            notifyIcon.ContextMenuStrip = contextMenu;

            // �]�m���������ƥ�
            notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
        }

        // ��l�� CPU �ʯ�p�ƾ�
        private void InitializeCpuCounter()
        {
            try
            {
                // �ϥ� PerformanceCounter �Ӻʱ� CPU �ϥβv
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                // ��lŪ���X��������ǽT���ƾ�
                for (int i = 0; i < 3; i++)
                {
                    cpuCounter.NextValue();
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"�L�k��l�� CPU �p�ƾ��G{ex.Message}", ex);
            }
        }

        // ��� CPU �ϥβv
        private float GetCpuUsage()
        {
            try
            {
                return cpuCounter?.NextValue() ?? 0.1f; // �p�GŪ�����ѡA��^ 0.1
            }
            catch (Exception ex)
            {
                Console.WriteLine($"��� CPU �ϥβv���ѡG{ex.Message}");
                return 0.1f;
            }
        }

        // �[�������ʵe�ϥ�
        private void LoadHamsterIcons()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            hamsterIcons = new Icon[76];
            string defaultIconPath = Path.Combine(basePath, "Image", "16x16", "ico_pack_16x16_chinchilla (1).ico");

            // �p�G�䤣��w�]�ϥܡA�ߥX���`
            if (!File.Exists(defaultIconPath))
            {
                throw new FileNotFoundException("�䤣��w�]�ϥ��ɮ�", defaultIconPath);
            }

            // �[���C�@�V�ϥ�
            for (int i = 0; i < 76; i++)
            {
                string iconPath = Path.Combine(basePath, "Image", "16x16", $"ico_pack_16x16_chinchilla ({i + 1}).ico");
                try
                {
                    hamsterIcons[i] = File.Exists(iconPath) ? new Icon(iconPath) : new Icon(defaultIconPath);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"���J�ϥܥ��ѡG{ex.Message}", ex);
                }
            }

            // �]�w��l�ϥ�
            if (notifyIcon != null && hamsterIcons[0] != null)
            {
                notifyIcon.Icon = hamsterIcons[0];
            }
        }

        // �}�l�ʵe�w�ɾ��A��s�ϥ�
        private void StartAnimation()
        {
            animationTimer = new System.Timers.Timer();
            animationTimer.Elapsed += (s, e) =>
            {
                if (isDisposed) return;

                try
                {
                    cpuUsage = Math.Max(0.1f, GetCpuUsage());

                    // �ھ� CPU �ϥβv�վ�ʵe��s���j
                    animationTimer.Interval = Math.Max(50, Math.Min(500 / Math.Max(cpuUsage, 0.1f), 200));

                    // �p���e�ʵe�V
                    currentFrame = (currentFrame + 1) % hamsterIcons.Length;

                    // �ϥ� Invoke �T�O�b UI ������W��s�ϥ�
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
                    // �����w���񪫥󪺨ҥ~
                }
                catch (Exception ex)
                {
                    // ��ܿ��~�q��
                    this.Invoke((MethodInvoker)delegate
                    {
                        notifyIcon.ShowBalloonTip(3000, "���~", $"�ʵe��s���~�G{ex.Message}", ToolTipIcon.Error);
                    });
                }
            };
            animationTimer.Start();
        }

        // ���������ƥ�A�i�H����۩w�q���欰�]�p������Ϊ��]�m�ɭ����^
        private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("�ܹ����b�V�O�]�B���I", "����", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // �{�ǵ����ɻݭn�M�z���L�ϼ�
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            notifyIcon.Visible = false; // �h�X�����æ��L�ϼ�
        }

        // ����귽
        protected override void Dispose(bool disposing)
        {
            if (disposing && !isDisposed)
            {
                isDisposed = true;
                notifyIcon?.Dispose();
                cpuCounter?.Dispose();
                animationTimer?.Stop(); // ����p�ɾ�
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
