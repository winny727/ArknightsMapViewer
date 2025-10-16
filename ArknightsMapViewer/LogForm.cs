using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
        }

        public void UpdateLog(string log)
        {
            richTextBox1.Clear(); // 清空旧内容，避免重复

            string[] lines = log.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                // 判断日志等级
                if (line.Contains("[ERROR]"))
                {
                    AppendColoredText(line + "\n", Color.Red);
                }
                else if (line.Contains("[WARNING]"))
                {
                    AppendColoredText(line + "\n", Color.Orange);
                }
                else
                {
                    AppendColoredText(line + "\n", Color.Black); // 或默认颜色
                }
            }

            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }

        private void AppendColoredText(string text, Color color)
        {
            int start = richTextBox1.TextLength;
            richTextBox1.AppendText(text);
            int end = richTextBox1.TextLength;

            richTextBox1.Select(start, end - start);
            richTextBox1.SelectionColor = color;
            richTextBox1.SelectionLength = 0; // 取消选中
        }
    }
}
