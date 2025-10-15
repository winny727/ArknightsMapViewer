using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace ArknightsMapViewer
{
    public partial class StageForm : Form
    {
        public StageForm()
        {
            InitializeComponent();
        }

        private void StageForm_Load(object sender, EventArgs e)
        {
            UpdateStages();
        }

        private void UpdateStages()
        {
            listBox1.Items.Clear();
            string searchText = textBox1.Text.ToLower();
            foreach (var item in GlobalDefine.StageTable)
            {
                StageInfo stageInfo = item.Value;
                if (!string.IsNullOrEmpty(searchText))
                {
                    string stageId = stageInfo.stageId ?? "";
                    string name = stageInfo.name ?? "";
                    string code = stageInfo.code ?? "";

                    bool match = false;
                    match |= stageId.ToLower().Contains(searchText);
                    match |= name.ToLower().Contains(searchText);
                    match |= $"[{code}]".ToLower().Contains(searchText);

                    if (!match)
                    {
                        continue;
                    }
                }

                listBox1.Items.Add(stageInfo);
            }
            UpdateStageInfo();
        }

        private void UpdateStageInfo()
        {
            StageInfo stageInfo = GetCurrentStageInfo();
            if (stageInfo != null)
            {
                label1.Text = StringHelper.GetObjFieldValueString(stageInfo);
                button1.Enabled = !string.IsNullOrEmpty(stageInfo.levelId);
            }
            else
            {
                label1.Text = "";
                button1.Enabled = false;
            }
        }

        private StageInfo GetCurrentStageInfo()
        {
            int index = listBox1.SelectedIndex;
            if (index >= 0 && index < listBox1.Items.Count && listBox1.Items[index] is StageInfo stageInfo)
            {
                return stageInfo;
            }
            return null;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdateStages();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStageInfo();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StageInfo stageInfo = GetCurrentStageInfo();
            if (stageInfo == null)
            {
                return;
            }

            string stagePath = stageInfo.levelId.ToLower();
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Download", stagePath + ".json");

            if (File.Exists(path))
            {
                if (MainForm.Instance.ReadMapFile(path))
                {
                    Close();
                    return;
                }
            }

            //TODO: Download and open
            string url = $"https://map.ark-nights.com/data/levels/{stagePath}.json";
        }
    }
}
