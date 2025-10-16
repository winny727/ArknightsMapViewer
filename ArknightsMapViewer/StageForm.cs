using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;

namespace ArknightsMapViewer
{
    public partial class StageForm : Form
    {
        private WebClient downloadClient;
        private bool IsDownloading => downloadClient != null;
        private double lastSpeed = 0;
        private long lastBytes = 0;
        private DateTime lastTime = DateTime.Now;

        public StageForm()
        {
            InitializeComponent();
        }

        private void StageForm_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            UpdateStages();
            UpdateUIState();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (IsDownloading)
            {
                var result = MessageBox.Show("下载正在进行，是否取消并关闭窗口？",
                                             "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    downloadClient.CancelAsync();
                }
                else
                {
                    e.Cancel = true;
                }
            }

            base.OnFormClosing(e);
        }

        private void UpdateStages()
        {
            listBox1.Items.Clear();
            string searchText = textBox1.Text.ToLower();
            foreach (var item in GlobalDefine.StageInfo)
            {
                foreach (var stageInfo in item.Value)
                {
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        string stageId = stageInfo.stageId ?? "";
                        string levelId = stageInfo.levelId ?? "";
                        string zoneId = stageInfo.zoneId ?? "";
                        string name = stageInfo.name ?? "";
                        string code = stageInfo.code ?? "";
                        string description = stageInfo.description ?? "";

                        bool match = false;
                        match |= stageId.ToLower().Contains(searchText);
                        match |= levelId.ToLower().Contains(searchText);
                        match |= zoneId.ToLower().Contains(searchText);
                        match |= name.ToLower().Contains(searchText);
                        match |= $"[{code}]".ToLower().Contains(searchText);
                        match |= description.ToLower().Contains(searchText);

                        if (!match)
                        {
                            continue;
                        }
                    }
                    listBox1.Items.Add(stageInfo);
                }
            }
            UpdateStageInfo();
        }

        private void UpdateStageInfo()
        {
            StageInfo stageInfo = GetCurrentStageInfo();
            if (stageInfo != null)
            {
                label1.Text = StringHelper.GetObjFieldValueString(stageInfo);
            }
            else
            {
                label1.Text = "";
            }
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            StageInfo stageInfo = GetCurrentStageInfo();
            bool enabled = stageInfo != null && !string.IsNullOrEmpty(stageInfo.levelId);
            bool isDownloading = IsDownloading;

            button1.Enabled = !isDownloading && enabled;
            comboBox1.Enabled = !isDownloading && enabled;
            linkLabel1.Enabled = enabled;
            linkLabel2.Enabled = enabled;

            listBox1.Enabled = !isDownloading;
            linkLabel2.Text = isDownloading ? "取消" : "下载";
            toolStripProgressBar1.Visible = isDownloading;
            toolStripStatusLabel1.Visible = isDownloading;
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

        private string GetCurrentStageUrl(bool rawFile)
        {
            string url = "";
            StageInfo stageInfo = GetCurrentStageInfo();
            if (stageInfo == null)
            {
                return url;
            }

            string stagePath = stageInfo.levelId.ToLower() + ".json";
            if (comboBox1.SelectedIndex == 0)
            {
                if (rawFile)
                {
                    url = $"https://map.ark-nights.com/data/levels/{stagePath}";
                }
                else
                {
                    url = $"https://map.ark-nights.com/map/{stageInfo.stageId.ToLower()}"; //有些关卡可能没有
                }
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                if (rawFile)
                {
                    url = $"https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/levels/{stagePath}";
                }
                else
                {
                    url = $"https://github.com/Kengxxiao/ArknightsGameData/tree/master/zh_CN/gamedata/levels/{stagePath}";
                }
            }

            return url;
        }

        private void CancelDownload()
        {
            if (IsDownloading)
            {
                downloadClient.CancelAsync();
            }
        }

        private async void DownloadLevelFile()
        {
            if (IsDownloading)
            {
                MessageBox.Show("已有下载任务在进行中，请稍后再试", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            StageInfo stageInfo = GetCurrentStageInfo();
            if (stageInfo == null)
            {
                return;
            }

            string stagePath = stageInfo.levelId.ToLower() + ".json";
            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/gamedata/levels", stagePath);
            string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Temp/{Path.GetFileName(savePath)}.{Guid.NewGuid()}.tmp"); // 临时文件路径

            if (File.Exists(savePath))
            {
                if (MessageBox.Show($"本地已存在文件:\n{savePath}\n是否覆盖下载?", "文件已存在", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            // 确保目标目录存在
            string tempDir = Path.GetDirectoryName(tempPath);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            string url = GetCurrentStageUrl(true);

            using var client = new WebClient();
            downloadClient = client;
            UpdateUIState();

            client.Encoding = Encoding.UTF8;

            lastSpeed = 0;
            lastBytes = 0;
            lastTime = DateTime.Now;

            toolStripStatusLabel1.Text = $"下载中... {0:F1} KB / {0:F1} KB ({0}%) | {0:F1} KB/s";
            toolStripProgressBar1.Value = 0;

            void DeleteTempFile()
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }

            client.DownloadProgressChanged += (s, e) =>
            {
                int percent = e.ProgressPercentage;

                double downloadedKB = e.BytesReceived / 1024.0;
                double totalKB = e.TotalBytesToReceive / 1024.0;

                var now = DateTime.Now;
                var timeDiff = (now - lastTime).TotalSeconds;
                if (timeDiff > 0.2) // 避免太频繁更新
                {
                    long bytesDiff = e.BytesReceived - lastBytes;
                    lastSpeed = bytesDiff / 1024.0 / timeDiff; // KB/s
                    lastBytes = e.BytesReceived;
                    lastTime = now;
                }

                toolStripStatusLabel1.Text = $"下载中... {downloadedKB:F1} KB / {totalKB:F1} KB ({percent}%) | {lastSpeed:F1} KB/s";
                toolStripProgressBar1.Value = percent;
            };

            client.DownloadFileCompleted += (s, e) =>
            {
                toolStripStatusLabel1.Text = "";
                toolStripProgressBar1.Value = 0;
                downloadClient = null;
                UpdateUIState();

                if (e.Error == null)
                {
                    try
                    {
                        string dir = Path.GetDirectoryName(savePath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.Copy(tempPath, savePath, true);
                        File.Delete(tempPath);

                        if (MessageBox.Show($"下载完成:\n{savePath}\n是否打开文件?", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            if (MainForm.Instance.ReadMapFile(savePath))
                            {
                                Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"移动文件失败:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (e.Cancelled)
                {
                    // 取消时删除临时文件
                    DeleteTempFile();
                }
                else
                {
                    DeleteTempFile();
                }
            };

            try
            {
                await client.DownloadFileTaskAsync(new Uri(url), tempPath);
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.RequestCanceled)
            {
                // 已取消，不显示错误
                downloadClient = null;
                UpdateUIState();
                DeleteTempFile();
            }
            catch (Exception ex)
            {
                downloadClient = null;
                UpdateUIState();
                DeleteTempFile();
                MessageBox.Show($"下载失败:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            string stagePath = stageInfo.levelId.ToLower() + ".json";
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/gamedata/levels", stagePath);

            if (File.Exists(path))
            {
                if (MainForm.Instance.ReadMapFile(path))
                {
                    Close();
                }
                return;
            }

            if (MessageBox.Show($"本地不存在文件:\n{path}\n是否下载?", "文件不存在", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DownloadLevelFile();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = GetCurrentStageUrl(false);
            if (!string.IsNullOrEmpty(url))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (IsDownloading)
            {
                CancelDownload();
            }
            else
            {
                DownloadLevelFile();
            }

        }
    }
}
