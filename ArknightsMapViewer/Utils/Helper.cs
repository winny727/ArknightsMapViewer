using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace ArknightsMapViewer
{
    public static class Helper
    {
        private static Dictionary<string, JObject> gameConfigTableCache = new Dictionary<string, JObject>();

        private static JObject ReadGameConfigTable(string configPath, bool cache = false)
        {
            if (!configPath.EndsWith(".json"))
            {
                configPath += ".json";
            }
            string path = DataPaths.GetGameDataReadPath(configPath);

            try
            {
                if (gameConfigTableCache.TryGetValue(path, out var jObject))
                {
                    return jObject;
                }

                if (!File.Exists(path))
                {
                    MainForm.Instance.Log($"GameConfigTable Not Found: {path}", MainForm.LogType.Warning);
                    return null;
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                string json = File.ReadAllText(path);
                jObject = JsonConvert.DeserializeObject<JObject>(json);

                MainForm.Instance.Log($"[{Path.GetFileName(path)}] Read GameConfig Table Success ({stopwatch.Elapsed.TotalMilliseconds} ms)");

                if (cache)
                {
                    gameConfigTableCache.Add(path, jObject);
                }
                return jObject;
            }
            catch (Exception ex)
            {
                string errorMsg = $"{configPath} Parse Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
            }
            return null;
        }

        public static string[] LoadLatestFilePath()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp", "latest_files.ini");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                string errorMsg = $"latest_files.ini Read Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
            }

            return null;
        }

        public static void SaveLatestFilePath(IEnumerable<string> latestFilePath)
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string path = Path.Combine(folderPath, "latest_files.ini");

            try
            {
                File.WriteAllLines(path, latestFilePath);
            }
            catch (Exception ex)
            {
                string errorMsg = $"latest_files.ini Write Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
            }
        }

        public static void InitDrawConfig()
        {
            DrawConfig drawConfig = null;
            string path = DataPaths.GetConfigReadPath("draw_config.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    drawConfig = DrawConfigValidator.Parse(json, message => MainForm.Instance.Log($"draw_config.json: {message}", MainForm.LogType.Warning));
                }
                catch (Exception ex)
                {
                    string errorMsg = $"draw_config.json Parse Error, {ex.Message}";
                    MainForm.Instance.Log(errorMsg + "，使用默认绘图配置。", MainForm.LogType.Warning);
                    drawConfig = new DrawConfig();
                }
            }

            if (drawConfig == null)
            {
                drawConfig = new DrawConfig();
                try
                {
                    string userPath = DataPaths.GetUserConfigPath("draw_config.json");
                    if (!File.Exists(userPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(userPath));
                        string json = JsonConvert.SerializeObject(drawConfig, Formatting.Indented);
                        using var stream = new FileStream(userPath, FileMode.CreateNew, FileAccess.Write);
                        using var writer = new StreamWriter(stream);
                        writer.Write(json);
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"draw_config.json Write Error, {ex.Message}";
                    MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                }
            }

            try
            {
                using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
                float dpi = Math.Max(g.DpiX, g.DpiY) * 0.01f;
                DrawConfigValidator.Apply(drawConfig, dpi,
                    message => MainForm.Instance.Log($"draw_config.json: {message}", MainForm.LogType.Warning));
            }
            catch (Exception ex)
            {
                MainForm.Instance.Log($"draw_config.json Apply Error，保留原有绘图配置: {ex.Message}", MainForm.LogType.Warning);
            }
        }

        public static void InitTileInfoConfig()
        {
            Color? ParseColor(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                try
                {
                    return ColorTranslator.FromHtml(value);
                }
                catch (Exception)
                {
                    MainForm.Instance.Log($"Invalid Tile Color: {value}", MainForm.LogType.Warning);
                }

                return null;
            }

            string path = DataPaths.GetConfigReadPath("tile_info.txt");

            try
            {
                TableReader tableReader = new TableReader(path, Encoding.Default, '\t'); //GB2312
                tableReader.ForEach((tileKey, line) =>
                {
                    if (!string.IsNullOrEmpty(tileKey))
                    {
                        if (!GlobalDefine.TileInfo.ContainsKey(tileKey))
                        {
                            GlobalDefine.TileInfo.Add(tileKey, new TileInfo()
                            {
                                tileKey = tileKey,
                                name = line["name"],
                                description = line["description"],
                                tileColor = ParseColor(line["tileColor"]),
                                tileText = line["tileText"],
                                textColor = ParseColor(line["textColor"]),
                                comment = line["comment"],
                            });
                        }
                        else
                        {
                            string errorMsg = $"tile_info.json Parse Warning, tileKey is already exist. {tileKey}";
                            MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                string errorMsg = $"tile_info.txt Read Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                //MessageBox.Show(errorMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            var stageTable = ReadGameConfigTable("excel/stage_table.json", true);
            var tileInfos = stageTable?["tileInfo"];
            if (tileInfos != null)
            {
                foreach (var item in tileInfos)
                {
                    TileInfo tileInfo = item.ToObject<JProperty>()?.Value?.ToObject<TileInfo>();
                    if (!string.IsNullOrEmpty(tileInfo.tileKey) && !GlobalDefine.TileInfo.ContainsKey(tileInfo.tileKey))
                    {
                        MainForm.Instance.Log($"Add TileInfo From stage_table.json: {tileInfo.tileKey} [{tileInfo.name}]", MainForm.LogType.Debug);
                        GlobalDefine.TileInfo.Add(tileInfo.tileKey, tileInfo);
                    }
                    else
                    {
                        MainForm.Instance.Log($"Skip TileInfo From stage_table.json: {tileInfo.tileKey} [{tileInfo.name}]", MainForm.LogType.Debug);
                    }
                }
            }
        }

        public static void InitEnemyDatabase()
        {
            var enemyDatabase = ReadGameConfigTable("levels/enemydata/enemy_database.json");
            var enemies = enemyDatabase?["enemies"];
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    string key = enemy["Key"]?.ToString();
                    Dictionary<int, DbData> dbDatas = new Dictionary<int, DbData>();
                    if (enemy["Value"] is JArray jArray)
                    {
                        for (int i = 0; i < jArray.Count; i++)
                        {
                            int level = jArray[i]["level"].ToObject<int>();
                            DbData dbData = jArray[i]["enemyData"].ToObject<DbData>();
                            if (i > 0)
                            {
                                dbData.InheritDbData(dbDatas[0]);
                            }
                            dbDatas.Add(level, dbData);
                        }
                    }
                    if (!string.IsNullOrEmpty(key) && !GlobalDefine.EnemyDatabase.ContainsKey(key))
                    {
                        GlobalDefine.EnemyDatabase.Add(key, dbDatas);
                    }
                    else
                    {
                        string errorMsg = $"enemy_database.json Parse Error, ErrorKey: {key}";
                        MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                    }
                }
            }
        }

        public static void InitCharacterTable()
        {
            var charcterTable = ReadGameConfigTable("excel/character_table.json");
            if (charcterTable != null)
            {
                foreach (var item in charcterTable)
                {
                    string key = item.Key;
                    JToken data = item.Value;

                    CharacterData characterData = data.ToObject<CharacterData>();
                    if (!string.IsNullOrEmpty(key) && !GlobalDefine.CharacterTable.ContainsKey(key))
                    {
                        GlobalDefine.CharacterTable.Add(key, characterData);
                    }
                    else
                    {
                        string errorMsg = $"character_table.json Parse Error, ErrorKey: {key}";
                        MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                    }
                }
            }
        }

        public static void InitStageTable()
        {
            ReadStageInfos("stage_table.json", true, "stages");
            ReadStageInfos("roguelike_table.json", false, "stages"); //集成战略-刻俄柏的灰蕈迷境
            ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_1", "stages"); //集成战略-傀影与猩红孤钻
            ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_2", "stages"); //集成战略-水月与深蓝之树
            ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_3", "stages"); //集成战略-探索者的银凇止境
            ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_4", "stages"); //集成战略-萨卡兹的无终奇语
            ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_5", "stages"); //集成战略-岁的界园志异
            ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_6", "stages"); //集成战略-沉沦者的黑流树海
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_7", "stages"); //备用
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_8", "stages"); //备用
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_9", "stages"); //备用
            ReadStageInfos("sandbox_table.json", false, "sandboxActTables", "act1sandbox", "stageDatas"); //生息演算-沙中之火
            ReadStageInfos("sandbox_perm_table.json", true, "detail", "SANDBOX_V2", "sandbox_1", "stageData"); //生息演算-沙洲遗闻
            ReadStageInfos("sandbox_perm_table.json", true, "detail", "SANDBOX_V3", "sandbox_2", "stageData"); //生息演算-重启锚点
            ReadStageInfos("climb_tower_table.json", false, "levels"); //保全派驻
            ReadStageInfos("handbook_info_table.json", false, "handbookStageData"); //悖论模拟
            ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_1", "stages"); //全息作战矩阵#1
            ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_2", "stages"); //全息作战矩阵#2
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_3", "stages"); //备用
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_4", "stages"); //备用
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_5", "stages"); //备用
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_6", "stages"); //备用
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_7", "stages"); //备用
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_8", "stages"); //备用
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_9", "stages"); //备用
            ReadStageInfos("retro_table.json", false, "stageList"); //复刻常驻
            ReadStageInfos("story_review_meta_table.json", false, "trainingCampData", "stageData"); //教学
            ReadStageInfos("activity_table.json", true, "activity", "TYPE_ACT42D0", "act42d0", "stageInfoData"); //纷争演绎
            ReadStageInfos("activity_table.json", true, "activity", "TYPE_ACT42D0", "act42d0", "challengeInfoData"); //纷争演绎
            ReadStageInfos("activity_table.json", true, "actFunData", "stages"); //四月辑录

            //危机合约 多维合作 stage_info.txt
            string path = DataPaths.GetConfigReadPath("stage_info.txt");
            try
            {
                TableReader tableReader = new TableReader(path, Encoding.Default, '\t'); //GB2312
                tableReader.ForEach((stageId, line) =>
                {
                    if (!string.IsNullOrEmpty(stageId))
                    {
                        StageInfo stageData = new StageInfo()
                        {
                            stageId = stageId,
                            code = line["code"],
                            zoneId = line["zoneId"],
                            levelId = line["levelId"],
                            name = line["name"],
                            description = line["description"]
                        };
                        AddStageInfo(stageData);
                    }
                });
            }
            catch (Exception ex)
            {
                string errorMsg = $"stage_info.txt Read Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                //MessageBox.Show(errorMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void ClearGameConfigTableCache(string key = null)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            if (string.IsNullOrEmpty(key))
            {
                gameConfigTableCache.Clear();
            }
            else if (gameConfigTableCache.ContainsKey(key))
            {
                gameConfigTableCache.Remove(key);
            }

            GC.Collect();
            stopwatch.Stop();

            string keyText = string.IsNullOrEmpty(key) ? "All" : key;
            MainForm.Instance.Log($"Clear GameConfigTable Cache [{keyText}] ({stopwatch.Elapsed.TotalMilliseconds} ms)", MainForm.LogType.Log);
        }

        private static void ReadStageInfos(string configName, bool cache, params string[] stageFieldPath)
        {
            if (string.IsNullOrEmpty(configName))
            {
                return;
            }

            var configTable = ReadGameConfigTable("excel/" + configName, cache);
            JToken stages = configTable?.SelectToken(string.Join(".", stageFieldPath));
            if (stages == null)
            {
                MainForm.Instance.Log($"StageInfo Not Found: {configName} {string.Join(".", stageFieldPath)}", MainForm.LogType.Warning);
                return;
            }

            foreach (var item in stages)
            {
                JToken jValue = item.ToObject<JProperty>()?.Value;
                StageInfo stageInfo = jValue?.ToObject<StageInfo>();
                if (stageInfo == null)
                {
                    continue;
                }

                stageInfo.stageId ??= jValue?["id"]?.ToString();
                stageInfo.name ??= jValue?["levelName"]?.ToString();
                stageInfo.code ??= jValue?["levelCode"]?.ToString();
                stageInfo.description ??= jValue?["desc"]?.ToString() ?? jValue?["levelDesc"]?.ToString();

                if (!string.IsNullOrEmpty(stageInfo.stageId))
                {
                    AddStageInfo(stageInfo);
                }
            }
        }

        private static void AddStageInfo(StageInfo stageInfo)
        {
            if (!GlobalDefine.StageInfo.TryGetValue(stageInfo.stageId, out var stages))
            {
                stages = new List<StageInfo>();
                GlobalDefine.StageInfo.Add(stageInfo.stageId, stages);
            }
            stages.Add(stageInfo);
        }

        /// <summary>
        /// 获取缩写
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string GetAbbreviation(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 使用正则表达式分割所有空白字符
            string[] words = Regex.Split(input.Trim(), @"\s+");
            StringBuilder abbreviation = new StringBuilder();

            foreach (string word in words)
            {
                foreach (char c in word)
                {
                    if (char.IsLetter(c))
                    {
                        abbreviation.Append(char.ToUpper(c));
                        break; // 只取第一个字母字符
                    }
                }
            }

            return abbreviation.ToString();
        }

        public static Bitmap CreateBitmap(LevelData levelData)
        {
            if (levelData == null)
            {
                return null;
            }

            int width = levelData.mapWidth * GlobalDefine.TILE_PIXLE;
            int height = levelData.mapHeight * GlobalDefine.TILE_PIXLE;

            return new Bitmap(width, height);
        }

        /// <summary>
        /// 地图障碍，数字表示cost，int.MaxValue表示障碍
        /// </summary>
        /// <param name="levelData"></param>
        /// <returns></returns>
        public static int[,] GetGridArray(LevelData levelData)
        {
            Tile[,] map = levelData.map;
            int mapWidth = levelData.mapWidth;
            int mapHeight = levelData.mapHeight;
            int[,] grid = new int[mapWidth, mapHeight];
            for (int row = 0; row < mapHeight; row++)
            {
                for (int col = 0; col < mapWidth; col++)
                {
                    Tile tile = map[col, row];
                    int cost = 1;
                    if (tile.passableMask == PassableMask.FLY_ONLY || tile.passableMask == PassableMask.NONE)
                    {
                        cost = int.MaxValue;
                    }
                    if (tile.tileKey == "tile_hole")
                    {
                        cost = 1000000;
                    }
                    grid[col, row] = cost;
                }
            }

            return grid;
        }

        public static bool[,] GetIsBarrier(int[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            bool[,] isBarrier = new bool[width, height];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    isBarrier[col, row] = grid[col, row] > 1;
                }
            }

            return isBarrier;
        }

        private static readonly float FloatMinNormal = 1.17549435E-38f;
        private static readonly float FloatMinDenormal = float.Epsilon;
        private static readonly bool IsFlushToZeroEnabled = FloatMinDenormal == 0f;
        private static readonly float Epsilon = (IsFlushToZeroEnabled ? FloatMinNormal : FloatMinDenormal);

        //copy from unity Mathf.Approximately
        public static bool Approximately(float a, float b)
        {
            return Math.Abs(b - a) < Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), Epsilon * 8f);
        }

        //曼哈顿距离
        public static float ManhattanDistance(Vector2 p1, Vector2 p2)
        {
            return Math.Abs(p1.x - p2.x) + Math.Abs(p1.y - p2.y);
        }

        /// <summary>
        /// 在四邻接格子组成的通道内拉直路径，默认使用首尾格中心。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> StringPulling(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0) return new List<Vector2>();
            return StringPulling(path, PositionToVector2(path[0]), PositionToVector2(path[path.Count - 1]));
        }

        /// <summary>
        /// 使用实际首尾坐标的完整漏斗算法。门户取相邻格的共享边，允许贴边但不扩张到通道外。
        /// 无效通道或端点不在首尾格内时返回空列表，由调用方选择安全回退并校验碰撞。
        /// </summary>
        public static List<Vector2> StringPulling(List<Vector2Int> path, Vector2 startPos, Vector2 endPos)
        {
            var result = new List<Vector2>();
            if (path == null || path.Count == 0 || !IsFinite(startPos.x) || !IsFinite(startPos.y) ||
                !IsFinite(endPos.x) || !IsFinite(endPos.y)) return result;

            bool InCell(Vector2 point, Vector2Int cell) =>
                point.x >= cell.x && point.x <= cell.x + 1d && point.y >= cell.y && point.y <= cell.y + 1d;
            if (!InCell(startPos, path[0]) || !InCell(endPos, path[path.Count - 1])) return result;

            var leftPortals = new List<Vector2> { startPos };
            var rightPortals = new List<Vector2> { startPos };
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int from = path[i - 1];
                Vector2Int to = path[i];
                long dx = (long)to.x - from.x;
                long dy = (long)to.y - from.y;
                if (dx == 0 && dy == 0) continue;
                if (Math.Abs(dx) + Math.Abs(dy) != 1) return result;

                // 面向行进方向，左端点为共享边中心加左法向量的半格偏移。
                Vector2 middle = new Vector2(from.x + 0.5f + dx * 0.5f, from.y + 0.5f + dy * 0.5f);
                Vector2 side = new Vector2(-dy * 0.5f, dx * 0.5f);
                leftPortals.Add(middle + side);
                rightPortals.Add(middle - side);
            }
            leftPortals.Add(endPos);
            rightPortals.Add(endPos);

            double Area(Vector2 a, Vector2 b, Vector2 c) =>
                ((double)b.x - a.x) * ((double)c.y - a.y) - ((double)b.y - a.y) * ((double)c.x - a.x);
            void AddPoint(Vector2 point)
            {
                if (result.Count == 0 || !result[result.Count - 1].Equals(point)) result.Add(point);
            }

            Vector2 apex = startPos;
            Vector2 left = apex;
            Vector2 right = apex;
            int apexIndex = 0, leftIndex = 0, rightIndex = 0;
            AddPoint(apex);

            // 终点的退化门户也必须参与收紧；转弯后从新顶点所在门户重新扫描。
            for (int i = 1; i < leftPortals.Count; i++)
            {
                Vector2 nextLeft = leftPortals[i];
                Vector2 nextRight = rightPortals[i];
                if (Area(apex, right, nextRight) >= 0)
                {
                    if (apex.Equals(right) || Area(apex, left, nextRight) < 0)
                    {
                        right = nextRight;
                        rightIndex = i;
                    }
                    else
                    {
                        AddPoint(left);
                        apex = left;
                        apexIndex = leftIndex;
                        left = right = apex;
                        leftIndex = rightIndex = apexIndex;
                        i = apexIndex;
                        continue;
                    }
                }

                if (Area(apex, left, nextLeft) <= 0)
                {
                    if (apex.Equals(left) || Area(apex, right, nextLeft) > 0)
                    {
                        left = nextLeft;
                        leftIndex = i;
                    }
                    else
                    {
                        AddPoint(right);
                        apex = right;
                        apexIndex = rightIndex;
                        left = right = apex;
                        leftIndex = rightIndex = apexIndex;
                        i = apexIndex;
                        continue;
                    }
                }
            }

            AddPoint(endPos);
            return result;
        }

        /// <summary>
        /// 寻路路径优化 By：Deepseek
        /// </summary>
        /// <param name="path"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static List<Vector2Int> OptimizePath(List<Vector2Int> path, bool[,] grid)
        {
            if (path == null) return null;

            int n = path.Count;
            if (n == 0) return new List<Vector2Int>();

            int[] dp = new int[n];
            int[] prev = new int[n]; // 前驱节点索引
            for (int i = 0; i < n; i++)
            {
                dp[i] = int.MaxValue;
                prev[i] = -1;
            }
            dp[0] = 0;

            for (int i = 1; i < n; i++)
            {
                // 常规移动：从i-1移动一步
                if (dp[i - 1] != int.MaxValue)
                {
                    dp[i] = dp[i - 1] + 1;
                    prev[i] = i - 1; // 记录前驱
                }

                // 检查所有可能的前向跳跃
                for (int j = 0; j < i; j++)
                {
                    if (dp[j] != int.MaxValue &&
                        !HasCollider(PositionToVector2(path[i]), PositionToVector2(path[j]), grid) &&
                        dp[j] + 1 < dp[i])
                    {
                        dp[i] = dp[j] + 1;
                        prev[i] = j; // 更新为更优前驱
                    }
                }
            }

            // 回溯重建路径
            List<Vector2Int> optimizedPath = new List<Vector2Int>();
            int current = path.Count - 1;

            if (prev[current] == -1) return path; // 无法优化时返回原路径

            // 反向追踪前驱节点
            while (current != -1)
            {
                optimizedPath.Add(path[current]);
                current = prev[current];
            }

            // 反转得到正确顺序
            optimizedPath.Reverse();
            return optimizedPath;
        }

        /// <summary>
        /// 从实际端点拉紧路径通道，只保留绕障碍必需的转折。
        /// 绘制中心线允许贴墙；保留格中心路径作为安全候选及必要高代价格的连接。
        /// </summary>
        public static List<Vector2> OptimizeRoutePath(List<Vector2Int> path, int[,] grid, Vector2 startOffset, Vector2 endOffset)
        {
            var result = new List<Vector2>();
            if (path == null || path.Count == 0) return result;

            var points = new List<Vector2>();
            void AddPoint(Vector2 point)
            {
                if (points.Count == 0 || !points[points.Count - 1].Equals(point)) points.Add(point);
            }

            AddPoint(PositionToVector2(path[0]) + startOffset);
            foreach (Vector2Int position in path) AddPoint(PositionToVector2(position));
            AddPoint(PositionToVector2(path[path.Count - 1]) + endOffset);

            bool[,] barriers = new bool[grid.GetLength(0), grid.GetLength(1)];
            bool[,] shortcutBarriers = new bool[grid.GetLength(0), grid.GetLength(1)];
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    barriers[x, y] = grid[x, y] == int.MaxValue;
                    shortcutBarriers[x, y] = grid[x, y] > 1;
                }
            }

            double Distance(Vector2 a, Vector2 b)
            {
                double dx = (double)a.x - b.x;
                double dy = (double)a.y - b.y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            List<Vector2> Shorten(List<Vector2> candidates, bool allowOriginalEdges)
            {
                var shortened = new List<Vector2>();
                if (candidates == null || candidates.Count == 0 ||
                    HasCollider(candidates[0], candidates[0], barriers, 0)) return shortened;

                double[] lengths = new double[candidates.Count];
                int[] segments = new int[candidates.Count];
                int[] previous = new int[candidates.Count];
                for (int i = 0; i < candidates.Count; i++)
                {
                    lengths[i] = double.PositiveInfinity;
                    segments[i] = int.MaxValue;
                    previous[i] = -1;
                }
                lengths[0] = 0;
                segments[0] = 0;
                for (int i = 1; i < candidates.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (double.IsPositiveInfinity(lengths[j])) continue;
                        double length = lengths[j] + Distance(candidates[i], candidates[j]);
                        if (length > lengths[i] + 1e-9 ||
                            (Math.Abs(length - lengths[i]) <= 1e-9 && segments[j] + 1 >= segments[i])) continue;
                        bool originalEdge = allowOriginalEdges && j == i - 1;
                        if (HasCollider(candidates[j], candidates[i], originalEdge ? barriers : shortcutBarriers, 0)) continue;
                        lengths[i] = length;
                        segments[i] = segments[j] + 1;
                        previous[i] = j;
                    }
                }

                if (double.IsPositiveInfinity(lengths[candidates.Count - 1])) return shortened;
                for (int i = candidates.Count - 1; i >= 0; i = previous[i]) shortened.Add(candidates[i]);
                shortened.Reverse();
                return shortened;
            }

            double Length(List<Vector2> route)
            {
                if (route.Count == 0) return double.PositiveInfinity;
                double length = 0;
                for (int i = 1; i < route.Count; i++) length += Distance(route[i], route[i - 1]);
                return length;
            }

            var fallback = Shorten(points, true);
            var pulled = Shorten(StringPulling(path, points[0], points[points.Count - 1]), false);
            // 共线门户可能保留不同的等长拐点；双向拉绳后再选短线，避免捷径受遍历方向影响。
            var reversePath = new List<Vector2Int>(path);
            reversePath.Reverse();
            var reversePulled = StringPulling(reversePath, points[points.Count - 1], points[0]);
            reversePulled.Reverse();
            reversePulled = Shorten(reversePulled, false);
            double pulledLength = Length(pulled);
            double reverseLength = Length(reversePulled);
            if (reverseLength < pulledLength ||
                (reverseLength == pulledLength && reversePulled.Count < pulled.Count))
            {
                pulled = reversePulled;
                pulledLength = reverseLength;
            }
            double fallbackLength = Length(fallback);
            return pulledLength < fallbackLength ||
                (pulledLength == fallbackLength && pulled.Count < fallback.Count) ? pulled : fallback;
        }

        /// <summary>
        /// 判断线段及其半径是否碰到障碍格；半径为0时只检查障碍内部，允许从边界向外移动。
        /// </summary>
        /// <param name="startPos">起点</param>
        /// <param name="endPos">终点</param>
        /// <returns></returns>
        public static bool HasCollider(Vector2 startPos, Vector2 endPos, bool[,] isBarrier, float characterRadius = 0.2f)
        {
            if (!IsFinite(startPos.x) || !IsFinite(startPos.y) || !IsFinite(endPos.x) ||
                !IsFinite(endPos.y) || !IsFinite(characterRadius)) return true;

            double radius = Math.Max(0, characterRadius);
            double minX = Math.Max(0, Math.Ceiling(Math.Min(startPos.x, endPos.x) - radius) - 1);
            double minY = Math.Max(0, Math.Ceiling(Math.Min(startPos.y, endPos.y) - radius) - 1);
            double maxX = Math.Min(isBarrier.GetLength(0) - 1, Math.Floor(Math.Max(startPos.x, endPos.x) + radius));
            double maxY = Math.Min(isBarrier.GetLength(1) - 1, Math.Floor(Math.Max(startPos.y, endPos.y) + radius));
            if (minX > maxX || minY > maxY) return false;

            for (int x = (int)minX; x <= (int)maxX; x++)
            {
                for (int y = (int)minY; y <= (int)maxY; y++)
                {
                    if (!isBarrier[x, y]) continue;
                    double enter = 0, exit = 1;
                    bool interiorOnly = radius == 0;
                    if (ClipSegmentAxis(startPos.x, (double)endPos.x - startPos.x, x, x + 1, interiorOnly, ref enter, ref exit) &&
                        ClipSegmentAxis(startPos.y, (double)endPos.y - startPos.y, y, y + 1, interiorOnly, ref enter, ref exit) &&
                        (!interiorOnly || enter < exit)) return true;

                    if (radius <= 0)
                    {
                        // 对角相接的障碍没有可走的缝；顶点作为折点也不能绕过这一限制。
                        for (int dx = -1; dx <= 1; dx += 2)
                        {
                            for (int dy = -1; dy <= 1; dy += 2)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= isBarrier.GetLength(0) || ny >= isBarrier.GetLength(1) ||
                                    !isBarrier[nx, ny] || isBarrier[nx, y] || isBarrier[x, ny]) continue;
                                int cornerX = x + (dx > 0 ? 1 : 0);
                                int cornerY = y + (dy > 0 ? 1 : 0);
                                if (PointSegmentDistanceSquared(cornerX, cornerY, startPos, endPos) <= 1e-18) return true;
                            }
                        }
                        // 两个障碍格的公共边仍在墙内，不能把沿公共边的线当成贴外墙移动。
                        if (startPos.x == endPos.x && startPos.y != endPos.y &&
                            ((startPos.x == x && x > 0 && isBarrier[x - 1, y]) ||
                             (startPos.x == x + 1 && x + 1 < isBarrier.GetLength(0) && isBarrier[x + 1, y])))
                        {
                            enter = 0; exit = 1;
                            if (ClipSegmentAxis(startPos.y, (double)endPos.y - startPos.y, y, y + 1, true, ref enter, ref exit) && enter < exit) return true;
                        }
                        if (startPos.y == endPos.y && startPos.x != endPos.x &&
                            ((startPos.y == y && y > 0 && isBarrier[x, y - 1]) ||
                             (startPos.y == y + 1 && y + 1 < isBarrier.GetLength(1) && isBarrier[x, y + 1])))
                        {
                            enter = 0; exit = 1;
                            if (ClipSegmentAxis(startPos.x, (double)endPos.x - startPos.x, x, x + 1, true, ref enter, ref exit) && enter < exit) return true;
                        }
                        continue;
                    }
                    double distanceSquared = Math.Min(PointRectangleDistanceSquared(startPos, x, y), PointRectangleDistanceSquared(endPos, x, y));
                    distanceSquared = Math.Min(distanceSquared, PointSegmentDistanceSquared(x, y, startPos, endPos));
                    distanceSquared = Math.Min(distanceSquared, PointSegmentDistanceSquared(x + 1, y, startPos, endPos));
                    distanceSquared = Math.Min(distanceSquared, PointSegmentDistanceSquared(x, y + 1, startPos, endPos));
                    distanceSquared = Math.Min(distanceSquared, PointSegmentDistanceSquared(x + 1, y + 1, startPos, endPos));
                    if (distanceSquared <= radius * radius) return true;
                }
            }
            return false;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool ClipSegmentAxis(double start, double delta, double minimum, double maximum, bool interiorOnly, ref double enter, ref double exit)
        {
            if (delta == 0) return interiorOnly ? start > minimum && start < maximum : start >= minimum && start <= maximum;
            double first = (minimum - start) / delta;
            double last = (maximum - start) / delta;
            if (first > last)
            {
                double swap = first;
                first = last;
                last = swap;
            }
            enter = Math.Max(enter, first);
            exit = Math.Min(exit, last);
            return enter <= exit;
        }

        private static double PointRectangleDistanceSquared(Vector2 point, int x, int y)
        {
            double dx = Math.Max(Math.Max(x - (double)point.x, 0), point.x - (double)x - 1);
            double dy = Math.Max(Math.Max(y - (double)point.y, 0), point.y - (double)y - 1);
            return dx * dx + dy * dy;
        }

        private static double PointSegmentDistanceSquared(double x, double y, Vector2 start, Vector2 end)
        {
            double dx = (double)end.x - start.x;
            double dy = (double)end.y - start.y;
            double lengthSquared = dx * dx + dy * dy;
            double t = lengthSquared == 0 ? 0 : ((x - start.x) * dx + (y - start.y) * dy) / lengthSquared;
            t = Math.Max(0, Math.Min(1, t));
            double distanceX = x - (start.x + t * dx);
            double distanceY = y - (start.y + t * dy);
            return distanceX * distanceX + distanceY * distanceY;
        }

        #region 判断两条线是否相交
        /// <summary> 
        /// 判断两条线是否相交
        /// </summary> 
        /// <param name="a"> 线段1起点坐标 </param> 
        /// <param name="b"> 线段1终点坐标 </param> 
        /// <param name="c"> 线段2起点坐标 </param> 
        /// <param name="d"> 线段2终点坐标 </param> 
        /// <param name="intersection"> 相交点坐标 </param> 
        /// <returns> 是否相交 0:两线平行 -1:不平行且未相交 1:两线相交 </returns> 
        public static bool GetIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            // 判断异常 
            if (Math.Abs(b.y - a.y) + Math.Abs(b.x - a.x) + Math.Abs(d.y - c.y) + Math.Abs(d.x - c.x) == 0)
            {
                return false;
            }

            if (Math.Abs(b.y - a.y) + Math.Abs(b.x - a.x) == 0)
            {
                return false;
            }

            if ((b.y - a.y) * (c.x - d.x) - (b.x - a.x) * (c.y - d.y) == 0)
            {
                //线段平行，无交点
                return false;
            }
            Vector2 contractPoint = new Vector2()
            {
                x = ((b.x - a.x) * (c.x - d.x) * (c.y - a.y) -
                c.x * (b.x - a.x) * (c.y - d.y) + a.x * (b.y - a.y) * (c.x - d.x)) /
                ((b.y - a.y) * (c.x - d.x) - (b.x - a.x) * (c.y - d.y)),
                y = ((b.y - a.y) * (c.y - d.y) * (c.x - a.x) - c.y
                    * (b.y - a.y) * (c.x - d.x) + a.y * (b.x - a.x) * (c.y - d.y))
                    / ((b.x - a.x) * (c.y - d.y) - (b.y - a.y) * (c.x - d.x))
            };

            return (contractPoint.x - a.x) * (contractPoint.x - b.x) <= 0
                    && (contractPoint.x - c.x) * (contractPoint.x - d.x) <= 0
                    && (contractPoint.y - a.y) * (contractPoint.y - b.y) <= 0
                    && (contractPoint.y - c.y) * (contractPoint.y - d.y) <= 0;
        }
        #endregion

        public static Point PositionToPoint(Position position, Offset offset, int mapHeight)
        {
            int x = (int)((position.col + offset.x + 0.5f) * GlobalDefine.TILE_PIXLE);
            //int y = (int)((position.row + offset.y + 0.5f) * GlobalDefine.TILE_PIXLE);
            int y = (int)((mapHeight - (position.row + offset.y) - 1 + 0.5f) * GlobalDefine.TILE_PIXLE);
            return new Point(x, y);
        }

        public static Position PointToPosition(Point point, int mapHeight)
        {
            int x = point.X / GlobalDefine.TILE_PIXLE;
            //int y = point.Y / GlobalDefine.TILE_PIXLE;
            int y = -(point.Y / GlobalDefine.TILE_PIXLE) + mapHeight - 1;
            return new Position
            {
                col = x,
                row = y,
            };
        }

        public static Vector2 PositionToVector2(Vector2Int position)
        {
            float x = position.x + 0.5f;
            float y = position.y + 0.5f;
            return new Vector2(x, y);
        }

        public static Vector2 PositionToVector2(Position position, Offset offset)
        {
            float x = position.col + offset.x + 0.5f;
            float y = position.row + offset.y + 0.5f;
            return new Vector2(x, y);
        }

        public static Point Vector2ToPoint(Vector2 vector, int mapHeight)
        {
            int x = (int)(vector.x * GlobalDefine.TILE_PIXLE);
            //int y = (int)(vector.y * GlobalDefine.TILE_PIXLE);
            int y = (int)((mapHeight - vector.y) * GlobalDefine.TILE_PIXLE);
            return new Point(x, y);
        }

        #region Extension Method

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        #endregion

        #region

        public static void SelectExplorerFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }
            filePath = Path.GetFullPath(filePath);

            //Process.Start("explorer.exe", $"/select,\"{filePath}\""); //容易被360误报，很坑

            //打开资源管理器并选中文件
            IntPtr pidl = ILCreateFromPath(filePath);
            if (pidl != IntPtr.Zero)
            {
                SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
                ILFree(pidl);
            }
            else
            {
                MessageBox.Show($"目录打开失败: {filePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In] IntPtr apidl,
            uint dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPTStr)] string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidl);

        #endregion
    }
}
