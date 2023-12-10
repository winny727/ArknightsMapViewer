using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using ArknightsMap;

namespace ArknightsMapViewer
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        private string log;

        private LevelView curLevelView;

        public MainForm()
        {
            InitializeComponent();
            Instance = this;
            log = "";
        }

        ~MainForm()
        {
            Instance = null;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Helper.InitTileColorConfig();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ReadMapFiles(files);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Confirm Exit?", "Continue?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                Application.Exit();
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateView();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(treeView1, e.Location);
            }
        }

        private void openToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                return;
            }

            TreeNode rootNode = treeView1.SelectedNode;
            while (rootNode.Level != 0)
            {
                rootNode = rootNode.Parent;
            }

            if (MessageBox.Show($"Confirm Remove [{rootNode.Name}]?", "Continue?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                rootNode.Remove();
                UpdateView();
            }
        }

        private void OpenFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Json Files|*.json";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                ReadMapFiles(dialog.FileNames);
            }
        }

        private void ReadMapFiles(string[] paths)
        {
            if (paths == null || paths.Length <= 0)
            {
                return;
            }

            foreach (string path in paths)
            {
                ReadMapFile(path);
            }
        }

        private void ReadMapFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string levelJson = File.ReadAllText(path);
                LevelReader levelReader = new LevelReader(levelJson);

                if (levelReader.IsValid)
                {
                    Log($"[{Path.GetFileName(path)}] Open Success");
                    AddRouteListToView(Path.GetFileNameWithoutExtension(path), levelReader.LevelData);
                }
                else
                {
                    string errorMsg;
                    if (!string.IsNullOrEmpty(levelReader.ErrorMsg))
                    {
                        errorMsg = $"[{Path.GetFileName(path)}] Parse Error, {levelReader.ErrorMsg}";
                    }
                    else
                    {
                        errorMsg = $"[{Path.GetFileName(path)}] Parse Error, Invalid Level File";
                    }
                    Log(errorMsg, LogType.Error);
                    MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"[{Path.GetFileName(path)}] Open Failed, {ex.Message}";
                Log(errorMsg, LogType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#if DEBUG
                MessageBox.Show(ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
            }
        }

        private void AddRouteListToView(string fileName, LevelData levelData)
        {
            if (treeView1.Nodes.ContainsKey(fileName))
            {
                if (MessageBox.Show($"[{fileName}] has been added, continue?", "Continue?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                {
                    return;
                }
            }

            TreeNode rootNode = treeView1.Nodes.Add(fileName, fileName);
            rootNode.Tag = new LevelView()
            {
                LevelData = levelData,
                MapDrawer = new WinformMapDrawer(pictureBox1, levelData.map),
            };

            int mapHeight = levelData.map.Length;
            int mapWidth = levelData.map.Length > 0 ? levelData.map[0].Length : 0;
            AStarPathFinding pathFinding = Helper.CreatePathFinding(levelData);

            void AddRouteList(string name, List<Route> routes)
            {
                if (routes == null || routes.Count <= 0)
                {
                    return;
                }

                TreeNode routesNode = rootNode.Nodes.Add(name);
                for (int i = 0; i < routes.Count; i++)
                {
                    Route route = routes[i];
                    TreeNode routeNode = routesNode.Nodes.Add($"routeIndex{i}");
                    routeNode.Tag = new RouteView()
                    {
                        Route = route,
                        RouteDrawer = new WinformRouteDrawer(pictureBox1, route, pathFinding, mapWidth, mapHeight),
                    };
                    routeNode.Nodes.Add($"{nameof(route.startPosition)}: {route.startPosition}");
                    for (int j = 0; j < route.checkPoints.Count; j++)
                    {
                        routeNode.Nodes.Add($"checkPoint{j}: {route.checkPoints[j]}");
                    }
                    routeNode.Nodes.Add($"{nameof(route.endPosition)}: {route.endPosition}");
                }
                routesNode.Expand();
            }

            AddRouteList(nameof(levelData.routes), levelData.routes);
            AddRouteList(nameof(levelData.extraRoutes), levelData.extraRoutes);

            rootNode.Expand();
            rootNode.EnsureVisible();
            treeView1.SelectedNode = rootNode;
        }

        private void UpdateView()
        {
            TreeNode treeNode = treeView1.SelectedNode;
            LevelView levelView = null;
            RouteView routeView = null;
            int routeSubIndex = -1;
            while (treeNode != null)
            {
                if (levelView == null && treeNode.Level == 0)
                {
                    levelView = treeNode.Tag as LevelView;
                }
                if (routeView == null && treeNode.Level == 2)
                {
                    routeView = treeNode.Tag as RouteView;
                }
                if (routeSubIndex < 0 && treeNode.Level == 3 && treeNode.Parent.Tag is RouteView)
                {
                    routeSubIndex = treeNode.Index;
                }
                treeNode = treeNode.Parent;
            }

            if (curLevelView != levelView)
            {
                curLevelView = levelView;
                pictureBox1.BackgroundImage?.Dispose();
                pictureBox1.BackgroundImage = null;
                levelView?.MapDrawer?.DrawMap();
            }

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;
            if (routeView != null)
            {
                if (routeSubIndex < 0)
                {
                    routeView.RouteDrawer?.DrawRoute();
                }
                else
                {
                    routeView.RouteDrawer?.InitCanvas();
                    for (int i = 0; i <= routeSubIndex; i++)
                    {
                        int checkPointIndex = i - 1;
                        routeView.RouteDrawer?.DrawMoveLine(checkPointIndex);
                    }
                    for (int i = 0; i <= routeSubIndex; i++)
                    {
                        int checkPointIndex = i - 1;
                        routeView.RouteDrawer?.DrawCheckPoint(checkPointIndex);
                    }
                    routeView.RouteDrawer?.RefreshCanvas();
                }
            }
        }

        public enum LogType
        {
            Log,
            Warning,
            Error,
        }

        public void Log(object obj, LogType logType = LogType.Log)
        {
            string content = obj.ToString();
            toolStripStatusLabel1.Text = content;
            log += $"[{logType.ToString().ToUpper()}][{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ff")}] {content} \n";
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(log);
            LogForm logForm = new LogForm();
            logForm.ShowLog(log);
        }
    }
}
