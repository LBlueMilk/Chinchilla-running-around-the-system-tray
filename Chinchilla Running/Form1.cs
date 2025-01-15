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
        private Icon[] hamsterIcons; // �s��ܹ��ʵe�V���ϼ�

        public Form1()
        {
            InitializeComponent();

            // ���åD���f
            this.Hide();

            // ��l�� NotifyIcon
            InitializeNotifyIcon();

            // ��l�� CPU �ʯ�p�ƾ�
            InitializeCpuCounter();

            // �[���ܹ��ʵe�ϼ�
            LoadHamsterIcons();

            // �Ұʰʵe�w�ɾ�
            StartAnimation();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon()
            {
                Icon = hamsterIcons[0], // �w�]��ܲĤ@�V
                Text = "�ܹ��b�]�ʵe",
                Visible = true
            };

            // �]�m�k����
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("�h�X", null, (sender, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = contextMenu;

            // �i�H�]�m�����������ƥ�Ӱ��@�Ǿާ@
            notifyIcon.MouseDoubleClick += notifyIcon_MouseDoubleClick;
        }

        private void InitializeCpuCounter()
        {
            // ���o CPU �ϥβv
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        private float GetCpuUsage()
        {
            return cpuCounter.NextValue();
        }

        private void LoadHamsterIcons()
        {
            // ���]�� 5 �V�ʵe�A�R�W�� hamster1.ico, hamster2.ico...
            hamsterIcons = new Icon[5];
            for (int i = 0; i < 5; i++)
            {
                hamsterIcons[i] = new Icon($"hamster{i + 1}.ico"); // �O�o�N�A���ʵe�V�O�s�� .ico �榡
            }
        }

        private void StartAnimation()
        {
            animationTimer = new System.Timers.Timer();
            animationTimer.Interval = 100; // �]�m��l�w�ɾ����j�ɶ��]100 �@��^
            animationTimer.Elapsed += (s, e) =>
            {
                float cpuUsage = GetCpuUsage();
                animationTimer.Interval = Math.Max(50, 500 / cpuUsage); // �ھ� CPU �ϥβv�վ�ʵe�t��

                // ��s��ܪ��ʵe�V
                currentFrame = (currentFrame + 1) % hamsterIcons.Length;
                notifyIcon.Icon = hamsterIcons[currentFrame];
            };
            animationTimer.Start();
        }

        // ���������ƥ�A�i�H����۩w�q���欰�]�p������Ϊ��]�m�ɭ����^
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("�����������L�ϼ�!");
        }

        // �{�ǵ����ɻݭn�M�z���L�ϼ�
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            notifyIcon.Visible = false; // �h�X�����æ��L�ϼ�
        }
    }
}
