namespace Chinchilla_Running
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            notifyIcon1 = new NotifyIcon(components);
            SuspendLayout();
            // 
            // notifyIcon1
            // 
            notifyIcon1.Text = "notifyIcon1";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseDoubleClick += notifyIcon1_MouseDoubleClick; // 綁定雙擊事件
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 438);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load; // 綁定加載事件
            ResumeLayout(false);
        }

        #endregion

        private NotifyIcon notifyIcon1;

        // Form1 加載事件處理
        private void Form1_Load(object sender, EventArgs e)
        {
            // 在此進行初始化設定
            // 例如設定托盤圖標、計時器等
        }

        // notifyIcon1 雙擊事件處理
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // 處理鼠標雙擊事件
            // 比如顯示一個訊息框
            MessageBox.Show("雙擊了系統托盤圖標!");
        }
    }
}
