using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace ArknightsMapViewer
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; }
        public string LogText { get; private set; }
        private Dictionary<Control, RectangleF> controlsBoundScaled = new Dictionary<Control, RectangleF>();

        private LevelView curLevelView;
        private SpawnView curSpawnView;
        private bool needUpdateMap;
        private bool readingMultiFiles;

        private Dictionary<Position, List<IMapData>> mapPredefines = new Dictionary<Position, List<IMapData>>();

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
            Control[] controls = new Control[] { tabControl1, flowLayoutPanel3, groupBox1 }; //要自动调整比例的控件

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
            Helper.InitDrawConfig();
            Helper.InitTileInfoConfig();
            Helper.InitEnemyDatabase();
            Helper.InitCharacterTable();
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
            curSpawnView?.UpdateRandomSpawnGroupNodes(treeView1.SelectedNode);
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

            if (paths.Length > 1)
            {
                readingMultiFiles = true;
            }

            foreach (string path in paths)
            {
                ReadMapFile(path);
            }

            if (readingMultiFiles)
            {
                readingMultiFiles = false;
                UpdateView();
            }
        }

        private void ReadMapFile(string path)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                if (!File.Exists(path))
                {
                    if (Directory.Exists(path))
                    {
                        string[] files = Directory.GetFiles(path, "*.json", SearchOption.AllDirectories);
                        ReadMapFiles(files);
                        return;
                    }

                    Log($"Open File Failed, File does not exist.\n{path}", LogType.Error);
                    return;
                }

                if (!path.ToLower().EndsWith(".json") || (!File.Exists(path) && Directory.Exists(path)))
                {
                    Log($"File Type Error, Request Json Files(*.json)\n{path}", LogType.Error);
                    return;
                }

                string levelJson = File.ReadAllText(path);
                LevelReader levelReader = new LevelReader(levelJson);

                if (levelReader.IsValid)
                {
                    AddLevelDataToView(path, levelReader.LevelData);
                    stopwatch.Stop();
                    Log($"[{Path.GetFileName(path)}] Open Success ({stopwatch.Elapsed.TotalMilliseconds} ms)");
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
                MapDrawer = new MapDrawer(levelData.map),
            };

            int mapWidth = levelData.mapWidth;
            int mapHeight = levelData.mapHeight;
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
                        RouteDrawer = RouteDrawer.Create(route, pathFinding, mapWidth, mapHeight),
                    };
                    if (route != null && route.checkPoints != null)
                    {
                        routeNode.Nodes.Add($"startPosition: {route.startPosition}");
                        for (int j = 0; j < route.checkPoints.Count; j++)
                        {
                            routeNode.Nodes.Add($"checkPoint #{j} {route.checkPoints[j].ToSimpleString()}");
                        }
                        routeNode.Nodes.Add($"endPosition: {route.endPosition}");
                    }
                }
            }

            //AddRouteList
            AddRouteList(nameof(levelData.routes), levelData.routes);
            AddRouteList(nameof(levelData.extraRoutes), levelData.extraRoutes);

            //AddWaveList & InitEnemyList
            int spawnIndex = 0;
            float spawnTime = 0;
            List<ISpawnAction> spawnActions = new List<ISpawnAction>();
            List<PredefineView> predefineViews = new List<PredefineView>();
            Dictionary<string, int> totalWeightDict = new Dictionary<string, int>();

            if (levelData.waves != null)
            {
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
                            TreeNode actionNode = fragmentNode.Nodes.Add($"#{k} {action.ToSimpleString()}");
                            if (action.actionType == ActionType.SPAWN)
                            {
                                if (levelData.routes != null && action.routeIndex >= 0 && action.routeIndex < levelData.routes.Count)
                                {
                                    Route route = levelData.routes[action.routeIndex];
                                    RouteDrawer routeDrawer = RouteDrawer.Create(route, pathFinding, mapWidth, mapHeight);
                                    actionNode.Tag = new SpawnActionView()
                                    {
                                        Action = action,
                                        Drawer = routeDrawer,
                                        IsExtraRoute = false,
                                    };

                                    for (int n = 0; n < action.count; n++)
                                    {
                                        levelData.enemyDbRefs.TryGetValue(action.key, out DbData enemyData);
                                        EnemySpawnView enemySpawnView = new EnemySpawnView()
                                        {
                                            EnemyKey = action.key,
                                            EnemyData = enemyData,
                                            SpawnTime = spawnTime + action.preDelay + n * action.interval,
                                            TotalSpawnIndex = spawnIndex,
                                            Route = route,
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
                                        spawnActions.Add(enemySpawnView);

                                        if (enemySpawnView.SpawnTime > fragmentSpawnTime)
                                        {
                                            fragmentSpawnTime = enemySpawnView.SpawnTime;
                                        }
                                    }
                                }
                            }
                            else if (action.actionType == ActionType.ACTIVATE_PREDEFINED)
                            {
                                if (levelData.predefines != null)
                                {
                                    bool isCard = false;
                                    Predefine.PredefineInst predefine = null;

                                    if (!string.IsNullOrEmpty(action.key))
                                    {
                                        predefine =
                                            levelData.predefines.characterInsts?.Find(x => (x.alias ?? x.inst.characterKey) == action.key) ??
                                            levelData.predefines.tokenInsts?.Find(x => (x.alias ?? x.inst.characterKey) == action.key);

                                        if (predefine == null)
                                        {
                                            predefine =
                                                levelData.predefines.characterCards?.Find(x => (x.alias ?? x.inst.characterKey) == action.key) ??
                                                levelData.predefines.tokenCards?.Find(x => (x.alias ?? x.inst.characterKey) == action.key);
                                            isCard = predefine != null;
                                        }
                                    }

                                    PredefineDrawer predefineDrawer = new PredefineDrawer(predefine, mapWidth, mapHeight);
                                    actionNode.Tag = new PredefineActionView()
                                    {
                                        Action = action,
                                        IsCard = isCard,
                                        Drawer = predefineDrawer,
                                    };

                                    //TODO 单个action内装置是否有多个？
                                    CharacterData characterData = null;
                                    if (predefine != null)
                                    {
                                        GlobalDefine.CharacterTable.TryGetValue(predefine.inst.characterKey, out characterData);
                                    }

                                    PredefineView predefineView = new PredefineView()
                                    {
                                        Predefine = predefine,
                                        PredefineKey = action.key,
                                        IsCard = isCard,
                                        PredefineData = characterData,
                                        ActivateTime = spawnTime + action.preDelay,
                                        TotalWave = levelData.waves.Count,
                                        WaveIndex = i,
                                        SpawnIndexInWave = waveSpawnIndex,
                                        HiddenGroup = action.hiddenGroup,
                                        RandomSpawnGroupKey = action.randomSpawnGroupKey,
                                        RandomSpawnGroupPackKey = action.randomSpawnGroupPackKey,
                                        Weight = action.weight,
                                        PredefineDrawer = predefineDrawer,
                                    };
                                    predefineViews.Add(predefineView);
                                    spawnActions.Add(predefineView);

                                    if (predefineView.ActivateTime > fragmentSpawnTime)
                                    {
                                        fragmentSpawnTime = predefineView.ActivateTime;
                                    }
                                }
                            }
                            else
                            {
                                actionNode.Tag = action;
                            }

                            if (!string.IsNullOrEmpty(action.randomSpawnGroupKey))
                            {
                                if (!totalWeightDict.ContainsKey(action.randomSpawnGroupKey))
                                {
                                    totalWeightDict.Add(action.randomSpawnGroupKey, 0);
                                }
                                totalWeightDict[action.randomSpawnGroupKey] += action.weight;
                            }

                            waveSpawnIndex++;
                            spawnIndex++;
                        }
                        spawnTime = fragmentSpawnTime;
                    }
                    spawnTime += wave.postDelay;
                }
            }

            //AddBranchList
            if (levelData.branches != null)
            {
                TreeNode branchesNode = rootNode.Nodes.Add(nameof(levelData.branches));
                foreach (var item in levelData.branches)
                {
                    string key = item.Key;
                    List<Fragment> phases = item.Value.phases;
                    TreeNode brancheNode = branchesNode.Nodes.Add(key);
                    for (int i = 0; i < phases.Count; i++)
                    {
                        Fragment phase = phases[i];
                        TreeNode phaseNode = brancheNode.Nodes.Add($"phase #{i}");
                        phaseNode.Tag = phase;

                        for (int j = 0; j < phase.actions.Count; j++)
                        {
                            Action action = phase.actions[j];
                            TreeNode actionNode = phaseNode.Nodes.Add($"#{j} {action.ToSimpleString()}");
                            if (action.actionType == ActionType.SPAWN && levelData.extraRoutes != null && action.routeIndex >= 0 && action.routeIndex < levelData.extraRoutes.Count)
                            {
                                actionNode.Tag = new SpawnActionView()
                                {
                                    Action = action,
                                    Drawer = RouteDrawer.Create(levelData.extraRoutes[action.routeIndex], pathFinding, mapWidth, mapHeight),
                                    IsExtraRoute = true,
                                };
                            }
                            else
                            {
                                actionNode.Tag = action;
                            }
                        }
                    }
                }
            }

            //AddPredefineList
            if (levelData.predefines != null && levelData.predefines.tokenInsts != null)
            {
                TreeNode tokensNode = rootNode.Nodes.Add("predefines");

                void InsertPredefine(List<Predefine.PredefineInst> predefineInsts)
                {
                    for (int i = 0; i < predefineInsts.Count; i++)
                    {
                        Predefine.PredefineInst predefine = predefineInsts[i];
                        string predefineKey = predefine.alias ?? predefine.inst.characterKey;
                        PredefineView predefineView = predefineViews.Find(x => predefineKey == x.PredefineKey);
                        if (predefineView != null)
                        {
                            TreeNode predefineNode = tokensNode.Nodes.Add($"#{i} {predefineView.ToSimpleString(false)}");
                            predefineNode.Tag = predefineView;
                        }
                        else
                        {
                            TreeNode predefineNode = tokensNode.Nodes.Add($"#{i} {predefineKey}");
                            GlobalDefine.CharacterTable.TryGetValue(predefine.inst.characterKey, out CharacterData characterData);
                            predefineNode.Tag = new PredefineView()
                            {
                                Predefine = predefine,
                                PredefineKey = predefineKey,
                                PredefineData = characterData,
                                ActivateTime = -1,
                                PredefineDrawer = new PredefineDrawer(predefine, mapWidth, mapHeight),
                            };
                        }
                    }
                }

                InsertPredefine(levelData.predefines.characterInsts);
                InsertPredefine(levelData.predefines.tokenInsts);
                InsertPredefine(levelData.predefines.characterCards);
                InsertPredefine(levelData.predefines.tokenCards);
            }

            //AddSpawnList
            if (spawnActions.Count > 0)
            {
                TreeNode spawnsNode = rootNode.Nodes.Add("spawns");
                SpawnView spawnView = new SpawnView()
                {
                    SpawnsNode = spawnsNode,
                    ShowPredefined = true,
                };
                spawnsNode.Tag = spawnView;
                spawnActions.Sort((a, b) => a.CompareTo(b));

                Dictionary<string, TreeNode> randomSpawnGroupDefaultNodes = new Dictionary<string, TreeNode>();
                for (int i = 0; i < spawnActions.Count; i++)
                {
                    ISpawnAction spawnAction = spawnActions[i];
                    TreeNode spawnNode = new TreeNode
                    {
                        Tag = spawnAction
                    };

                    if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupKey) && totalWeightDict.TryGetValue(spawnAction.RandomSpawnGroupKey, out int totalWeight))
                    {
                        spawnAction.TotalWeight = totalWeight;
                    }

                    if (spawnAction is EnemySpawnView enemySpawnView)
                    {
                        Route route = enemySpawnView.Route;
                        if (route != null && route.checkPoints != null)
                        {
                            spawnNode.Nodes.Add($"startPosition: {route.startPosition}");
                            for (int j = 0; j < route.checkPoints.Count; j++)
                            {
                                spawnNode.Nodes.Add($"checkPoint #{j} {route.checkPoints[j].ToSimpleString()}");
                            }
                            spawnNode.Nodes.Add($"endPosition: {route.endPosition}");
                        }
                    }

                    if (!string.IsNullOrEmpty(spawnAction.HiddenGroup))
                    {
                        if (!spawnView.HiddenGroups.ContainsKey(spawnAction.HiddenGroup))
                        {
                            spawnView.HiddenGroups.Add(spawnAction.HiddenGroup, true);
                        }
                    }

                    if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupKey))
                    {
                        if (!spawnView.RandomSpawnGroupNodesDict.ContainsKey(spawnAction.RandomSpawnGroupKey))
                        {
                            spawnView.RandomSpawnGroupNodesDict.Add(spawnAction.RandomSpawnGroupKey, new List<TreeNode>());
                        }
                        if (!randomSpawnGroupDefaultNodes.ContainsKey(spawnAction.RandomSpawnGroupKey))
                        {
                            randomSpawnGroupDefaultNodes.Add(spawnAction.RandomSpawnGroupKey, spawnNode);
                        }
                        spawnView.RandomSpawnGroupNodesDict[spawnAction.RandomSpawnGroupKey].Add(spawnNode);
                    }

                    if (!string.IsNullOrEmpty(spawnAction.RandomSpawnGroupPackKey))
                    {
                        if (!spawnView.RandomSpawnGroupPackNodesDict.ContainsKey(spawnAction.RandomSpawnGroupPackKey))
                        {
                            spawnView.RandomSpawnGroupPackNodesDict.Add(spawnAction.RandomSpawnGroupPackKey, new List<TreeNode>());
                        }
                        spawnView.RandomSpawnGroupPackNodesDict[spawnAction.RandomSpawnGroupPackKey].Add(spawnNode);
                    }

                    spawnView.SpawnNodesList.Add(spawnNode);
                    spawnView.ValidSpawnNodes.Add(spawnNode, true);
                }

                if (!readingMultiFiles)
                {
                    spawnsNode.Expand();
                }

                foreach (var item in randomSpawnGroupDefaultNodes)
                {
                    spawnView.UpdateRandomSpawnGroupNodes(item.Value);
                }
                spawnView.UpdateSpawnViewNodeColor();
                spawnView.UpdateNodes();
            }

            if (!readingMultiFiles)
            {
                rootNode.Expand();
                treeView1.SelectedNode = rootNode;
            }
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
            SpawnView spawnView = null;
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
                    spawnView ??= treeNode.Tag as SpawnView;
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
            }

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;

            if (curLevelView != null)
            {
                Bitmap bitmap = Helper.CreateBitmap(curLevelView.LevelData);

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
            }

            UpdateLabelInfo(routeSubIndex);
            UpdateSpawnGroups(spawnView);
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

            PredefineDrawer predefineDrawer = predefineDrawerView?.GetDrawer();
            if (predefineDrawer != null && predefineDrawer.Predefine != null)
            {
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
                        if (checkBox3.Checked || !predefineView.Predefine.hidden)
                        {
                            predefineView.PredefineDrawer?.Draw(bitmap);

                            if (!mapPredefines.ContainsKey(predefineView.Predefine.position))
                            {
                                mapPredefines.Add(predefineView.Predefine.position, new List<IMapData>());
                            }
                            mapPredefines[predefineView.Predefine.position].Add(predefineView);
                        }
                    }
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
                showCheckBox4 = true;
                routeDrawer.ShowRouteLength = checkBox4.Checked;
                if (routeSubIndex < 0)
                {
                    routeDrawer.Draw(bitmap);
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
            StringBuilder stringBuilder = new StringBuilder();

            TreeNode treeNode = treeView1.SelectedNode;
            while (treeNode != null)
            {
                if (treeNode.Tag is IMapDataView<Route> routeDataView)
                {
                    Route route = routeDataView?.GetData();
                    stringBuilder.AppendLine($"[{treeView1.SelectedNode.Text}]");
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

            richTextBox1.Text = stringBuilder.ToString();
            richTextBox1.SelectionStart = 0;
            richTextBox1.ScrollToCaret();
        }

        private void UpdateSpawnGroups(SpawnView spawnView)
        {
            if (curSpawnView != spawnView)
            {
                curSpawnView = spawnView;
                flowLayoutPanel2.Controls.Clear();
                if (spawnView != null)
                {
                    checkBox7.Checked = spawnView.ShowPredefined;
                    foreach (var item in spawnView.HiddenGroups)
                    {
                        CheckBox checkBox = new CheckBox
                        {
                            Text = item.Key,
                            Checked = item.Value,
                            AutoSize = true
                        };
                        checkBox.CheckedChanged += (s, e) =>
                        {
                            spawnView.HiddenGroups[checkBox.Text] = checkBox.Checked;
                            spawnView.UpdateNodes();
                            UpdateView();
                        };
                        flowLayoutPanel2.Controls.Add(checkBox);
                    }
                }
            }
            checkBox7.Visible = spawnView != null;
        }

        private void FilterTreeNode()
        {
            void Filter(TreeNodeCollection treeNodes)
            {
                foreach (TreeNode treeNode in treeNodes)
                {
                    if (!string.IsNullOrEmpty(textBox1.Text) && treeNode.Text.Contains(textBox1.Text))
                    {
                        treeNode.ForeColor = Color.Red;
                    }
                    else
                    {
                        if (curSpawnView != null && curSpawnView.ValidSpawnNodes.TryGetValue(treeNode, out bool isValid))
                        {
                            treeNode.ForeColor = isValid ? Color.FromKnownColor(KnownColor.WindowText) : Color.Gray;
                        }
                        else
                        {
                            treeNode.ForeColor = Color.FromKnownColor(KnownColor.WindowText);
                        }
                    }
                    Filter(treeNode.Nodes);
                }
            }

            //搜索框筛选
            Filter(treeView1.Nodes);
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
            LogText += $"[{logType.ToString().ToUpper()}][{DateTime.Now:yyyy-MM-dd HH:mm:ss:ff}] {msg} \n";
        }

        private void ShowLog()
        {
            //MessageBox.Show(log);
            LogForm logForm = new LogForm();
            logForm.UpdateLog(LogText);
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

            richTextBox1.Text = text;
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
            if (curSpawnView != null)
            {
                curSpawnView.ShowPredefined = checkBox7.Checked;
                curSpawnView.UpdateNodes();
                UpdateView();
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

            bool saved = false;
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
                        saved = true;
                        backGroundSaved = true;
                    }

                    if (saveRoute && pictureBox1.Image != null)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_{treeNode.Parent.Text}_R{treeNode.Index}.png");
                        pictureBox1.Image.Save(fullPath, ImageFormat.Png);
                        saved = true;
                    }

                    if (saveFull)
                    {
                        string fullPath = Path.Combine(path, $"{curLevelView.Name}_{treeNode.Parent.Text}_F{treeNode.Index}.png");
                        Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                        pictureBox1.DrawToBitmap(bitmap, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                        bitmap.Save(fullPath, ImageFormat.Png);
                        saved = true;
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

            if (saved)
            {
                MessageBox.Show("Export Image Completed.");
                Process.Start(path);
            }
        }
    }
}
