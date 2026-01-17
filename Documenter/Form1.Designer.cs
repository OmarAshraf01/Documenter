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
            SuspendLayout();
            // 
            // txtUrl
            // 
            txtUrl.Location = new Point(12, 37);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(452, 27);
            txtUrl.TabIndex = 0;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(157, 121);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(150, 40);
            btnStart.TabIndex = 1;
            btnStart.Text = "Generate Docs";
            btnStart.UseVisualStyleBackColor = true;
            // 
            // lstLog
            // 
            lstLog.FormattingEnabled = true;
            lstLog.Location = new Point(12, 180);
            lstLog.Name = "lstLog";
            lstLog.Size = new Size(700, 224);
            lstLog.TabIndex = 2;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(471, 71);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(94, 29);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "Browse";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // lblSelectedPath
            // 
            lblSelectedPath.Location = new Point(13, 73);
            lblSelectedPath.Name = "lblSelectedPath";
            lblSelectedPath.Size = new Size(452, 27);
            lblSelectedPath.TabIndex = 4;
            // 
            // progressBar1
            // 
            progressBar1.BackColor = SystemColors.Menu;
            progressBar1.Location = new Point(471, 106);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(81, 25);
            progressBar1.TabIndex = 5;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(471, 141);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(0, 20);
            lblStatus.TabIndex = 6;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Menu;
            ClientSize = new Size(743, 450);
            Controls.Add(lblStatus);
            Controls.Add(progressBar1);
            Controls.Add(lblSelectedPath);
            Controls.Add(btnBrowse);
            Controls.Add(lstLog);
            Controls.Add(btnStart);
            Controls.Add(txtUrl);
            Name = "Form1";
            Text = "AI Documenter";
            this.Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtUrl;
        private Button btnStart;
        private ListBox lstLog;
        private Button btnBrowse;
        private TextBox lblSelectedPath;
        private ProgressBar progressBar1;
        private Label lblStatus;
    }
}