namespace Documenter
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
            txtUrl = new TextBox();
            btnStart = new Button();
            lstLog = new ListBox();
            btnBrowse = new Button();
            lblSelectedPath = new TextBox();
            progressBar1 = new ProgressBar();
            lblStatus = new Label();
            lblTitle = new Label();
            lblGitHubUrl = new Label();
            lblLocalPath = new Label();
            panel1 = new Panel();
            panel2 = new Panel();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // txtUrl
            // 
            txtUrl.BackColor = Color.FromArgb(52, 73, 94);
            txtUrl.BorderStyle = BorderStyle.None;
            txtUrl.Font = new Font("Segoe UI", 11F);
            txtUrl.ForeColor = Color.White;
            txtUrl.Location = new Point(30, 90);
            txtUrl.Name = "txtUrl";
            txtUrl.PlaceholderText = "Enter GitHub repository URL (leave empty for local folder)";
            txtUrl.Size = new Size(740, 25);
            txtUrl.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.FromArgb(46, 204, 113);
            btnStart.Cursor = Cursors.Hand;
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            btnStart.ForeColor = Color.White;
            btnStart.Location = new Point(290, 200);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(220, 50);
            btnStart.TabIndex = 1;
            btnStart.Text = "🚀 Generate Documentation";
            btnStart.UseVisualStyleBackColor = false;
            // 
            // lstLog
            // 
            lstLog.BackColor = Color.FromArgb(52, 73, 94);
            lstLog.BorderStyle = BorderStyle.None;
            lstLog.Font = new Font("Consolas", 9F);
            lstLog.ForeColor = Color.FromArgb(149, 165, 166);
            lstLog.FormattingEnabled = true;
            lstLog.ItemHeight = 18;
            lstLog.Location = new Point(15, 15);
            lstLog.Name = "lstLog";
            lstLog.Size = new Size(740, 216);
            lstLog.TabIndex = 2;
            // 
            // btnBrowse
            // 
            btnBrowse.BackColor = Color.FromArgb(52, 152, 219);
            btnBrowse.Cursor = Cursors.Hand;
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnBrowse.ForeColor = Color.White;
            btnBrowse.Location = new Point(640, 135);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(130, 35);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "📁 Browse";
            btnBrowse.UseVisualStyleBackColor = false;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // lblSelectedPath
            // 
            lblSelectedPath.BackColor = Color.FromArgb(52, 73, 94);
            lblSelectedPath.BorderStyle = BorderStyle.None;
            lblSelectedPath.Font = new Font("Segoe UI", 10F);
            lblSelectedPath.ForeColor = Color.FromArgb(149, 165, 166);
            lblSelectedPath.Location = new Point(30, 140);
            lblSelectedPath.Name = "lblSelectedPath";
            lblSelectedPath.ReadOnly = true;
            lblSelectedPath.Size = new Size(600, 23);
            lblSelectedPath.TabIndex = 4;
            // 
            // progressBar1
            // 
            progressBar1.BackColor = Color.FromArgb(52, 73, 94);
            progressBar1.ForeColor = Color.FromArgb(46, 204, 113);
            progressBar1.Location = new Point(30, 260);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(740, 8);
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.TabIndex = 5;
            progressBar1.Visible = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblStatus.ForeColor = Color.FromArgb(149, 165, 166);
            lblStatus.Location = new Point(30, 275);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 20);
            lblStatus.TabIndex = 6;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(25, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(368, 41);
            lblTitle.TabIndex = 7;
            lblTitle.Text = "🤖 AI Code Documenter";
            // 
            // lblGitHubUrl
            // 
            lblGitHubUrl.AutoSize = true;
            lblGitHubUrl.Font = new Font("Seg oe UI", 9F, FontStyle.Bold);
            lblGitHubUrl.ForeColor = Color.White;
            lblGitHubUrl.Location = new Point(30, 65);
            lblGitHubUrl.Name = "lblGitHubUrl";
            lblGitHubUrl.Size = new Size(173, 20);
            lblGitHubUrl.TabIndex = 8;
            lblGitHubUrl.Text = "GitHub Repository URL:";
            // 
            // lblLocalPath
            // 
            lblLocalPath.AutoSize = true;
            lblLocalPath.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblLocalPath.ForeColor = Color.White;
            lblLocalPath.Location = new Point(30, 120);
            lblLocalPath.Name = "lblLocalPath";
            lblLocalPath.Size = new Size(189, 20);
            lblLocalPath.TabIndex = 9;
            lblLocalPath.Text = "Output / Local Folder Path:";
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(44, 62, 80);
            panel1.Controls.Add(lblTitle);
            panel1.Controls.Add(lblLocalPath);
            panel1.Controls.Add(lblGitHubUrl);
            panel1.Controls.Add(txtUrl);
            panel1.Controls.Add(btnBrowse);
            panel1.Controls.Add(lblSelectedPath);
            panel1.Controls.Add(btnStart);
            panel1.Controls.Add(lblStatus);
            panel1.Controls.Add(progressBar1);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 310);
            panel1.TabIndex = 10;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(44, 62, 80);
            panel2.Controls.Add(lstLog);
            panel2.Location = new Point(0, 310);
            panel2.Name = "panel2";
            panel2.Size = new Size(800, 250);
            panel2.TabIndex = 11;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(34, 47, 62);
            ClientSize = new Size(800, 560);
            Controls.Add(panel2);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "AI Code Documenter v2.0";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtUrl;
        private Button btnStart;
        private ListBox lstLog;
        private Button btnBrowse;
        private TextBox lblSelectedPath;
        private ProgressBar progressBar1;
        private Label lblStatus;
        private Label lblTitle;
        private Label lblGitHubUrl;
        private Label lblLocalPath;
        private Panel panel1;
        private Panel panel2;
    }
}