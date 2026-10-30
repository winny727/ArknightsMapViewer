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
        private bool isExporting;
        private readonly MapLegend mapLegend;
        // Control.Visible 会受父容器隐藏影响；组布局需要各选项自身的显示状态。
        private readonly Dictionary<CheckBox, bool> optionVisibility = new Dictionary<CheckBox, bool>();
        private readonly List<(GroupBox Group, CheckBox[] Options)> optionGroups = new List<(GroupBox, CheckBox[])>();
        private TableLayoutPanel optionGrid;
        private GroupBox spawnFilterGroup;
        private int optionGroupWidth;

        private Dictionary<Position, List<IMapData>> mapPredefines = new Dictionary<Position, List<IMapData>>();

        private TimelineSimulator curTimelineSimulator;
        private bool isSimulationEnabled;
        private bool rawSetValue;

        private const string lastBuildDate = "20251021";

        public MainForm()
        {
            Instance = this;
            InitializeComponent();
            mapLegend = new MapLegend { Visible = false };
            InitializeOptionGroups();
            flowLayoutPanel3.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel3.WrapContents = false;
            flowLayoutPanel3.Padding = new Padding(12);
            flowLayoutPanel3.BackColor = Color.FromArgb(28, 28, 28);
            pictureBox1.Margin = Padding.Empty;
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            flowLayoutPanel3.Controls.Add(mapLegend);
            flowLayoutPanel3.Controls.SetChildIndex(pictureBox1, 0);
            flowLayoutPanel3.SizeChanged += (sender, args) =>
                mapLegend.Width = Math.Max(112, flowLayoutPanel3.ClientSize.Width - 44);
            mapLegend.Width = Math.Max(112, flowLayoutPanel3.ClientSize.Width - 44);
            InitControlsBoundsScale();
        }

        private void InitializeOptionGroups()
        {
            // 表格统一每一行的组框高度；外层只负责整组选项的滚动。
            optionGroupWidth = Math.Max(166, checkBoxTileName.GetPreferredSize(Size.Empty).Width + 20);
            flowLayoutPanel1.SuspendLayout();
            optionGrid = new TableLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty, Padding = Padding.Empty,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
            };
            flowLayoutPanel1.Controls.Add(optionGrid);
            var toolTip = new ToolTip(components);
            toolTip.SetToolTip(checkBoxTileName, "控制未配置固定文字时的地块名称；配置中的固定地块文字始终显示，地图图例始终显示在地图下方。");
            AddOptionGroup("地图显示", checkBoxTileName, checkBox1, checkBox2, checkBox3, checkBox7);
            AddOptionGroup("路径与敌人", checkBox4, checkBox5, checkBox6);
            spawnFilterGroup = AddOptionGroup("分组筛选", checkBox8);
            var filterBody = (FlowLayoutPanel)spawnFilterGroup.Controls[0];
            flowLayoutPanel2.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            flowLayoutPanel2.AutoScroll = false;
            flowLayoutPanel2.AutoSize = true;
            flowLayoutPanel2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowLayoutPanel2.MinimumSize = new Size(optionGroupWidth - 18, 0);
            flowLayoutPanel2.MaximumSize = new Size(optionGroupWidth - 18, 0);
            flowLayoutPanel2.Margin = Padding.Empty;
            flowLayoutPanel2.Size = new Size(optionGroupWidth - 18, 0);
            filterBody.Controls.Add(flowLayoutPanel2);
            groupBox1.SizeChanged += (sender, args) => LayoutOptionPanel();
            LayoutOptionPanel();
            flowLayoutPanel1.ResumeLayout(true);
        }

        private GroupBox AddOptionGroup(string title, params CheckBox[] options)
        {
            var group = new GroupBox
            {
                Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(optionGroupWidth, 0), Dock = DockStyle.Fill,
                Padding = new Padding(6, 16, 6, 6), Margin = new Padding(0, 0, 6, 8),
            };
            var body = new FlowLayoutPanel
            {
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                Dock = DockStyle.Top, Margin = Padding.Empty,
                MinimumSize = new Size(optionGroupWidth - 12, 0), MaximumSize = new Size(optionGroupWidth - 12, 0),
            };
            foreach (CheckBox option in options)
            {
                option.MaximumSize = new Size(optionGroupWidth - 18, 0);
                body.Controls.Add(option);
                optionVisibility[option] = false;
                option.Visible = false;
            }
            group.Controls.Add(body);
            optionGroups.Add((group, options));
            return group;
        }

        private void LayoutOptionPanel()
        {
            if (Disposing || IsDisposed) return;
            int width = Math.Min(2 * (optionGroupWidth + 6) + SystemInformation.VerticalScrollBarWidth + 4,
                Math.Max(optionGroupWidth + SystemInformation.VerticalScrollBarWidth + 10, groupBox1.ClientSize.Width / 2));
            int height = Math.Max(1, groupBox1.ClientSize.Height - 24);
            flowLayoutPanel1.SetBounds(groupBox1.ClientSize.Width - width - 6, 18, width, height);
            richTextBox1.SetBounds(6, 18, Math.Max(1, flowLayoutPanel1.Left - 12), height);
            LayoutOptionGrid();
        }

        private void SetOptionVisible(CheckBox option, bool visible)
        {
            optionVisibility[option] = visible;
            option.Visible = visible;
        }

        private void LayoutOptionGrid()
        {
            if (Disposing || IsDisposed || optionGrid == null || optionGrid.Disposing || optionGrid.IsDisposed) return;
            int width = Math.Max(optionGroupWidth + 6,
                flowLayoutPanel1.Width - SystemInformation.VerticalScrollBarWidth - 2);
            int columns = width >= 2 * (optionGroupWidth + 6) ? 2 : 1;
            width -= width % columns;
            var visibleGroups = new List<GroupBox>();
            foreach (var item in optionGroups)
            {
                bool show = item.Group == spawnFilterGroup && flowLayoutPanel2.Controls.Count > 0;
                foreach (CheckBox option in item.Options)
                    show |= optionVisibility.TryGetValue(option, out bool visible) && visible;
                if (show && curLevelView?.MapDrawer != null) visibleGroups.Add(item.Group);
            }
            bool changed = optionGrid.ColumnCount != columns || optionGrid.Controls.Count != visibleGroups.Count;
            for (int index = 0; !changed && index < visibleGroups.Count; index++)
                changed = optionGrid.GetControlFromPosition(index % columns, index / columns) != visibleGroups[index];
            optionGrid.MinimumSize = new Size(width, 0);
            optionGrid.MaximumSize = new Size(width, 0);
            if (!changed)
            {
                optionGrid.PerformLayout();
                return;
            }
            Point scrollPosition = flowLayoutPanel1.AutoScrollPosition;
            flowLayoutPanel1.SuspendLayout();
            optionGrid.SuspendLayout();
            try
            {
                // 隐藏的组仍保留在窗体控件树中，切换地图后可以恢复并正确释放资源。
                optionGrid.Controls.Clear();
                foreach (var item in optionGroups)
                {
                    item.Group.Visible = false;
                    flowLayoutPanel1.Controls.Add(item.Group);
                }
                optionGrid.ColumnCount = columns;
                optionGrid.RowCount = Math.Max(1, (visibleGroups.Count + columns - 1) / columns);
                optionGrid.ColumnStyles.Clear();
                optionGrid.RowStyles.Clear();
                for (int column = 0; column < columns; column++)
                    optionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                for (int row = 0; row < optionGrid.RowCount; row++)
                    optionGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                for (int index = 0; index < visibleGroups.Count; index++)
                {
                    GroupBox group = visibleGroups[index];
                    optionGrid.Controls.Add(group, index % columns, index / columns);
                    group.Visible = true;
                }
            }
            finally
            {
                optionGrid.ResumeLayout(true);
                flowLayoutPanel1.ResumeLayout(true);
                flowLayoutPanel1.AutoScrollPosition = new Point(-scrollPosition.X, -scrollPosition.Y);
            }
        }

        private void checkBoxTileName_CheckedChanged(object sender, EventArgs e)
        {
            needUpdateMap = true;
            UpdateView();
            flowLayoutPanel3.PerformLayout();
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
            if (isExporting)
            {
                return;
            }

            TreeNode rootNode = e.Node;
            while (rootNode?.Parent != null)
            {
                rootNode = rootNode.Parent;
            }

            LevelView selectedLevelView = rootNode?.Tag as LevelView;
            selectedLevelView?.SpawnView?.UpdateRandomSpawnGroupNodes(e.Node);
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
            //dialog.InitialDirectory = Environment.CurrentDirectory;
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

            int mapHeight = levelData.mapHeight;
            PathFinding pathFinding = new AStarPathFinding();
            //PathFinding pathFinding = new DijkstraPathFinding();
            pathFinding.SetGridArray(Helper.GetGridArray(levelData));

            //TODO SPFA

            RouteDrawer routeDrawerCreater(Route route) => RouteDrawer.Create(route, pathFinding, mapHeight);
            PredefineDrawer predefineDrawerCreater(Predefine.PredefineInst predefine) => PredefineDrawer.Create(predefine, mapHeight);

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

            bool levelChanged = curLevelView != levelView;
            if (levelChanged || needUpdateMap)
            {
                curLevelView = levelView;
                pictureBox1.BackgroundImage?.Dispose();
                pictureBox1.BackgroundImage = null;
                needUpdateMap = false;

                if (curLevelView != null && curLevelView.MapDrawer != null)
                {
                    Bitmap bitmap = Helper.CreateBitmap(curLevelView.LevelData);

                    curLevelView.MapDrawer.MarkUndefinedTile = checkBox1.Checked;
                    curLevelView.MapDrawer.ShowTileName = checkBoxTileName.Checked;
                    curLevelView.MapDrawer.Draw(bitmap);
                    pictureBox1.Width = bitmap.Width;
                    pictureBox1.Height = bitmap.Height;
                    pictureBox1.BackgroundImage = bitmap;
                    mapLegend.SetEntries(curLevelView.MapDrawer.GetLegendEntries());
                }
                else mapLegend.SetEntries(null);

                // 显示选项只重绘底图；切换关卡时才重建分组与时间轴控件。
                if (levelChanged)
                {
                    UpdateSpawnGroups();
                    UpdateTimelineSimulationState();
                }
            }

            pictureBox1.Image?.Dispose();
            pictureBox1.Image = null;

            SetOptionVisible(checkBoxTileName, curLevelView?.MapDrawer != null);
            if (curLevelView != null)
            {
                Bitmap bitmap = Helper.CreateBitmap(curLevelView.LevelData);
                drawingImage = bitmap;

                UpdatePredefine(bitmap, predefineRootNode, predefineDrawerView);
                UpdateRoute(bitmap, routeDrawerView, routeSubIndex);
                pictureBox1.Image = bitmap;

                if (curLevelView.MapDrawer != null)
                {
                    SetOptionVisible(checkBox1, curLevelView.MapDrawer.HaveUndefinedTiles);
                }
                SetOptionVisible(checkBox6, enemySpawnView != null);
            }
            else
            {
                SetOptionVisible(checkBox1, false);
                SetOptionVisible(checkBox2, false);
                SetOptionVisible(checkBox3, false);
                SetOptionVisible(checkBox4, false);
                SetOptionVisible(checkBox5, false);
                SetOptionVisible(checkBox6, false);
                SetOptionVisible(checkBox7, false);
                SetOptionVisible(checkBox8, false);
            }

            UpdateLabelInfo(routeSubIndex);
            FilterTreeNode();

            LayoutOptionGrid();
            mapLegend.Visible = curLevelView?.MapDrawer != null;
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

            SetOptionVisible(checkBox2, predefineRootNode != null && predefineRootNode.Nodes.Count > 0);
            SetOptionVisible(checkBox3, showCheckBox3);
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

            SetOptionVisible(checkBox4, showCheckBox4);
            SetOptionVisible(checkBox5, showCheckBox5);
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
            while (flowLayoutPanel2.Controls.Count > 0) flowLayoutPanel2.Controls[0].Dispose();
            SetOptionVisible(checkBox7, false);
            SetOptionVisible(checkBox8, false);
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
                        MaximumSize = new Size(optionGroupWidth - 22, 0),
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
                        SetOptionVisible(checkBox7, true);
                        break;
                    }
                }
                foreach (var item in spawnView.ValidSpawnNodes)
                {
                    if (!item.Value)
                    {
                        SetOptionVisible(checkBox8, true);
                        break;
                    }
                }
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

            using var folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            string path = folderBrowserDialog.SelectedPath;
            int saved;
            try
            {
                saved = ExportImages(selectedNode, path, saveBackground, saveRoute, saveFull);
            }
            catch (Exception ex)
            {
                Log($"Export Image Failed: {ex.Message}", LogType.Error);
                MessageBox.Show($"导出图片失败，部分图片可能已经保存。\n{ex.Message}", "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (saved > 0)
            {
                MessageBox.Show("Export Image Completed.");
                if (saved > 1)
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            }
        }

        private static string GetExportFileStem(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder();
            foreach (char character in name ?? "")
            {
                builder.Append(char.IsControl(character) || Array.IndexOf(invalidChars, character) >= 0 ? '_' : character);
            }
            string stem = builder.ToString().Trim().TrimEnd('.', ' ');
            if (stem.Length == 0) stem = "Image";

            // 给扩展名和重名后缀留出空间，避免长分组标签超过单个文件名的长度限制。
            if (stem.Length > 120)
            {
                int length = char.IsHighSurrogate(stem[119]) ? 119 : 120;
                stem = stem.Substring(0, length).TrimEnd('.', ' ');
            }

            // Windows设备名即使带扩展名也不能作为普通文件使用；截断后的名字也要检查。
            string deviceName = stem.Split('.')[0].TrimEnd(' ').ToUpperInvariant();
            if (deviceName == "CON" || deviceName == "PRN" || deviceName == "AUX" || deviceName == "NUL" ||
                deviceName == "CONIN$" || deviceName == "CONOUT$" ||
                (deviceName.Length == 4 && (deviceName.StartsWith("COM") || deviceName.StartsWith("LPT")) &&
                 "123456789¹²³".IndexOf(deviceName[3]) >= 0)) stem = "_" + stem;
            return stem;
        }

        private static void SaveExportImage(Image image, string directory, string name)
        {
            string stem = GetExportFileStem(name);
            for (int number = 1; ; number++)
            {
                string suffix = number == 1 ? "" : $"_{number}";
                string fullPath = Path.Combine(directory, $"{stem}{suffix}.png");
                if (File.Exists(fullPath) || Directory.Exists(fullPath)) continue;

                FileStream stream;
                try
                {
                    stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
                }
                catch (IOException) when (File.Exists(fullPath) || Directory.Exists(fullPath))
                {
                    // 名称检查后又被占用时换一个后缀，不能覆盖已有图片。
                    continue;
                }
                using (stream) image.Save(stream, ImageFormat.Png);
                return;
            }
        }

        private int ExportImages(TreeNode selectedNode, string path, bool saveBackground, bool saveRoute, bool saveFull)
        {
            Directory.CreateDirectory(path);

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
                        SaveExportImage(pictureBox1.BackgroundImage, path, $"{curLevelView.Name}_Background");
                        saved++;
                        backGroundSaved = true;
                    }

                    if (saveRoute && pictureBox1.Image != null)
                    {
                        SaveExportImage(pictureBox1.Image, path, $"{curLevelView.Name}_{treeNode.Parent?.Text}_R{treeNode.Index}");
                        saved++;
                    }

                    if (saveFull)
                    {
                        using Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                        pictureBox1.DrawToBitmap(bitmap, new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height));
                        SaveExportImage(bitmap, path, $"{curLevelView.Name}_{treeNode.Parent?.Text}_F{treeNode.Index}");
                        saved++;
                    }
                }

                foreach (TreeNode child in treeNode.Nodes)
                {
                    ForEachRouteNode(child);
                }
            }

            isExporting = true;
            try
            {
                ForEachRouteNode(selectedNode);
                return saved;
            }
            finally
            {
                try
                {
                    // 恢复选择时仍禁用 AfterSelect，避免改变随机分组。
                    treeView1.SelectedNode = selectedNode;
                    UpdateView();
                }
                finally
                {
                    isExporting = false;
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

                SyncTimelineControls();

                comboBox2.Items.Clear();
                for (int i = 0; i < curTimelineSimulator.WaveMaxTimes.Count; i++)
                {
                    comboBox2.Items.Add(i);
                }

                comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;
                comboBox2.SelectedIndex = curTimelineSimulator.WaveIndex >= 0 && curTimelineSimulator.WaveIndex < comboBox2.Items.Count
                    ? curTimelineSimulator.WaveIndex : -1;

                rawSetValue = false;
            }
            else
            {
                UpdateSimulationState(false);
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rawSetValue) return;
            curTimelineSimulator?.SetWaveIndex(comboBox2.SelectedIndex);
            rawSetValue = true;
            SyncTimelineControls();
            rawSetValue = false;
        }

        private void SyncTimelineControls()
        {
            numericUpDown1.Value = 0;
            trackBar1.Value = 0;
            numericUpDown1.Maximum = (decimal)curTimelineSimulator.MaxTime;
            trackBar1.Maximum = (int)(curTimelineSimulator.MaxTime * 100f);
            numericUpDown1.Value = (decimal)curTimelineSimulator.Time;
            trackBar1.Value = (int)(curTimelineSimulator.Time * 100f);
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

        private double lastSpeed = 0;
        private long lastBytes = 0;
        private DateTime lastTime = DateTime.Now;

        private async void updateGameDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> fileList;
            try
            {
                fileList = DataPaths.GetGameDataUpdateFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取游戏数据文件列表失败:\n{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"当前共有{fileList.Count}个游戏数据文件，是否全部更新？\n更新保存到程序目录下的 UserData/gamedata，完成后需要重新启动程序。\n（下载源：Github/Kengxxiao/ArknightsGameData）", "确认", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
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

            int updatedCount = 0;
            for (int i = 0; i < fileList.Count; i++)
            {
                lastBytes = 0;

                updateIndex = i + 1;
                string file = fileList[i];
                string url = $"https://raw.githubusercontent.com/Kengxxiao/ArknightsGameData/master/zh_CN/gamedata/{file}";

                string tempPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Temp/{Path.GetFileName(file)}.{Guid.NewGuid()}.tmp"); // 临时文件路径
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
                    await client.DownloadFileTaskAsync(new Uri(url), tempPath);

                    // 确保目标目录存在
                    string savePath = DataPaths.GetUserGameDataPath(file);
                    string dir = Path.GetDirectoryName(savePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    // 只更新用户数据，不修改随程序提供的默认数据。
                    File.Copy(tempPath, savePath, true);
                    File.Delete(tempPath);
                    updatedCount++;
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

            MessageBox.Show($"游戏数据更新结束：成功 {updatedCount}/{fileList.Count} 个。\n请重启程序以应用更新。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    internal sealed class MapLegend : Control
    {
        private sealed class ItemLayout
        {
            internal TileLegendEntry Entry;
            internal string Text;
            internal Rectangle Bounds;
            internal Rectangle TextBounds;
            internal readonly List<Rectangle> Swatches = new List<Rectangle>();
        }

        private const int SwatchWidth = 18;
        private const int SwatchHeight = 16;
        private const int SwatchStride = 22;
        private const TextFormatFlags TextFlags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
        private IReadOnlyList<TileLegendEntry> entries = Array.Empty<TileLegendEntry>();
        private readonly List<ItemLayout> items = new List<ItemLayout>();
        private readonly ToolTip toolTip = new ToolTip { InitialDelay = 350, ReshowDelay = 100, AutoPopDelay = 15000, ShowAlways = true };
        private bool arranging;
        private string hoverText;

        public MapLegend()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(28, 28, 28);
            ForeColor = Color.FromArgb(220, 220, 220);
            Margin = new Padding(0, 12, 0, 0);
            AccessibleName = "当前地图地块类型图例";
        }

        internal void SetEntries(IReadOnlyList<TileLegendEntry> value)
        {
            entries = value ?? Array.Empty<TileLegendEntry>();
            SetHoverText(null);
            ArrangeItems();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ArrangeItems();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ArrangeItems();
        }

        private static string DisplayName(TileLegendEntry entry) =>
            !string.IsNullOrWhiteSpace(entry.Name) ? entry.Name :
                !string.IsNullOrEmpty(entry.TileKey) ? entry.TileKey : "（未指定类型）";

        private void ArrangeItems()
        {
            if (arranging || items == null || entries == null) return;
            arranging = true;
            try
            {
                items.Clear();
                var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (TileLegendEntry entry in entries)
                {
                    string name = DisplayName(entry);
                    nameCounts[name] = nameCounts.TryGetValue(name, out int count) ? count + 1 : 1;
                }
                int available = Math.Max(1, Width - 8);
                int columns = Math.Max(1, available / Math.Max(220, Font.Height * 14));
                int cellWidth = Math.Max(1, available / columns);
                int y = 4;
                for (int first = 0; first < entries.Count; first += columns)
                {
                    int rowHeight = 0;
                    int last = Math.Min(entries.Count, first + columns);
                    for (int index = first; index < last; index++)
                    {
                        TileLegendEntry entry = entries[index];
                        string name = DisplayName(entry);
                        if (nameCounts[name] > 1 && !string.Equals(name, entry.TileKey, StringComparison.Ordinal))
                            name += " · " + entry.TileKey;
                        int x = 4 + (index - first) * cellWidth;
                        int count = entry.Variants.Count;
                        int swatchColumns = Math.Max(1, Math.Min(count, Math.Max(1, cellWidth / 2 / SwatchStride)));
                        int swatchRows = (count + swatchColumns - 1) / swatchColumns;
                        int swatchWidth = count == 0 ? 0 : swatchColumns * SwatchStride - 4;
                        int swatchHeight = swatchRows * SwatchStride;
                        int textX = x + swatchWidth + (count > 0 ? 8 : 0);
                        int textWidth = Math.Max(1, x + cellWidth - 8 - textX);
                        int textHeight = TextRenderer.MeasureText(name, Font, new Size(textWidth, int.MaxValue), TextFlags).Height;
                        int contentHeight = Math.Max(textHeight, swatchHeight);
                        var item = new ItemLayout
                        {
                            Entry = entry, Text = name,
                            Bounds = new Rectangle(x, y, Math.Max(1, cellWidth - 8), contentHeight + 8),
                            TextBounds = new Rectangle(textX, y + 4 + (contentHeight - textHeight) / 2, textWidth, textHeight),
                        };
                        for (int variant = 0; variant < count; variant++)
                            item.Swatches.Add(new Rectangle(x + variant % swatchColumns * SwatchStride,
                                y + 4 + (contentHeight - swatchHeight) / 2 + variant / swatchColumns * SwatchStride + 3,
                                SwatchWidth, SwatchHeight));
                        items.Add(item);
                        rowHeight = Math.Max(rowHeight, item.Bounds.Height);
                    }
                    y += rowHeight;
                }
                Height = entries.Count == 0 ? 0 : y + 4;
                Invalidate();
            }
            finally
            {
                arranging = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            foreach (ItemLayout item in items)
            {
                for (int index = 0; index < item.Swatches.Count; index++)
                {
                    TileLegendVariant variant = item.Entry.Variants[index];
                    Rectangle rect = item.Swatches[index];
                    using (var brush = new SolidBrush(variant.Color)) e.Graphics.FillRectangle(brush, rect);
                    if (variant.Hatched) MapDrawer.DrawDeploymentHatch(e.Graphics, rect, variant.Color);
                    using var border = new Pen(Color.FromArgb(100, ForeColor));
                    e.Graphics.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                }
                TextRenderer.DrawText(e.Graphics, item.Text, Font, item.TextBounds, ForeColor, TextFlags);
            }
        }

        private static string VariantDescription(TileLegendVariant variant)
        {
            string height = variant.HeightType == HeightType.HIGHLAND ? "高台" :
                variant.HeightType == HeightType.LOWLAND ? "地面" : "高度：" + variant.HeightType;
            return $"{height}；{(variant.CanDeploy ? "可部署" : "不可部署")}；数量：{variant.Count}";
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            foreach (ItemLayout item in items)
            {
                if (!item.Bounds.Contains(e.Location)) continue;
                var text = new StringBuilder(DisplayName(item.Entry));
                text.Append("\n类型：").Append(item.Entry.TileKey);
                int variant = item.Swatches.FindIndex(rect => rect.Contains(e.Location));
                if (variant >= 0) text.Append('\n').Append(VariantDescription(item.Entry.Variants[variant]));
                else foreach (TileLegendVariant value in item.Entry.Variants)
                        text.Append('\n').Append(VariantDescription(value));
                SetHoverText(text.ToString());
                return;
            }
            SetHoverText(null);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            SetHoverText(null);
        }

        private void SetHoverText(string text)
        {
            if (hoverText == text) return;
            hoverText = text;
            toolTip.SetToolTip(this, text);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
