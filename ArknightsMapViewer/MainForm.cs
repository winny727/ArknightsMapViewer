using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        public string LogText { get; private set; }
        private Dictionary<Control, RectangleF> controlsBoundScaled = new Dictionary<Control, RectangleF>();

        private LevelView curLevelView;
        private Bitmap drawingImage;
        private bool needUpdateMap;
        private bool readingMultiFiles;
        private bool rawSetCheckBox;

        private Dictionary<Position, List<IMapData>> mapPredefines = new Dictionary<Position, List<IMapData>>();

        private TimelineSimulator curTimelineSimulator;
        private bool isSimulationEnabled;
        private bool rawSetValue;

        private const string lastBuildDate = "20251021";

        public MainForm()
        {
            Instance = this;
            InitializeComponent();
            InitControlsBoundsScale();
        }

        ~MainForm()
        {
            Instance = null;
        }

        private void InitControlsBoundsScale()
        {
            controlsBoundScaled.Clear();
            Control[] controls = new Control[] { tabControl1, flowLayoutPanel3, groupBox1, groupBox2 }; //要自动调整比例的控件

            foreach (Control control in controls)
            {
                float x = (float)control.Left / ClientSize.Width;
                float y = (float)control.Top / ClientSize.Height;
                float width = (float)control.Width / ClientSize.Width;
                float height = (float)control.Height / ClientSize.Height;
                RectangleF rect = new RectangleF(x, y, width, height);
                controlsBoundScaled.Add(control, rect);
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            foreach (Control control in Controls)
            {
                if (controlsBoundScaled.TryGetValue(control, out RectangleF rect))
                {
                    control.Left = (int)(rect.X * ClientSize.Width);
                    control.Top = (int)(rect.Y * ClientSize.Height);
                    control.Width = (int)(rect.Width * ClientSize.Width);
                    control.Height = (int)(rect.Height * ClientSize.Height);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LogText = "";

            Stopwatch stopwatch = Stopwatch.StartNew();
            Helper.InitDrawConfig();
            Helper.InitTileInfoConfig();
            Helper.InitEnemyDatabase();
            Helper.InitCharacterTable();
            Helper.InitStageTable();
            Helper.ClearGameConfigTableCache();
            stopwatch.Stop();
            Log($"Init All Config Completed, Total Time: {stopwatch.Elapsed.TotalMilliseconds} ms");

            UpdateView();
            UpdateTimelineSimulationState();

            string[] latestFilePath = Helper.LoadLatestFilePath();
            ReadMapFiles(latestFilePath);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            List<string> latestFilePath = new List<string>();
            foreach (TreeNode child in treeView1.Nodes)
            {
                if (child.Tag is LevelView levelView)
                {
                    latestFilePath.Add(levelView.Path);
                }
            }
            Helper.SaveLatestFilePath(latestFilePath);
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
            curLevelView?.SpawnView?.UpdateRandomSpawnGroupNodes(treeView1.SelectedNode);
            UpdateView();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
            e.Node.ContextMenuStrip = contextMenuStrip1;
        }

        private void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    removeToolStripMenuItem_Click(sender, e);
                    e.Handled = true;
                    break;
                default:
                    break;
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

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"Confirm Remove All?", "Continue?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                treeView1.Nodes.Clear();
                UpdateView();
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            ShowLog();
            toolStripStatusLabel1.Text = "";
        }

        private void showLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void informationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule.FileName);
            if (fileVersionInfo == null)
            {
                return;
            }

            MessageBox.Show(
                $"ProductName: {fileVersionInfo.ProductName} ({fileVersionInfo.Comments})\n" + 
                $"Version: {fileVersionInfo.ProductVersion} ({lastBuildDate})\n" +
                $"Author: {fileVersionInfo.LegalCopyright} ({fileVersionInfo.CompanyName})"
                );
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

        internal bool ReadMapFiles(string[] paths)
        {
            if (paths == null || paths.Length <= 0)
            {
                return false;
            }

            if (paths.Length > 1)
            {
                readingMultiFiles = true;
            }

            bool result = true;
            foreach (string path in paths)
            {
                if (!ReadMapFile(path))
                {
                    result = false;
                }
            }

            if (readingMultiFiles)
            {
                readingMultiFiles = false;
                UpdateView();
            }

            return result;
        }

        internal bool ReadMapFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                if (!File.Exists(path))
                {
                    if (Directory.Exists(path))
                    {
                        string[] files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
                        return ReadMapFiles(files);
                    }

                    Log($"Open File Failed, File does not exist: {path}", LogType.Error);
                    return false;
                }

                if (!path.ToLower().EndsWith(".json") || (!File.Exists(path) && Directory.Exists(path)))
                {
                    Log($"File Type Error, Request Json Files(*.json), {path}", LogType.Error);
                    return false;
                }

                string levelJson = File.ReadAllText(path);
                LevelReader levelReader = new LevelReader(levelJson);

                if (levelReader.IsValid)
                {
                    AddLevelDataToView(path, levelReader.LevelData);
                    stopwatch.Stop();
                    Log($"[{Path.GetFileName(path)}] Open Success ({stopwatch.Elapsed.TotalMilliseconds} ms)");
                    return true;
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
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"[{Path.GetFileName(path)}] Open Failed, {ex.Message}";
                Log(errorMsg, LogType.Error);
#if DEBUG
                Log(ex.StackTrace, LogType.Debug);
#endif
                return false;
            }
        }

        private void AddLevelDataToView(string path, LevelData levelData)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (treeView1.Nodes.ContainsKey(fileName))
            {
                if (MessageBox.Show($"[{fileName}] has already been added, add another one?", "Continue?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                {
                    return;
                }
            }

            TreeNode rootNode = treeView1.Nodes.Add(fileName, fileName);

            int mapWidth = levelData.mapWidth;
            int mapHeight = levelData.mapHeight;
            PathFinding pathFinding = new AStarPathFinding();
            //PathFinding pathFinding = new DijkstraPathFinding();
            pathFinding.SetGridArray(Helper.GetGridArray(levelData));

            //TODO SPFA

            RouteDrawer routeDrawerCreater(Route route) => RouteDrawer.Create(route, pathFinding, mapWidth, mapHeight);
            PredefineDrawer predefineDrawerCreater(Predefine.PredefineInst predefine) => PredefineDrawer.Create(predefine, mapWidth, mapHeight);

            TreeNode routesNode = LevelViewHelper.CreateRoutesNode(nameof(levelData.routes), levelData.routes, routeDrawerCreater);
            TreeNode extraRoutesNode = LevelViewHelper.CreateRoutesNode(nameof(levelData.extraRoutes), levelData.extraRoutes, routeDrawerCreater);

            TreeNode wavesNode = LevelViewHelper.CreateWavesNode(levelData, routeDrawerCreater, predefineDrawerCreater,
                out List<ISpawnAction> spawnActions, out List<PredefineView> predefineViews, out Dictionary<string, int> totalWeightDict);

            TreeNode branchesNode = LevelViewHelper.CreateBranchsNode(nameof(levelData.branches), levelData.branches, levelData.extraRoutes, routeDrawerCreater);

            PredefineView getPredefineView(string predefineKey) => predefineViews.Find(x => predefineKey == x.PredefineKey);
            TreeNode predefinesNode = LevelViewHelper.CreatePredefinesNode(nameof(levelData.predefines), levelData.predefines, getPredefineView, predefineDrawerCreater);

            TreeNode spawnsNode = LevelViewHelper.CreateSpawnsNode("spawns", spawnActions, totalWeightDict);
            //spawnsNode.ForeColor = Color.Red;

            SpawnView spawnView = spawnsNode?.Tag as SpawnView;
            TreeNode groupsNode = LevelViewHelper.CreateGroupsNode("groups", spawnView);

            TimelineSimulator timelineSimulator = new TimelineSimulator(levelData);

            rootNode.Tag = new LevelView()
            {
                Path = path,
                Name = fileName,
                LevelData = levelData,
                SpawnView = spawnView,
                TimelineSimulator = timelineSimulator,
                MapDrawer = MapDrawer.Create(levelData.map),
            };

            void AddTreeNode(TreeNode treeNode)
            {
                if (treeNode != null) rootNode.Nodes.Add(treeNode);
            }

            AddTreeNode(routesNode);
            AddTreeNode(extraRoutesNode);
            AddTreeNode(wavesNode);
            AddTreeNode(branchesNode);
            AddTreeNode(predefinesNode);
            AddTreeNode(spawnsNode);
            AddTreeNode(groupsNode);

            if (!readingMultiFiles)
            {
                rootNode.Expand();
                treeView1.SelectedNode = rootNode;
            }

            spawnsNode.Expand();
            rootNode.EnsureVisible();
        }

        private void UpdateView()
        {
            if (readingMultiFiles)
            {
                return;
            }

            LevelView levelView = null;
            SpawnActionView spawnActionView = null;
            EnemySpawnView enemySpawnView = null;
            int routeSubIndex = -1;

            IDrawerView<RouteDrawer> routeDrawerView = null;
            IDrawerView<PredefineDrawer> predefineDrawerView = null;

            TreeNode predefineRootNode = null;

            TreeNode treeNode = treeView1.SelectedNode;
            while (treeNode != null)
            {
                if (treeNode.Level == 0)
                {
                    levelView ??= treeNode.Tag as LevelView;
                    foreach (TreeNode subRoot in treeNode.Nodes)
                    {
                        if (subRoot.Text == "predefines")
                        {
                            predefineRootNode = subRoot;
                        }
                    }
                }
                else
                {
                    spawnActionView ??= treeNode.Tag as SpawnActionView;
                    enemySpawnView ??= treeNode.Tag as EnemySpawnView;
                    routeDrawerView ??= treeNode.Tag as IDrawerView<RouteDrawer>;
                    predefineDrawerView ??= treeNode.Tag as IDrawerView<PredefineDrawer>;

                    if (routeSubIndex < 0 && (treeNode.Parent.Tag is IMapDataView<Route>))
                    {
                        routeSubIndex = treeNode.Index;
                    }
                }

                treeNode = treeNode.Parent;
            }

            if (curLevelView != levelView || needUpdateMap)
            {
                curLevelView = levelView;
                pictureBox1.BackgroundImage?.Dispose();
                pictureBox1.BackgroundImage = null;
                needUpdateMap = false;

                if (curLevelView != null && curLevelView.MapDrawer != null)
                {
                    Bitmap bitmap = Helper.CreateBitmap(curLevelView.LevelData);

                    curLevelView.MapDrawer.MarkUndefinedTile = checkBox1.Checked;
                    curLevelView.MapDrawer.Draw(bitmap);
                    pictureBox1.Width = bitmap.Width;
                    pictureBox1.Height = bitmap.Height;
                    pictureBox1.BackgroundImage = bitmap;
                }

                UpdateSpawnGroups();
                UpdateTimelineSimulationState();
            }

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;

            if (curLevelView != null)
            {
                Bitmap bitmap = Helper.CreateBitmap(curLevelView.LevelData);
                drawingImage = bitmap;

                UpdatePredefine(bitmap, predefineRootNode, predefineDrawerView);
                UpdateRoute(bitmap, routeDrawerView, routeSubIndex);
                pictureBox1.Image = bitmap;

                if (curLevelView.MapDrawer != null)
                {
                    checkBox1.Visible = curLevelView.MapDrawer.HaveUndefinedTiles;
                }
                checkBox6.Visible = enemySpawnView != null;
            }
            else
            {
                checkBox1.Visible = false;
                checkBox2.Visible = false;
                checkBox3.Visible = false;
                checkBox4.Visible = false;
                checkBox5.Visible = false;
                checkBox6.Visible = false;
                checkBox7.Visible = false;
                checkBox8.Visible = false;
            }

            UpdateLabelInfo(routeSubIndex);
            FilterTreeNode();

            pictureBox1.Refresh();
        }

        private void UpdatePredefine(Bitmap bitmap, TreeNode predefineRootNode, IDrawerView<PredefineDrawer> predefineDrawerView)
        {
            bool showCheckBox3 = false;

            foreach (var item in mapPredefines)
            {
                item.Value.Clear();
            }

            if (predefineRootNode != null && checkBox2.Checked)
            {
                foreach (TreeNode predefineNode in predefineRootNode.Nodes)
                {
                    if (predefineNode.Tag is PredefineView predefineView && predefineView.Predefine != null)
                    {
                        if (predefineView.Predefine.hidden)
                        {
                            showCheckBox3 = true;
                        }

                        bool isShow = !predefineView.Predefine.hidden;

                        if (checkBox3.Checked && !isShow)
                        {
                            if (curLevelView?.SpawnView != null)
                            {
                                foreach (var item in curLevelView.SpawnView.ValidSpawnNodes)
                                {
                                    if (item.Key.Tag is PredefineView validPredefineView && validPredefineView.PredefineKey == predefineView.PredefineKey)
                                    {
                                        isShow = item.Value;
                                    }
                                }
                            }
                            else
                            {
                                isShow = true;
                            }
                        }

                        if (isShow && predefineView.PredefineDrawer != null)
                        {
                            predefineView.PredefineDrawer.IsSelected = false;
                            predefineView.PredefineDrawer.Draw(bitmap);

                            if (!mapPredefines.ContainsKey(predefineView.Predefine.position))
                            {
                                mapPredefines.Add(predefineView.Predefine.position, new List<IMapData>());
                            }
                            mapPredefines[predefineView.Predefine.position].Add(predefineView);
                        }
                    }
                }
            }

            PredefineDrawer predefineDrawer = predefineDrawerView?.GetDrawer();
            if (predefineDrawer != null && predefineDrawer.Predefine != null)
            {
                UpdateSimulationState(false);

                predefineDrawer.IsSelected = checkBox2.Checked;
                predefineDrawer.Draw(bitmap);

                if (!mapPredefines.ContainsKey(predefineDrawer.Predefine.position))
                {
                    mapPredefines.Add(predefineDrawer.Predefine.position, new List<IMapData>());
                }

                IMapData mapData = (IMapData)(predefineDrawerView as PredefineView) ?? (IMapData)(predefineDrawerView as PredefineActionView) ?? (IMapData)(predefineDrawer.Predefine);
                if (mapData != null)
                {
                    mapPredefines[predefineDrawer.Predefine.position].Add(mapData);
                }
            }

            checkBox2.Visible = predefineRootNode != null && predefineRootNode.Nodes.Count > 0;
            checkBox3.Visible = showCheckBox3;
        }

        private void UpdateRoute(Bitmap bitmap, IDrawerView<RouteDrawer> routeDrawerView, int routeSubIndex)
        {
            bool showCheckBox4 = false;
            bool showCheckBox5 = false;

            RouteDrawer routeDrawer = routeDrawerView?.GetDrawer();
            if (routeDrawer != null)
            {
                UpdateSimulationState(false);

                showCheckBox4 = true;
                routeDrawer.ShowRouteLength = checkBox4.Checked;
                if (routeSubIndex < 0)
                {
                    routeDrawer.Draw(bitmap);
                    //MoveRoute moveRoute = new MoveRoute(routeDrawer.Route, routeDrawer.PathFinding);
                }
                else
                {
                    showCheckBox5 = true;
                    int checkPointIndex = routeSubIndex - 1;
                    int startIndex = checkBox5.Checked ? checkPointIndex : -1; //-1表示从startPosition开始

                    for (int i = startIndex; i <= checkPointIndex; i++)
                    {
                        routeDrawer.DrawMoveLine(bitmap, i);
                    }
                    for (int i = startIndex; i <= checkPointIndex; i++)
                    {
                        routeDrawer.DrawCheckPoint(bitmap, i);
                    }
                }
            }

            checkBox4.Visible = showCheckBox4;
            checkBox5.Visible = showCheckBox5;
        }

        private void UpdateLabelInfo(int routeSubIndex)
        {
            string title = "";
            StringBuilder stringBuilder = new StringBuilder();

            TreeNode treeNode = treeView1.SelectedNode;
            while (treeNode != null)
            {
                if (treeNode.Tag is IMapDataView<Route> routeDataView)
                {
                    Route route = routeDataView?.GetData();
                    title = $"[{treeView1.SelectedNode.Text}]";
                    if (routeSubIndex < 0)
                    {
                        if (routeDataView is EnemySpawnView enemySpawnView)
                        {
                            stringBuilder.AppendLine(enemySpawnView.ToString(checkBox6.Checked));
                            break;

                        }
                        else if (route != null)
                        {
                            stringBuilder.AppendLine(route.ToString());
                            break;
                        }
                    }
                    else
                    {
                        int checkPointIndex = routeSubIndex - 1;
                        if (checkPointIndex < 0)
                        {
                            stringBuilder.AppendLine($"startPosition: {route.startPosition}");
                            stringBuilder.AppendLine($"spawnOffset: {route.spawnOffset}");
                            stringBuilder.AppendLine($"spawnRandomRange: {route.spawnRandomRange}");
                        }
                        else if (checkPointIndex < route.checkPoints.Count)
                        {
                            CheckPoint checkPoint = route.checkPoints[checkPointIndex];
                            stringBuilder.AppendLine($"checkPoint #{checkPointIndex}");
                            stringBuilder.AppendLine(checkPoint.ToString());
                        }
                        else
                        {
                            stringBuilder.AppendLine($"endPosition: {route.endPosition}");
                        }
                        break;
                    }
                }

                IMapData mapData = treeNode.Tag as IMapData ?? (treeNode.Tag as ActionView)?.Action;
                if (mapData != null)
                {
                    stringBuilder.AppendLine($"[{treeView1.SelectedNode.Text}]");
                    string text = mapData.ToString();
                    if (mapData is SpawnActionView spawnActionView && spawnActionView.IsExtraRoute)
                    {
                        text = text.Replace("routeIndex", "routeIndex (extra)");
                    }
                    stringBuilder.AppendLine(text);
                    break;
                }

                treeNode = treeNode.Parent;
            }

            if (stringBuilder.Length <= 0 && curLevelView != null)
            {
                LevelData levelData = curLevelView.LevelData;
                stringBuilder.AppendLine($"[{curLevelView.Name}]");
                stringBuilder.AppendLine(levelData.ToString());
            }

            richTextBox1.ResetText();
            if (!string.IsNullOrEmpty(title))
            {
                richTextBox1.SelectionFont = new Font("微软雅黑", 9f);
                richTextBox1.AppendText(title + "\n"); //标题中可能有中文或者希腊字符，这里分开设置避免下面的文字也用默认字体显示
            }
            richTextBox1.SelectionFont = richTextBox1.Font;
            richTextBox1.AppendText(stringBuilder.ToString());
            richTextBox1.SelectionStart = 0;
            richTextBox1.ScrollToCaret();
        }

        private void UpdateSpawnGroups()
        {
            SpawnView spawnView = curLevelView?.SpawnView;
            flowLayoutPanel2.Controls.Clear();
            if (spawnView != null)
            {
                rawSetCheckBox = true;
                checkBox7.Checked = spawnView.ShowPredefinedNodes;
                checkBox8.Checked = spawnView.HideInvalidNodes;
                rawSetCheckBox = false;
                foreach (var item in spawnView.HiddenGroups)
                {
                    CheckBox checkBox = new CheckBox
                    {
                        Text = item.Key,
                        Checked = item.Value,
                        AutoSize = true,
                        TabIndex = flowLayoutPanel2.TabIndex + flowLayoutPanel2.Controls.Count + 1,
                    };
                    checkBox.CheckedChanged += (s, e) =>
                    {
                        spawnView.HiddenGroups[checkBox.Text] = checkBox.Checked;
                        spawnView.UpdateNodes();
                        UpdateView();
                    };
                    flowLayoutPanel2.Controls.Add(checkBox);
                }
                foreach (TreeNode treeNode in spawnView.SpawnNodesList)
                {
                    if (treeNode.Tag is PredefineView)
                    {
                        checkBox7.Visible = true;
                        break;
                    }
                }
                foreach (var item in spawnView.ValidSpawnNodes)
                {
                    if (!item.Value)
                    {
                        checkBox8.Visible = true;
                        break;
                    }
                }
            }
            else
            {
                checkBox8.Visible = false;
            }
        }

        private void FilterTreeNode()
        {
            void Filter(TreeNodeCollection treeNodes)
            {
                foreach (TreeNode treeNode in treeNodes)
                {
                    if (curLevelView?.SpawnView != null && curLevelView.SpawnView.ValidSpawnNodes.TryGetValue(treeNode, out bool isValid))
                    {
                        treeNode.ForeColor = isValid ? Color.FromKnownColor(KnownColor.WindowText) : Color.Gray;
                    }

                    if (!string.IsNullOrEmpty(textBox1.Text) && treeNode.Text.Contains(textBox1.Text))
                    {
                        treeNode.BackColor = Color.LightPink;
                    }
                    else
                    {
                        treeNode.BackColor = Color.FromKnownColor(KnownColor.Window);
                    }

                    Filter(treeNode.Nodes);
                }
            }

            //搜索框筛选
            Filter(treeView1.Nodes);
        }

        #region for debug

        public enum LogType
        {
            Log,
            Debug,
            Warning,
            Error,
        }

        public void Log(object obj, LogType logType = LogType.Log)
        {
            string msg = obj.ToString();
            if (logType != LogType.Debug)
            {
                toolStripStatusLabel1.Text = msg;
            }
            if (logType == LogType.Error)
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            LogText += $"[{logType.ToString().ToUpper()}][{DateTime.Now:yyyy-MM-dd HH:mm:ss:ff}] {msg} \n";
        }

        private void ShowLog()
        {
            //MessageBox.Show(log);
            LogForm logForm = new LogForm();
            logForm.UpdateLog(LogText);
            logForm.ShowDialog();
        }

        public void DebugDrawPoint(Vector2 position, Color color)
        {
            Bitmap bitmap = (Bitmap)pictureBox1.Image ?? drawingImage;
            if (bitmap != null)
            {
                DrawUtil.DrawPoint(bitmap, Helper.Vector2ToPoint(position, curLevelView.LevelData.mapHeight), color, 5);
            }
        }

        public void DebugDrawLine(Vector2 startPosition, Vector2 endPosition, Color color)
        {
            Bitmap bitmap = (Bitmap)pictureBox1.Image ?? drawingImage;
            if (bitmap != null)
            {
                Point startPoint = Helper.Vector2ToPoint(startPosition, curLevelView.LevelData.mapHeight);
                Point endPoint = Helper.Vector2ToPoint(endPosition, curLevelView.LevelData.mapHeight);
                DrawUtil.DrawLine(bitmap, startPoint, endPoint, color, 2);
            }
        }

        #endregion

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox1.BackgroundImage == null || curLevelView == null)
            {
                return;
            }

            //Point to row & col
            Point point = e.Location;
            Tile[,] map = curLevelView.LevelData.map;
            int mapWidth = curLevelView.LevelData.mapWidth;
            int mapHeight = curLevelView.LevelData.mapHeight;

            Position position = Helper.PointToPosition(point, mapHeight);
            position.col = position.col.Clamp(0, mapWidth - 1);
            position.row = position.row.Clamp(0, mapHeight - 1);
            Tile tile = map[position.col, position.row];

            string text = $"[Tile: {position}]\n" + tile.ToString();

            //predefine
            if (mapPredefines.TryGetValue(position, out List<IMapData> predefines) && predefines.Count > 0)
            {
                text += "\npredefines:\n";
                foreach (IMapData predefine in predefines)
                {
                    text += predefine.ToString() + "\n";
                }
            }

            richTextBox1.ResetText();
            richTextBox1.SelectionFont = richTextBox1.Font;
            richTextBox1.AppendText(text);
            richTextBox1.SelectionStart = 0;
            richTextBox1.ScrollToCaret();
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (curLevelView != null && curLevelView.MapDrawer != null)
            {
                curLevelView.MapDrawer.MarkUndefinedTile = checkBox1.Checked;
            }
            needUpdateMap = true;
            UpdateView();
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (curLevelView?.SpawnView != null && !rawSetCheckBox)
            {
                curLevelView.SpawnView.ShowPredefinedNodes = checkBox7.Checked;
                curLevelView.SpawnView.UpdateNodes();
                UpdateView();
                curLevelView.SpawnView.SpawnsNode?.EnsureVisible();
            }
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            if (curLevelView?.SpawnView != null && !rawSetCheckBox)
            {
                curLevelView.SpawnView.HideInvalidNodes = checkBox8.Checked;
                curLevelView.SpawnView.UpdateNodes();
                UpdateView();
                curLevelView.SpawnView.SpawnsNode?.EnsureVisible();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            FilterTreeNode();
        }

        private void fullImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImageToFile(saveFull: true);
        }

        private void routeOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImageToFile(saveRoute: true);
        }

        private void backgroundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImageToFile(saveBackground: true);
        }

        private void allToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveImageToFile(saveBackground: true, saveRoute: true, saveFull: true);
        }

        private void githubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/winny727/ArknightsMapViewer") { UseShellExecute = true });
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (curLevelView != null && !string.IsNullOrEmpty(curLevelView.Path))
            {
                Helper.SelectExplorerFile(curLevelView.Path);
            }
        }

        private void SaveImageToFile(bool saveBackground = false, bool saveRoute = false, bool saveFull = false)
        {
            if (!saveBackground && !saveRoute && !saveFull)
            {
                return;
            }

            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                return;
            }

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string path = folderBrowserDialog.SelectedPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            int saved = 0;
            bool backGroundSaved = false;
            void ForEachRouteNode(TreeNode treeNode)
            {
                if (backGroundSaved && !saveRoute && !saveFull)
                {
                    return;
                }

                int routeIndex = -1;
                if (treeNode.Tag is RouteView routeView)
                {
                    routeIndex = routeView.RouteIndex;
                }
                else if (treeNode.Tag is EnemySpawnView enemySpawnView)
                {
                    routeIndex = enemySpawnView.RouteIndex;
                }

                if (routeIndex >= 0)
                {
                    treeView1.SelectedNode = treeNode;
                    UpdateView();

                    if (saveBackground && !backGroundSaved && pictureBox1.BackgroundImage != null)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_Background.png");
                        pictureBox1.BackgroundImage.Save(fullPath, ImageFormat.Png);
                        saved++;
                        backGroundSaved = true;
                    }

                    if (saveRoute && pictureBox1.Image != null)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_{treeNode.Parent.Text}_R{treeNode.Index}.png");
                        pictureBox1.Image.Save(fullPath, ImageFormat.Png);
                        saved++;
                    }

                    if (saveFull)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_{treeNode.Parent.Text}_F{treeNode.Index}.png");
                        Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                        pictureBox1.DrawToBitmap(bitmap, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                        bitmap.Save(fullPath, ImageFormat.Png);
                        saved++;
                    }
                }

                foreach (TreeNode child in treeNode.Nodes)
                {
                    ForEachRouteNode(child);
                }
            }

            ForEachRouteNode(selectedNode);

            //还原
            treeView1.SelectedNode = selectedNode;
            UpdateView();

            if (saved > 0)
            {
                MessageBox.Show("Export Image Completed.");
                if (saved > 1)
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
        }

        private void UpdateTimelineSimulationState()
        {
            groupBox2.Enabled = curLevelView != null;
            if (curLevelView != null)
            {
                curTimelineSimulator = curLevelView.TimelineSimulator;

                rawSetValue = true;

                numericUpDown1.Value = 0;
                trackBar1.Value = 0;
                numericUpDown1.Maximum = (decimal)curTimelineSimulator.MaxTime;
                trackBar1.Maximum = (int)(curTimelineSimulator.MaxTime * 100f);
                numericUpDown1.Value = (decimal)curTimelineSimulator.Time;
                trackBar1.Value = (int)(curTimelineSimulator.Time * 100f);

                comboBox2.Items.Clear();
                for (int i = 0; i < curTimelineSimulator.WaveMaxTimes.Count; i++)
                {
                    comboBox2.Items.Add(i);
                }

                comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;
                comboBox2.SelectedIndex = comboBox2.Items.Count > 0 ? 0 : -1;

                rawSetValue = false;
            }
            else
            {
                UpdateSimulationState(false);
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rawSetValue) return;
            curTimelineSimulator?.SetWaveIndex(comboBox2.SelectedIndex);
            rawSetValue = true;
            numericUpDown1.Value = 0;
            trackBar1.Value = 0;
            numericUpDown1.Maximum = (decimal)curTimelineSimulator.MaxTime;
            trackBar1.Maximum = (int)(curTimelineSimulator.MaxTime * 100f);
            numericUpDown1.Value = (decimal)curTimelineSimulator.Time;
            trackBar1.Value = (int)(curTimelineSimulator.Time * 100f);
            rawSetValue = false;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (rawSetValue) return;
            rawSetValue = true;
            numericUpDown1.Value = trackBar1.Value / 100m;
            curTimelineSimulator?.UpdateTimeline((float)numericUpDown1.Value);
            rawSetValue = false;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (rawSetValue) return;
            rawSetValue = true;
            trackBar1.Value = (int)(numericUpDown1.Value * 100m);
            curTimelineSimulator?.UpdateTimeline((float)numericUpDown1.Value);
            rawSetValue = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateSimulationState(!isSimulationEnabled);
        }

        private void UpdateSimulationState(bool isEnabled)
        {
            isSimulationEnabled = isEnabled;
            button1.Text = isEnabled ? "Disable" : "Enable";

            if (isEnabled && treeView1.SelectedNode != null)
            {
                TreeNode rootNode = treeView1.SelectedNode;
                while (rootNode.Parent != null)
                {
                    rootNode = rootNode.Parent;
                }
                treeView1.SelectedNode = rootNode;
                curTimelineSimulator?.UpdateTimeline((float)numericUpDown1.Value);
            }
        }

        private void openStagesWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StageForm stageForm = new StageForm();
            stageForm.ShowDialog();
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            // 统一斜杠
            basePath = basePath.Replace('\\', '/');
            fullPath = fullPath.Replace('\\', '/');

            if (fullPath.StartsWith(basePath))
            {
                string relative = fullPath.Substring(basePath.Length);
                if (relative.StartsWith("/")) relative = relative.Substring(1);
                return relative;
            }
            return fullPath; // 不是 basePath 下的文件就返回原路径
        }

        private double lastSpeed = 0;
        private long lastBytes = 0;
        private DateTime lastTime = DateTime.Now;

        private async void updateGameDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/gamedata");
            List<string> fileList = new List<string>();

            if (Directory.Exists(baseDir))
            {
                // 获取excel中所有文件
                string[] files = Directory.GetFiles(baseDir + "/excel", "*.json", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string relativePath = GetRelativePath(baseDir, file).Replace("\\", "/");
                    fileList.Add(relativePath);
                }

                fileList.Add("levels/enemydata/enemy_database.json");
            }

            if (fileList.Count <= 0)
            {
                MessageBox.Show("没有找到游戏数据文件", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"当前共有{fileList.Count}个游戏数据文件，是否全部更新？更新完成后需要重新启动程序\n（下载源：Github/Kengxxiao/ArknightsGameData）", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }

            lastBytes = 0;
            lastTime = DateTime.Now;
            lastSpeed = 0;
            toolStripStatusLabel1.Text = $"更新游戏数据中... [{0} / {fileList.Count}] {0:F1} KB / {0:F1} KB ({0}%) | {0:F1} KB/s";
            updateGameDataToolStripMenuItem.Enabled = false;

            using var client = new WebClient();
            client.Encoding = Encoding.UTF8;

            int updateIndex = 0;
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

                if (updateIndex > 0 && updateIndex <= fileList.Count)
                {
                    string file = fileList[updateIndex - 1];
                    toolStripStatusLabel1.Text = $"更新游戏数据中... [{updateIndex}/{fileList.Count}] {file} {downloadedKB:F1} KB / {totalKB:F1} KB ({percent}%) | {lastSpeed:F1} KB/s";
                }
            };

            for (int i = 0; i < fileList.Count; i++)
            {
                lastBytes = 0;

                updateIndex = i + 1;
                string file = fileList[i];
                string url = $"https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/{file}";

                string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Temp/{Path.GetFileName(file)}.{Guid.NewGuid()}.tmp"); // 临时文件路径
                string tempDir = Path.GetDirectoryName(tempPath);
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                try
                {
                    await client.DownloadFileTaskAsync(new Uri(url), tempPath);

                    // 确保目标目录存在
                    string savePath = Path.Combine(baseDir, file);
                    string dir = Path.GetDirectoryName(savePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 覆盖原文件
                    File.Copy(tempPath, savePath, true);
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下载失败: {file}\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                Application.DoEvents(); // 保证 UI 更新
            }

            toolStripStatusLabel1.Text = "";
            updateGameDataToolStripMenuItem.Enabled = true;

            MessageBox.Show("游戏数据已更新完成，请重启程序以应用更新。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void pRTSMAPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://map.ark-nights.com/areas") { UseShellExecute = true });
        }

        private void githubKengxxiaoArknightsGameDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/Kengxxiao/ArknightsGameData/tree/master/zh_CN/gamedata") { UseShellExecute = true });
        }
    }
}
