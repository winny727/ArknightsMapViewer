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
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/gamedata", configPath);

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
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "draw_config.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    drawConfig = JsonConvert.DeserializeObject<DrawConfig>(json);
                }
                catch (Exception ex)
                {
                    string errorMsg = $"draw_config.json Parse Error, {ex.Message}";
                    MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
                }
            }

            if (drawConfig == null)
            {
                drawConfig = new DrawConfig();
                try
                {
                    string json = JsonConvert.SerializeObject(drawConfig, Formatting.Indented);
                    File.WriteAllText(path, json);
                }
                catch (Exception ex)
                {
                    string errorMsg = $"draw_config.json Write Error, {ex.Message}";
                    MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
                }
            }

            using Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            float dpi = Math.Max(g.DpiX, g.DpiY) * 0.01f;

            GlobalDefine.TILE_PIXLE = (int)(drawConfig.Size.tilePixle * dpi);
            GlobalDefine.LINE_WIDTH = (int)(drawConfig.Size.lineWidth * dpi);
            GlobalDefine.CIRCLE_EDGE_WIDTH = (int)(drawConfig.Size.circleEdgeWidth * dpi);
            GlobalDefine.CIRCLE_RADIUS = (int)(drawConfig.Size.circleRadius * dpi);
            GlobalDefine.POINT_RADIUS = (int)(drawConfig.Size.pointRadius * dpi);

            Enum.TryParse(drawConfig.Font.textFontStyle, true, out FontStyle textFontStyle);
            Enum.TryParse(drawConfig.Font.indexFontStyle, true, out FontStyle indexFontStyle);
            Enum.TryParse(drawConfig.Font.timeFontStyle, true, out FontStyle timeFontStyle);
            Enum.TryParse(drawConfig.Font.lenghtFontStyle, true, out FontStyle lenghtFont);
            Enum.TryParse(drawConfig.Font.predefinedFontStyle, true, out FontStyle predefinedFont);

            GlobalDefine.TEXT_FONT = new Font(drawConfig.Font.textFont, drawConfig.Font.textFontSize, textFontStyle);
            GlobalDefine.INDEX_FONT = new Font(drawConfig.Font.indexFont, drawConfig.Font.indexFontSize, indexFontStyle);
            GlobalDefine.TIME_FONT = new Font(drawConfig.Font.timeFont, drawConfig.Font.timeFontSize, timeFontStyle);
            GlobalDefine.LENGTH_FONT = new Font(drawConfig.Font.lenghtFont, drawConfig.Font.lenghtFontSize, lenghtFont);
            GlobalDefine.PREDEFINED_FONT = new Font(drawConfig.Font.predefinedFont, drawConfig.Font.predefinedFontSize, predefinedFont);
            GlobalDefine.TEXT_COLOR = ColorTranslator.FromHtml(drawConfig.Color.textColor);
            GlobalDefine.LINE_COLOR = ColorTranslator.FromHtml(drawConfig.Color.lineColor);
            GlobalDefine.CIRCLE_COLOR = ColorTranslator.FromHtml(drawConfig.Color.circleColor);
            GlobalDefine.LENGTH_COLOR = ColorTranslator.FromHtml(drawConfig.Color.lengthColor);
            GlobalDefine.DEFAULT_ROAD_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultRoadColor);
            GlobalDefine.DEFAULT_WALL_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultWallColor);
            GlobalDefine.DEFAULT_FLOOR_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultFloorColor);
            GlobalDefine.DEFAULT_FORBIDDEN_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultForbiddenColor);
            GlobalDefine.PREDEFINED_LINECOLOR = ColorTranslator.FromHtml(drawConfig.Color.predefinedForeColor);
            GlobalDefine.PREDEFINED_BACKCOLOR = ColorTranslator.FromHtml(drawConfig.Color.predefinedBackColor);
            GlobalDefine.PREDEFINED_SELECTED_BACKCOLOR = ColorTranslator.FromHtml(drawConfig.Color.predefinedSelectedBackColor);
            GlobalDefine.PREDEFINED_SELECTED_TEXTCOLOR = ColorTranslator.FromHtml(drawConfig.Color.predefinedSelectedTextColor);
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

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "tile_info.txt");

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
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_6", "stages"); //备用
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_7", "stages"); //备用
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_8", "stages"); //备用
            //ReadStageInfos("roguelike_topic_table.json", true, "details", "rogue_9", "stages"); //备用
            ReadStageInfos("sandbox_table.json", false, "sandboxActTables", "act1sandbox", "stageDatas"); //生息演算-沙中之火
            ReadStageInfos("sandbox_perm_table.json", false, "detail", "SANDBOX_V2", "sandbox_1", "stageData"); //生息演算-沙洲遗闻
            ReadStageInfos("climb_tower_table.json", false, "levels"); //保全派驻
            ReadStageInfos("handbook_info_table.json", false, "handbookStageData"); //悖论模拟
            ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_1", "stages"); //全息作战矩阵#1
            //ReadStageInfos("crisis_v2_table.json", true, "recalRuneData", "seasons", "recalRune_season_2", "stages"); //备用
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
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "stage_info.txt");
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
                        if (!GlobalDefine.StageInfo.ContainsKey(stageData.stageId))
                        {
                            GlobalDefine.StageInfo.Add(stageData.stageId, new List<StageInfo>());
                        }
                        GlobalDefine.StageInfo[stageData.stageId].Add(stageData);
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
                    if (!GlobalDefine.StageInfo.ContainsKey(stageInfo.stageId))
                    {
                        GlobalDefine.StageInfo.Add(stageInfo.stageId, new List<StageInfo>());
                    }
                    GlobalDefine.StageInfo[stageInfo.stageId].Add(stageInfo);
                }
            }
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
        /// 拉绳（漏斗）算法，优化相连的方格的路径的行走位置的优化（贴边走） TODO 推广到优化后的不相连路径
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> StringPulling(List<Vector2Int> path)
        {
            if (path == null)
            {
                return null;
            }

            if (path.Count >= 2)
            {
                //一次遍历获取所有左右边
                float offset = 0.4f; //距正方形中心距离
                List<Vector2> leftVertexs = new List<Vector2>();
                List<Vector2> rightVertexs = new List<Vector2>();
                Vector2 startPos = new Vector2
                {
                    x = path[0].x + 0.5f,
                    y = path[0].y + 0.5f,
                };
                Vector2 endPos = new Vector2
                {
                    x = path[path.Count - 1].x + 0.5f,
                    y = path[path.Count - 1].y + 0.5f,
                };

                leftVertexs.Add(startPos);
                rightVertexs.Add(startPos);
                for (int i = 1; i < path.Count; i++)
                {
                    Vector2Int curPoint = path[i];
                    Vector2Int prevPoint = path[i - 1];

                    if (prevPoint.y == curPoint.y)
                    {
                        //x方向移动
                        int dir = curPoint.x - prevPoint.x;
                        float vertexOffset = offset * (curPoint.x - prevPoint.x);
                        leftVertexs.Add(new Vector2(curPoint.x + 0.5f - dir * 0.5f, curPoint.y + vertexOffset + 0.5f));
                        rightVertexs.Add(new Vector2(curPoint.x + 0.5f - dir * 0.5f, curPoint.y - vertexOffset + 0.5f));
                    }
                    else if (prevPoint.x == curPoint.x)
                    {
                        //y方向移动
                        int dir = curPoint.y - prevPoint.y;
                        float vertexOffset = offset * (curPoint.y - prevPoint.y);
                        leftVertexs.Add(new Vector2(curPoint.x - vertexOffset + 0.5f, curPoint.y + 0.5f - dir * 0.5f));
                        rightVertexs.Add(new Vector2(curPoint.x + vertexOffset + 0.5f, curPoint.y + 0.5f - dir * 0.5f));
                    }
                }
                leftVertexs.Add(endPos);
                rightVertexs.Add(endPos);

                List<Vector2> newPath = new List<Vector2>();
                Vector2 apex = leftVertexs[0];
                Vector2 left = apex;
                Vector2 right = apex;

                newPath.Add(apex);

                for (int i = 1; i < path.Count; i++)
                {
                    Vector2 curLeft = leftVertexs[i];
                    Vector2 curRight = rightVertexs[i];

                    //判断当前左方向在右方向的左侧
                    if (Vector2.Cross(curLeft - apex, right - apex) <= 0)
                    {
                        //判断新的左方向在原左方向的右侧（收窄）
                        if (apex == left || Vector2.Cross(curLeft - apex, left - apex) >= 0)
                        {
                            left = curLeft;
                        }
                        else
                        {
                            apex = left;
                            left = apex;
                            right = apex;
                            newPath.Add(apex);
                        }
                    }

                    //判断当前右方向在左方向的右侧
                    if (Vector2.Cross(curRight - apex, left - apex) >= 0)
                    {
                        //判断新的右方向在原右方向的左侧（收窄）
                        if (apex == right || Vector2.Cross(curRight - apex, right - apex) <= 0)
                        {
                            right = curRight;
                        }
                        else
                        {
                            apex = right;
                            left = apex;
                            right = apex;
                            newPath.Add(apex);
                        }
                    }
                }

                newPath.Add(endPos);

                return newPath;
            }

            return null;
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
        /// 判断两点之间有无碰撞体，类似射线
        /// </summary>
        /// <param name="startPos">起点</param>
        /// <param name="endPos">终点</param>
        /// <returns></returns>
        public static bool HasCollider(Vector2 startPos, Vector2 endPos, bool[,] isBarrier, float characterRadius = 0.2f)
        {
            int mapWidth = isBarrier.GetLength(0);
            int mapHeight = isBarrier.GetLength(1);

            //xy都相等则为同一点
            if (startPos.x == endPos.x && startPos.y == endPos.y)
            {
                if (isBarrier[(int)startPos.x, (int)startPos.y]) return true;
                else return false;
            }
            //若为竖直
            else if (startPos.x == endPos.x)
            {
                for (int i = 1; i < Math.Abs(endPos.y - startPos.y); i++)
                {
                    if (isBarrier[(int)startPos.x,
                        (int)(startPos.y + i * Math.Sign(endPos.y - startPos.y))]) return true;
                }
                return false;
            }
            //若为水平
            else if (startPos.y == endPos.y)
            {
                for (int i = 1; i < Math.Abs(endPos.x - startPos.x); i++)
                {
                    if (isBarrier[(int)(startPos.x + i * Math.Sign(endPos.x - startPos.x)),
                        (int)startPos.y]) return true;
                }
                return false;
            }
            //若为倾斜
            else
            {
                float deltax = endPos.x - startPos.x;
                float deltay = endPos.y - startPos.y;
                //法向单位向量
                Vector2 verticalUnit = new Vector2(deltay, -deltax).normalized;

                //检测两点连线之间是否有相交；同时检测两条偏移平行线，以消除敌人模型半径穿模影响
                for (int v = -1; v <= 1; v++)
                {
                    //偏移平行线，将点往法方向偏移固定距离
                    Vector2 startOffset = startPos + verticalUnit * v * characterRadius;
                    Vector2 endOffset = endPos + verticalUnit * v * characterRadius;

                    //遍历两点所作矩形之间的所有小正方形再加外围一圈小正方形
                    for (int i = -1; i <= Math.Abs(deltax) + 1; i++)
                    {
                        for (int j = -1; j <= Math.Abs(deltay) + 1; j++)
                        {
                            int rectx = (int)startPos.x + i * (int)Math.Sign(deltax);
                            int recty = (int)startPos.y + j * (int)Math.Sign(deltay);
                            //若这个方形不可穿过
                            if (rectx >= 0 && rectx < mapWidth &&
                                recty < mapHeight && recty >= 0 &&
                                isBarrier[rectx, recty])
                            {
                                //MainForm.Instance.DebugDrawLine(startOffset, endOffset, Color.Green);
                                //MainForm.Instance.DebugDrawLine(new Vector2(rectx, recty), new Vector2(rectx, recty + 1f), Color.Red);
                                //MainForm.Instance.DebugDrawLine(new Vector2(rectx, recty + 1f), new Vector2(rectx + 1f, recty + 1f), Color.Red);
                                //MainForm.Instance.DebugDrawLine(new Vector2(rectx + 1f, recty + 1f), new Vector2(rectx + 1f, recty), Color.Red);
                                //MainForm.Instance.DebugDrawLine(new Vector2(rectx + 1f, recty), new Vector2(rectx, recty), Color.Red);

                                //拿到正方形的四条边，判断是否与线段相交
                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx, recty),
                                    new Vector2(rectx, recty + 1f))) return true;

                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx, recty + 1f),
                                    new Vector2(rectx + 1f, recty + 1f))) return true;

                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx + 1f, recty + 1f),
                                    new Vector2(rectx + 1f, recty))) return true;

                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx + 1f, recty),
                                    new Vector2(rectx, recty))) return true;
                            }
                        }
                    }
                }
            }
            return false;
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
                if ((c.x - a.x) + (c.y - a.y) == 0)
                {
                    //ABCD是同一个点
                }
                else
                {
                    //AB是一个点，CD是一个点，且AC不同
                }
                return false;
            }

            if (Math.Abs(b.y - a.y) + Math.Abs(b.x - a.x) == 0)
            {
                if ((a.x - d.x) * (c.y - d.y) - (a.y - d.y) * (c.x - d.x) == 0)
                {
                    //A、B是一个点，且在CD线段上
                }
                else
                {
                    //A、B是一个点，且不在CD线段上
                }
                return false;
            }
            if (Math.Abs(d.y - c.y) + Math.Abs(d.x - c.x) == 0)
            {
                if ((d.x - b.x) * (a.y - b.y) - (d.y - b.y) * (a.x - b.x) == 0)
                {
                    //C、D是一个点，且在AB线段上
                }
                else
                {
                    //C、D是一个点，且不在AB线段上
                }
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

            if ((contractPoint.x - a.x) * (contractPoint.x - b.x) <= 0
                    && (contractPoint.x - c.x) * (contractPoint.x - d.x) <= 0
                    && (contractPoint.y - a.y) * (contractPoint.y - b.y) <= 0
                    && (contractPoint.y - c.y) * (contractPoint.y - d.y) <= 0)
            {
                //线段相交于点contractPoint
                return true; // '相交  
            }
            else
            {
                //线段相交于虚交点contractPoint
                return false; // '相交但不在线段上  
            }
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

            //Process.Start("explorer.exe", $"/select,\"{filePath}\""); //容易被360误报，很坑

            //打开资源管理器并选中日志文件
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
