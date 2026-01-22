using System;
using System.Drawing;
using System.Windows.Forms;

namespace Documenter
{
    public class RealTimeView : Form
    {
        private RichTextBox _outputBox;

        public RealTimeView()
        {
            this.Text = "🔴 AI Real-Time Generation Stream";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30); // Dark Background

            _outputBox = new RichTextBox();
            _outputBox.Dock = DockStyle.Fill;
            _outputBox.BackColor = Color.FromArgb(30, 30, 30);
            _outputBox.ForeColor = Color.FromArgb(0, 255, 0); // Hacker Green Text
            _outputBox.Font = new Font("Consolas", 10f);
            _outputBox.ReadOnly = true;
            _outputBox.BorderStyle = BorderStyle.None;
            _outputBox.ScrollBars = RichTextBoxScrollBars.Vertical;

            this.Controls.Add(_outputBox);
        }

        public void AppendLog(string fileName, string content)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string>(AppendLog), fileName, content);
                return;
            }

            _outputBox.SelectionColor = Color.Yellow;
            _outputBox.AppendText($"\n\n>>> COMPLETED ANALYSIS: {fileName} [{DateTime.Now:HH:mm:ss}]\n");
            _outputBox.AppendText("--------------------------------------------------\n");

            _outputBox.SelectionColor = Color.LightGreen;
            _outputBox.AppendText(content);

            _outputBox.SelectionStart = _outputBox.Text.Length;
            _outputBox.ScrollToCaret();
        }
    }
}