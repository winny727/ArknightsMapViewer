using System;
using System.Collections.Generic;
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
            richTextBox1.Text = log;
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }
    }
}
