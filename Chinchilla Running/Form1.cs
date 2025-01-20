using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Timers;
using Microsoft.VisualBasic.Devices; // �ޥ� Microsoft.VisualBasic �R�W�Ŷ��A�Ω�����t�Ωʯ�H��(�O����)


namespace Chinchilla_Running
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;  // �Ω���ܨt�Φ��L�ϼ�
        private PerformanceCounter cpuCounter;  // �ΨӺʴ� CPU �ϥβv
        private float cpuUsage;  // �ثe�� CPU �ϥβv
        private System.Timers.Timer animationTimer;  // �Ω󱱨�ʵe��s���w�ɾ�
        private int currentFrame = 0;  // ��e���ʵe�V
        private Icon[] hamsterIcons;  // �s�񵳹��ʵe�V���ϼа}�C
        private volatile bool isDisposed;  // �аO�O�_�w�Q����
        private PerformanceCounter memoryCounter; // �Ω�ʴ��i�ΰO����
        private float memoryUsage;                // �ثe���O����ϥβv

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
                Text = "��l�Ƥ�...", // ��l��
                Visible = true,
                Icon = hamsterIcons[0]  // �]�m��l�ϥܬ��Ĥ@�Ӱʵe�V
            };

            // �]�m�k����
            var contextMenu = new ContextMenuStrip();

            // �K�[�h�X�ﶵ
            contextMenu.Items.Add("�h�X", null, (sender, e) =>
            {
                // ��ܽT�{�ءA���ϥΪ̽T�{�O�_�h�X
                if (MessageBox.Show("�n�������𮧤F��?", "�h�X", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Application.Exit(); // �h�X�{��
                }
            });

            // �]�m���
            notifyIcon.ContextMenuStrip = contextMenu;

            // �]�m���������ƥ�
            notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

            // �Ұʩw�ɧ�s NotifyIcon �� Text
            StartNotifyIconUpdater();
        }

        // �Ұ� NotifyIcon �����ܤ�r��s��
        private void StartNotifyIconUpdater()
        {
            var notifyIconUpdater = new System.Timers.Timer(1000); // �C���s�@��
            notifyIconUpdater.Elapsed += (s, e) =>
            {
                try
                {
                    // ��� CPU �M�O����ϥβv
                    float cpuUsage = GetCpuUsage();
                    float memoryUsage = GetMemoryUsage();

                    // ��s NotifyIcon �����ܤ�r
                    notifyIcon.Text = $"CPU �ϥβv: {cpuUsage:F1}%\n�O����ϥβv: {memoryUsage:F1}%";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"��s NotifyIcon ���ܤ�r���ѡG{ex.Message}");
                }
            };

            notifyIconUpdater.Start();
        }

        // ��l�� CPU �ʯ�p�ƾ�
        private void InitializeCpuCounter()
        {
            try
            {
                // �ϥ� PerformanceCounter �Ӻʱ� CPU �ϥβv
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                // ��l�ưO����p�ƾ��A����i�ΰO����
                memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                // ��lŪ���X��������ǽT���ƾ�
                for (int i = 0; i < 3; i++)
                {
                    cpuCounter.NextValue();
                    memoryCounter.NextValue();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"�L�k��l�Ʃʯ�p�ƾ��G{ex.Message}", ex);
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

        // ����O����ϥβv
        private float GetMemoryUsage()
        {
            try
            {
                // �ʺA����t�Ϊ��`���s�j�p�]�H MB �����^
                ComputerInfo computerInfo = new ComputerInfo();
                float totalMemoryInMB = computerInfo.TotalPhysicalMemory / (1024 * 1024); // �N byte �ഫ�� MB

                // ����i�ΰO����
                float availableMemory = memoryCounter.NextValue();

                // �p��ϥβv�ʤ���
                return (1 - (availableMemory / totalMemoryInMB)) * 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"����O����ϥβv���ѡG{ex.Message}");
                return 0.1f; // �Y���ѫh��^�q�{��
            }
        }


        // �[�������ʵe�ϥ�
        private void LoadHamsterIcons()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            hamsterIcons = new Icon[34]; // 34 �V�]51 - 18 + 1�^
            string defaultIconPath = Path.Combine(basePath, "Image", "16x16", "ico_pack_16x16_chinchilla (1).ico");

            // �p�G�䤣��w�]�ϥܡA�ߥX���`
            if (!File.Exists(defaultIconPath))
            {
                throw new FileNotFoundException("�䤣��w�]�ϥ��ɮ�", defaultIconPath);
            }

            // �[���� 18 �V��� 51 �V���ϥ�
            for (int i = 0; i < 34; i++)  // 34 �V�ϥ�
            {
                string iconPath = Path.Combine(basePath, "Image", "16x16", $"ico_pack_16x16_chinchilla ({i + 18}).ico");
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
                    memoryUsage = Math.Max(0.1f, GetMemoryUsage());

                    // �ھ� CPU �M�O����ϥβv�p��ʵe���j
                    float avgUsage = (cpuUsage + memoryUsage) / 2;
                    animationTimer.Interval = Math.Max(20, Math.Min(1000 / Math.Max(avgUsage, 0.1f), 200));

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
            // �w�q�H���y�l
            string[] sentences = new string[]
            {
                "�g�{�����P�ɡA�]�n�g�U�ּ֪��ɨ�I",
                "�N�X�i�Hdebug�A����n�����@�I",
                "�C�Ӥp�ɯ��_�Ө����A�������M���I",
                "���U�n��éM�ӻH�A���̬O�A���o�O�U��I",
                "�x����bug�׷|�ѨM�A���P����I",
                "���n��ۤv���Y��A�i�B�O���i���I",
                "�C�ѨM�@�Ӱ��D�A���O�@�������I",
                "coding���l�A�O�ѤF��P�Ʋ��ѡI",
                "�M�������ɡA�`�I�l�A�C�C�ӡI",
                "�O�o�ɨ��ͬ��A�{���u�O�䤤�@�����I",
                "�A�ɪ��𮧡A�O���ɮĲv������I",
                "�u�@�A���A�]�n�O�o�ɨ������I",
                "�A���C�@��{���X�A���b���ܥ@�ɡI",
                "���Ѫ��V�O�A�O���Ѫ��i�B�I",
                "�۫H�ۤv�A�A��Q������j�j�I",
                "�x���O�Ȯɪ��A���A�������O�ä[���I",
                "�ӪM�Ť��A���߱��]�x�x���I",
                "���r���l�A�O�o���ʤ����I",
                "�U�Z��A���ۤv�@�Ӽ��y�a�I",
                "�֤F�N�𮧡A���ѦA�ԡI"
            };

            // �H����ܤ@�y
            Random random = new Random();
            string randomSentence = sentences[random.Next(sentences.Length)];

            // ��ܰT��
            MessageBox.Show(randomSentence, "�������b�V�O�]�B���I", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
