using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using ArknightsMap;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Action = ArknightsMap.Action;

namespace ArknightsMapViewer
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        public string logText { get; private set; }

        private LevelView curLevelView;
        private SpawnView curSpawnView;

        public MainForm()
        {
            Instance = this;
            InitializeComponent();
        }

        ~MainForm()
        {
            Instance = null;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            logText = "";
            Helper.InitDrawConfig();
            Helper.InitTileDefineConfig();
            Helper.InitEnemyDatabase();
            UpdateView();

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
            UpdateView();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
            e.Node.ContextMenuStrip = contextMenuStrip1;
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

        private void expendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                return;
            }

            treeView1.SelectedNode.ExpandAll();
        }

        private void collapseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode == null)
            {
                return;
            }

            treeView1.SelectedNode.Collapse();
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
            ShowLog();
        }

        private void showLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowLog();
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

            if (!File.Exists(path))
            {
                Log($"Open File Failed, File does not exist.\n{path}", LogType.Error);
                return;
            }

            if (!path.ToLower().EndsWith(".json") || (!File.Exists(path) && Directory.Exists(path)))
            {
                Log($"File Type Error, Request Json Files(*.json)\n{path}", LogType.Error);
                return;
            }

            try
            {
                string levelJson = File.ReadAllText(path);
                LevelReader levelReader = new LevelReader(levelJson);

                if (levelReader.IsValid)
                {
                    Log($"[{Path.GetFileName(path)}] Open Success");
                    AddLevelDataToView(path, levelReader.LevelData);
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
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"[{Path.GetFileName(path)}] Open Failed, {ex.Message}";
                Log(errorMsg, LogType.Error);
#if DEBUG
                Log(ex.StackTrace, LogType.Debug);
#endif
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
            rootNode.Tag = new LevelView()
            {
                Path = path,
                Name = fileName,
                LevelData = levelData,
                MapDrawer = new WinformMapDrawer(pictureBox1, levelData.map),
            };

            int mapWidth = levelData.map.GetLength(0);
            int mapHeight = levelData.map.GetLength(1);
            //PathFinding pathFinding = new AStarPathFinding(); //A*在节点周围多个点cost相同的情况，可能会选到后续较远的路径，即不一定会搜到最短路径
            PathFinding pathFinding = new DijkstraPathFinding();
            pathFinding.SetIsBarrierArray(Helper.GetIsBarrierArray(levelData));

            //TODO SPFA https://blog.csdn.net/beijinghorn/article/details/125510627

            void AddRouteList(string name, List<Route> routes)
            {
                if (routes == null || routes.Count <= 0)
                {
                    return;
                }

                string title = name.TrimEnd('s'); // routes -> route
                TreeNode routesNode = rootNode.Nodes.Add(name);
                for (int i = 0; i < routes.Count; i++)
                {
                    Route route = routes[i];
                    TreeNode routeNode = routesNode.Nodes.Add($"{title} #{i}");
                    routeNode.Tag = new RouteView()
                    {
                        RouteIndex = i,
                        Route = route,
                        RouteDrawer = WinformRouteDrawer.Create(pictureBox1, route, pathFinding, mapWidth, mapHeight),
                    };
                    if (route.checkPoints != null)
                    {
                        routeNode.Nodes.Add($"startPosition: {route.startPosition}");
                        for (int j = 0; j < route.checkPoints.Count; j++)
                        {
                            routeNode.Nodes.Add($"checkPoint #{j} {route.checkPoints[j].ToSimpleString()}");
                        }
                        routeNode.Nodes.Add($"endPosition: {route.endPosition}");
                    }
                }
                //routesNode.Expand();
            }

            //AddRouteList
            AddRouteList(nameof(levelData.routes), levelData.routes);
            AddRouteList(nameof(levelData.extraRoutes), levelData.extraRoutes);

            //AddWaveList & InitEnemyList
            int spawnIndex = 0;
            float spawnTime = 0;
            List<EnemySpawnView> enemySpawnViews = new List<EnemySpawnView>();
            TreeNode wavesNode = rootNode.Nodes.Add(nameof(levelData.waves));
            for (int i = 0; i < levelData.waves.Count; i++)
            {
                Wave wave = levelData.waves[i];
                TreeNode waveNode = wavesNode.Nodes.Add($"wave #{i}");
                waveNode.Tag = wave;

                int waveSpawnIndex = 0;
                spawnTime += wave.preDelay;
                for (int j = 0; j < wave.fragments.Count; j++)
                {
                    Fragment fragment = wave.fragments[j];
                    TreeNode fragmentNode = waveNode.Nodes.Add($"fragment #{j}");
                    fragmentNode.Tag = fragment;

                    float fragmentSpawnTime = 0;
                    spawnTime += fragment.preDelay;
                    for (int k = 0; k < fragment.actions.Count; k++)
                    {
                        Action action = fragment.actions[k];
                        TreeNode actionNode = fragmentNode.Nodes.Add($"action #{k} {action.ToSimpleString()}");
                        if (action.actionType == ActionType.SPAWN && action.routeIndex >= 0 && action.routeIndex < levelData.routes.Count)
                        {
                            IRouteDrawer routeDrawer = WinformRouteDrawer.Create(pictureBox1, levelData.routes[action.routeIndex], pathFinding, mapWidth, mapHeight);
                            actionNode.Tag = new SpawnActionView()
                            {
                                SpawnAction = action,
                                RouteDrawer = routeDrawer,
                            };

                            for (int n = 0; n < action.count; n++)
                            {
                                levelData.enemyDbRefs.TryGetValue(action.key, out DbData enemyData);
                                EnemySpawnView enemySpawnView = new EnemySpawnView()
                                {
                                    EnemyKey = action.key,
                                    EnemyData = enemyData,
                                    //TODO action中其他字段作用（下载所有地图json查看）
                                    SpawnTime = spawnTime + action.preDelay + n * action.interval,
                                    TotalSpawnIndex = spawnIndex,
                                    RouteIndex = action.routeIndex,
                                    TotalWave = levelData.waves.Count,
                                    WaveIndex = i,
                                    SpawnIndexInWave = waveSpawnIndex,
                                    HiddenGroup = action.hiddenGroup,
                                    RandomSpawnGroupKey = action.randomSpawnGroupKey,
                                    RandomSpawnGroupPackKey = action.randomSpawnGroupPackKey,
                                    Weight = action.weight,
                                    BlockFragment = action.blockFragment,
                                    BlockWave = !action.dontBlockWave,
                                    MaxTimeWaitingForNextWave = wave.maxTimeWaitingForNextWave,
                                    RouteDrawer = routeDrawer,
                                };
                                enemySpawnViews.Add(enemySpawnView);

                                if (enemySpawnView.SpawnTime > fragmentSpawnTime)
                                {
                                    fragmentSpawnTime = enemySpawnView.SpawnTime;
                                }
                            }

                            waveSpawnIndex++;
                            spawnIndex++;
                        }
                        else
                        {
                            actionNode.Tag = action;
                        }
                    }
                    spawnTime = fragmentSpawnTime;
                }
                spawnTime += wave.postDelay;

                //waveNode.Expand();
            }
            //wavesNode.Expand();

            //AddSpawnList
            TreeNode spawnsNode = rootNode.Nodes.Add("spawns");
            SpawnView spawnView = new SpawnView()
            {
                SpawnsNode = spawnsNode,
            };
            spawnsNode.Tag = spawnView;
            enemySpawnViews.Sort((a, b) => a.CompareTo(b));

            void InsertGroup(string groupName, TreeNode treeNode)
            {
                if (!string.IsNullOrEmpty(groupName))
                {
                    if (!spawnView.SpawnGroups.ContainsKey(groupName))
                    {
                        spawnView.SpawnGroups.Add(groupName, true);
                    }
                }
            }

            for (int i = 0; i < enemySpawnViews.Count; i++)
            {
                EnemySpawnView enemySpawnView = enemySpawnViews[i];
                TreeNode spawnNode = spawnsNode.Nodes.Add($"#{i} {enemySpawnView.ToSimpleString()}");
                spawnNode.Tag = enemySpawnView;
                InsertGroup(enemySpawnView.HiddenGroup, spawnNode);
                InsertGroup(enemySpawnView.RandomSpawnGroupKey, spawnNode);
                InsertGroup(enemySpawnView.RandomSpawnGroupPackKey, spawnNode);
            }
            spawnsNode.Expand();

            rootNode.Expand();
            rootNode.EnsureVisible();
            treeView1.SelectedNode = rootNode;
        }

        private void UpdateView()
        {
            TreeNode treeNode = treeView1.SelectedNode;

            StringBuilder stringBuilder = new StringBuilder();
            LevelView levelView = null;
            RouteView routeView = null;
            SpawnActionView spawnActionView = null;
            SpawnView spawnView = null;
            EnemySpawnView enemySpawnView = null;
            int routeSubIndex = -1;
            checkBox1.Visible = false;
            checkBox2.Visible = false;
            checkBox3.Visible = false;

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
                if (spawnActionView == null && treeNode.Level == 4)
                {
                    spawnActionView = treeNode.Tag as SpawnActionView;
                }
                if (spawnView == null && treeNode.Level == 1)
                {
                    spawnView = treeNode.Tag as SpawnView;
                }
                if (enemySpawnView == null && treeNode.Level == 2)
                {
                    enemySpawnView = treeNode.Tag as EnemySpawnView;
                    if (enemySpawnView != null)
                    {
                        spawnView = treeNode.Parent.Tag as SpawnView;
                    }
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

            IRouteDrawer routeDrawer = null;
            if (routeView != null)
            {
                routeDrawer = routeView.RouteDrawer;
            }
            else if (spawnActionView != null)
            {
                routeDrawer = spawnActionView.RouteDrawer;
            }
            else if (enemySpawnView != null)
            {
                checkBox3.Visible = true;
                routeDrawer = enemySpawnView.RouteDrawer;
            }

            if (routeDrawer != null)
            {
                checkBox2.Visible = true;
                routeDrawer.ShowRouteLength = checkBox2.Checked;
                if (routeSubIndex < 0)
                {
                    routeDrawer.DrawRoute();
                }
                else
                {
                    checkBox1.Visible = true;
                    int checkPointIndex = routeSubIndex - 1;
                    int startIndex = checkBox1.Checked ? checkPointIndex : -1; //-1表示从startPosition开始

                    routeDrawer.InitCanvas();
                    for (int i = startIndex; i <= checkPointIndex; i++)
                    {
                        routeDrawer.DrawMoveLine(i);
                    }
                    for (int i = startIndex; i <= checkPointIndex; i++)
                    {
                        routeDrawer.DrawCheckPoint(i);
                    }
                    routeDrawer.RefreshCanvas();
                }
            }

            //Info
            IData data = treeView1.SelectedNode?.Tag as IData;
            if (levelView != null && routeView == null && spawnActionView == null && data == null)
            {
                LevelData levelData = levelView.LevelData;
                stringBuilder.AppendLine($"[{levelView.Name}]");
                stringBuilder.AppendLine(levelData.options.ToString());
                stringBuilder.AppendLine($"width: {levelData.map.GetLength(0)}");
                stringBuilder.AppendLine($"height: {levelData.map.GetLength(1)}");
                stringBuilder.AppendLine($"routes: {levelData.routes.Count}");
                stringBuilder.AppendLine($"waves: {levelData.waves.Count}");
                stringBuilder.AppendLine($"extraRoutes: {levelData.extraRoutes.Count}");
            }
            else if (routeView != null)
            {
                Route route = routeView.Route;
                stringBuilder.AppendLine($"[{treeView1.SelectedNode.Text}]");
                if (routeSubIndex < 0)
                {
                    stringBuilder.AppendLine(route.ToString());
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
                }
            }
            else if (spawnActionView != null)
            {
                Action action = spawnActionView.SpawnAction;
                stringBuilder.AppendLine($"[{treeView1.SelectedNode.Text}]");
                stringBuilder.AppendLine(action.ToString());
            }
            else if (enemySpawnView != null)
            {
                stringBuilder.AppendLine($"[{treeView1.SelectedNode.Text}]");
                stringBuilder.AppendLine(enemySpawnView.ToString(checkBox3.Checked));
            }
            else if (data != null)
            {
                stringBuilder.AppendLine($"[{treeView1.SelectedNode.Text}]");
                stringBuilder.AppendLine(data.ToString());
            }

            if (curSpawnView != spawnView)
            {
                curSpawnView = spawnView;
                flowLayoutPanel2.Controls.Clear();
                if (spawnView != null)
                {
                    foreach (var item in spawnView.SpawnGroups)
                    {
                        CheckBox checkBox = new CheckBox
                        {
                            Text = item.Key,
                            Checked = item.Value,
                        };
                        checkBox.CheckedChanged += (s, e) =>
                        {
                            spawnView.SpawnGroups[checkBox.Text] = checkBox.Checked;
                            spawnView.UpdateNodes();
                            UpdateView();
                        };
                        flowLayoutPanel2.Controls.Add(checkBox);
                    }

                }
            }

            label1.Text = stringBuilder.ToString();
        }

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
            logText += $"[{logType.ToString().ToUpper()}][{DateTime.Now:yyyy-MM-dd HH:mm:ss:ff}] {msg} \n";
        }

        private void ShowLog()
        {
            //MessageBox.Show(log);
            LogForm logForm = new LogForm();
            logForm.UpdateLog(logText);
            logForm.ShowDialog();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (pictureBox1.BackgroundImage == null || curLevelView == null)
            {
                return;
            }

            //Point to row & col
            Point point = e.Location;
            Tile[,] map = curLevelView.LevelData.map;
            int mapWidth = map.GetLength(0);
            int mapHeight = map.GetLength(1);

            Position position = Helper.PointToPosition(point, mapHeight);
            position.col = position.col.Clamp(0, mapWidth - 1);
            position.row = position.row.Clamp(0, mapHeight - 1);
            Tile tile = map[position.col, position.row];

            string text = $"[Tile: {position}]\n" + tile.ToString();

            if (!GlobalDefine.TileColor.ContainsKey(tile.tileKey))
            {
                text += "\n(Warning: Undefined Tile)";
            }

            label1.Text = text;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            UpdateView();
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

            bool backGroundSaved = false;
            void ForEachRouteNode(TreeNode treeNode)
            {
                if (backGroundSaved && !saveRoute && !saveFull)
                {
                    return;
                }

                if (treeNode.Tag is RouteView routeView)
                {
                    treeView1.SelectedNode = treeNode;
                    UpdateView();

                    if (saveBackground && !backGroundSaved)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_Background.png");
                        pictureBox1.BackgroundImage.Save(fullPath, ImageFormat.Png);
                        backGroundSaved = true;
                    }

                    if (saveRoute)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_{treeNode.Parent.Text}_R{routeView.RouteIndex}.png");
                        pictureBox1.Image.Save(fullPath, ImageFormat.Png);
                    }

                    if (saveFull)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_{treeNode.Parent.Text}_F{routeView.RouteIndex}.png");
                        Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                        pictureBox1.DrawToBitmap(bitmap, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                        bitmap.Save(fullPath, ImageFormat.Png);
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

            MessageBox.Show("Export Image Completed.");
        }
    }
}
