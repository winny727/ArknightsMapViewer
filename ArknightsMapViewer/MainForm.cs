using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using ArknightsMap;

namespace ArknightsMapViewer
{
    public partial class MainForm : Form
    {
        private LevelViewData curLevelViewData;

        public MainForm()
        {
            InitializeComponent();
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
                    MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"[{Path.GetFileName(path)}] Open Failed, {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            rootNode.Tag = new LevelViewData()
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
                    routeNode.Tag = new RouteViewData()
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
            LevelViewData levelViewData = null;
            RouteViewData routeViewData = null;
            int routeSubIndex = -1;
            while (treeNode != null)
            {
                if (levelViewData == null && treeNode.Level == 0)
                {
                    levelViewData = treeNode.Tag as LevelViewData;
                }
                if (routeViewData == null && treeNode.Level == 2)
                {
                    routeViewData = treeNode.Tag as RouteViewData;
                }
                if (routeSubIndex < 0 && treeNode.Level == 3 && treeNode.Parent.Tag is RouteViewData)
                {
                    routeSubIndex = treeNode.Index;
                }
                treeNode = treeNode.Parent;
            }

            if (curLevelViewData != levelViewData)
            {
                curLevelViewData = levelViewData;
                pictureBox1.BackgroundImage?.Dispose();
                pictureBox1.BackgroundImage = null;
                levelViewData?.MapDrawer?.DrawMap();
            }

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;
            if (routeViewData != null)
            {
                if (routeSubIndex < 0)
                {
                    routeViewData.RouteDrawer?.DrawRoute();
                }
                else
                {
                    routeViewData.RouteDrawer?.InitCanvas();
                    for (int i = 0; i <= routeSubIndex; i++)
                    {
                        int checkPointIndex = i - 1;
                        routeViewData.RouteDrawer?.DrawMoveLine(checkPointIndex);
                    }
                    for (int i = 0; i <= routeSubIndex; i++)
                    {
                        int checkPointIndex = i - 1;
                        routeViewData.RouteDrawer?.DrawCheckPoint(checkPointIndex);
                    }
                    routeViewData.RouteDrawer?.RefreshCanvas();
                }
            }
        }
    }
}
