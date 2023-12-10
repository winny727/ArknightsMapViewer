using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ArknightsMap;
using System.Drawing;

namespace ArknightsMapViewer
{
    public static class Helper
    {
        public static void InitTileColorConfig()
        {
            Color ParseColor(string value)
            {
                Color color = Color.White;
                try
                {
                    color = ColorTranslator.FromHtml(value);
                }
                catch (Exception ex)
                {
                    MainForm.Instance.Log("Invalid Tile Color: " + value, MainForm.LogType.Warning);
                }
                //value = value.Replace(" ", "").Replace("\"", "");
                //string[] colors = value.Split(',');
                //int a, r, g, b;
                //if (colors.Length == 3)
                //{
                //    int.TryParse(colors[0], out r);
                //    int.TryParse(colors[1], out g);
                //    int.TryParse(colors[2], out b);
                //    color = Color.FromArgb(r, g, b);
                //}
                //else if (colors.Length == 4)
                //{
                //    int.TryParse(colors[0], out a);
                //    int.TryParse(colors[1], out r);
                //    int.TryParse(colors[2], out g);
                //    int.TryParse(colors[3], out b);
                //    color = Color.FromArgb(a, r, g, b);
                //}
                return color;
            }

            string path = Environment.CurrentDirectory + "/TileDefine.txt";
            if (File.Exists(path))
            {
                try
                {
                    string text = File.ReadAllText(path);
                    string[] lines = text.Split('\n');
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] values = lines[i].Split('\t');
                        if (values.Length >= 2 && Enum.TryParse(values[0], out TileKey tileKey))
                        {
                            Color tileColor = ParseColor(values[1]);
                            if (!GlobalDefine.TileColor.ContainsKey(tileKey))
                            {
                                GlobalDefine.TileColor.Add(tileKey, tileColor);
                            }
                            else
                            {
                                GlobalDefine.TileColor[tileKey] = tileColor;
                            }

                            string tileText = values[2];
                            if (values.Length >= 4 && !string.IsNullOrEmpty(tileText))
                            {
                                Color textColor = ParseColor(values[3]);
                                if (!GlobalDefine.TileString.ContainsKey(tileKey))
                                {
                                    GlobalDefine.TileString.Add(tileKey, (tileText, textColor));
                                }
                                else
                                {
                                    GlobalDefine.TileString[tileKey] = (tileText, textColor);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = $"TileDefine.txt Open Failed, {ex.Message}";
                    MainForm.Instance.Log(errorMsg, MainForm.LogType.Warning);
                    //MessageBox.Show(errorMsg, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
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
                    isBarrier[col, mapHeight - row - 1] = (tile.passableMask & PassableMask.WALK_ONLY) == 0;
                }
            }

            return isBarrier;
        }

        public static List<Vector2Int> PathFinding(this PathFinding pathFinding, Position origin, Position destination)
        {
            if (pathFinding == null)
            {
                return null;
            }

            Vector2Int originVec = origin.ToVector2Int();
            Vector2Int destinationVec = destination.ToVector2Int();
            List<Vector2Int> result = pathFinding.GetPath(originVec, destinationVec);

            return result;
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
            int y = (int)((mapHeight - (position.row + offset.y) - 1 + 0.5f) * GlobalDefine.TILE_PIXLE);
            return new Point(x, y);
        }

        public static Position PointToPosition(Point point, int mapHeight)
        {
            int x = point.X / GlobalDefine.TILE_PIXLE;
            int y = -(point.Y / GlobalDefine.TILE_PIXLE) + mapHeight - 1;
            return new Position
            {
                col = x,
                row = y,
            };
        }

    #region Extension Method

        public static Position ToPosition(this Vector2Int vector)
        {
            return new Position
            {
                col = vector.x,
                row = vector.y,
            };
        }

        public static Vector2Int ToVector2Int(this Position position)
        {
            return new Vector2Int
            {
                x = position.col,
                y = position.row,
            };
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        #endregion
    }
}
