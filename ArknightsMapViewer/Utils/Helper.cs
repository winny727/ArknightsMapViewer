using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMap;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Text;

namespace ArknightsMapViewer
{
    public static class Helper
    {
        public static string[] LoadLatestFilePath()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "latest_files.ini");
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
            string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
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
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "draw_config.json");
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

            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            float dpi = g.DpiX * 0.01f;

            GlobalDefine.TILE_PIXLE = (int)(drawConfig.Size.tilePixle * dpi);
            GlobalDefine.LINE_WIDTH = (int)(drawConfig.Size.lineWidth * dpi);
            GlobalDefine.CIRCLE_EDGE_WIDTH = (int)(drawConfig.Size.circleEdgeWidth * dpi);
            GlobalDefine.CIRCLE_RADIUS = (int)(drawConfig.Size.circleRadius * dpi);
            GlobalDefine.POINT_RADIUS = (int)(drawConfig.Size.pointRadius * dpi);

            Enum.TryParse(drawConfig.Font.textFontStyle, out FontStyle textFontStyle);
            Enum.TryParse(drawConfig.Font.indexFontStyle, out FontStyle indexFontStyle);
            Enum.TryParse(drawConfig.Font.timeFontStyle, out FontStyle timeFontStyle);
            Enum.TryParse(drawConfig.Font.lenghtFontStyle, out FontStyle lenghtFont);

            GlobalDefine.TEXT_FONT = new Font(drawConfig.Font.textFont, drawConfig.Font.textFontSize, textFontStyle);
            GlobalDefine.INDEX_FONT = new Font(drawConfig.Font.indexFont, drawConfig.Font.indexFontSize, indexFontStyle);
            GlobalDefine.TIME_FONT = new Font(drawConfig.Font.timeFont, drawConfig.Font.timeFontSize, timeFontStyle);
            GlobalDefine.LENGTH_FONT = new Font(drawConfig.Font.lenghtFont, drawConfig.Font.lenghtFontSize, lenghtFont);
            GlobalDefine.TEXT_COLOR = ColorTranslator.FromHtml(drawConfig.Color.textColor);
            GlobalDefine.LINE_COLOR = ColorTranslator.FromHtml(drawConfig.Color.lineColor);
            GlobalDefine.CIRCLE_COLOR = ColorTranslator.FromHtml(drawConfig.Color.circleColor);
            GlobalDefine.LENGTH_COLOR = ColorTranslator.FromHtml(drawConfig.Color.lengthColor);
            GlobalDefine.DEFAULT_ROAD_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultRoadColor);
            GlobalDefine.DEFAULT_WALL_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultWallColor);
            GlobalDefine.DEFAULT_FLOOR_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultFloorColor);
            GlobalDefine.DEFAULT_FORBIDDEN_COLOR = ColorTranslator.FromHtml(drawConfig.Color.defaultForbiddenColor);
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
                catch (Exception ex)
                {
                    MainForm.Instance.Log($"Invalid Tile Color: {value}", MainForm.LogType.Warning);
                }

                return null;
            }

            //注意tile_info.csv要保存为UTF-8格式的否则中文无法读取
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "tile_info.csv");

            try
            {
                TableReader tableReader = new TableReader(path, ',');
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
            catch(Exception ex)
            {
                string errorMsg = $"tile_info.csv Read Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                //MessageBox.Show(errorMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void InitEnemyDatabase()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "enemy_database.json");
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var enemyDatabase = JsonConvert.DeserializeObject<JObject>(json);
                var enemies = enemyDatabase["enemies"];
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
                                DbData data = jArray[i]["enemyData"].ToObject<DbData>();
                                if (i > 0)
                                {
                                    data.InheritData(dbDatas[0]);
                                }
                                dbDatas.Add(level, data);
                            }
                        }
                        if (!string.IsNullOrEmpty(key) && !GlobalDefine.EnemyDBData.ContainsKey(key))
                        {
                            GlobalDefine.EnemyDBData.Add(key, dbDatas);
                        }
                        else
                        {
                            string errorMsg = $"enemy_database.json Parse Error, ErrorKey: {key}";
                            MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"enemy_database.json Parse Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
            }
        }

        public static void InitTrapDatabase()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Config", "character_table.json");
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var charcterDatabase = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(json);
                if (charcterDatabase != null)
                {
                    foreach (var item in charcterDatabase)
                    {
                        string key = item.Key;
                        JObject data = item.Value;

                        if (data["profession"]?.ToString() == "TRAP")
                        {
                            TrapData trapData = new TrapData()
                            {
                                name = data["name"]?.ToString(),
                                description = data["description"]?.ToString(),
                                appellation = data["appellation"]?.ToString(),
                            };

                            if (!string.IsNullOrEmpty(key) && !GlobalDefine.TrapDBData.ContainsKey(key))
                            {
                                GlobalDefine.TrapDBData.Add(key, trapData);
                            }
                            else
                            {
                                string errorMsg = $"character_table.json Parse Error, ErrorKey: {key}";
                                MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"character_table.json Parse Error, {ex.Message}";
                MainForm.Instance.Log(errorMsg, MainForm.LogType.Error);
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

        public static bool[,] GetIsBarrierArray(LevelData levelData)
        {
            Tile[,] map = levelData.map;
            int mapWidth = map.GetLength(0);
            int mapHeight = map.GetLength(1);
            bool[,] isBarrier = new bool[mapWidth, mapHeight];
            for (int row = 0; row < mapHeight; row++)
            {
                for (int col = 0; col < mapWidth; col++)
                {
                    Tile tile = map[col, row];
                    isBarrier[col, row] = tile.passableMask == PassableMask.FLY_ONLY || tile.passableMask == PassableMask.NONE;
                }
            }

            return isBarrier;
        }

        /// <summary>
        /// 判断两点之间有无碰撞体，类似射线
        /// </summary>
        /// <param name="startPos">起点</param>
        /// <param name="endPos">终点</param>
        /// <returns></returns>
        public static bool HasCollider(Vector2 startPos, Vector2 endPos, bool[,] isBarrier)
        {
            int mapWidth = isBarrier.GetLength(0);
            int mapHeight = isBarrier.GetLength(1);

            //xy都相等则为同一点
            if (startPos.x == endPos.x && startPos.y == endPos.y)
            {
                if (isBarrier[(int)(startPos.x + 0.5f), (int)(startPos.y + 0.5f)]) return true;
                else return false;
            }
            //若为竖直
            else if (startPos.x == endPos.x)
            {
                for (int i = 1; i < Math.Abs(endPos.y - startPos.y); i++)
                {
                    if (isBarrier[(int)(startPos.x + 0.5f),
                        (int)(startPos.y + i * Math.Sign(endPos.y - startPos.y) + 0.5f)]) return true;
                }
                return false;
            }
            //若为水平
            else if (startPos.y == endPos.y)
            {
                for (int i = 1; i < Math.Abs(endPos.x - startPos.x); i++)
                {
                    if (isBarrier[(int)(startPos.x + i * Math.Sign(endPos.x - startPos.x) + 0.5f),
                        (int)(startPos.y + 0.5f)]) return true;
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
                float characterRadius = 0.1f;

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
                                //拿到正方形的四条边，判断是否与线段相交
                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx - 0.5f, recty - 0.5f),
                                    new Vector2(rectx - 0.5f, recty + 0.5f))) return true;

                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx - 0.5f, recty + 0.5f),
                                    new Vector2(rectx + 0.5f, recty + 0.5f))) return true;

                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx + 0.5f, recty + 0.5f),
                                    new Vector2(rectx + 0.5f, recty - 0.5f))) return true;

                                if (GetIntersection(startOffset, endOffset,
                                    new Vector2(rectx + 0.5f, recty - 0.5f),
                                    new Vector2(rectx - 0.5f, recty - 0.5f))) return true;
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

        #region Extension Method

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        #endregion
    }
}
