using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace ArknightsMapViewer
{
    public class RouteDrawer : IDrawer
    {
        public Route Route { get; private set; }
        public bool ShowRouteLength { get; set; }
        public PathFinding PathFinding { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        private RouteDrawer(Route route, PathFinding pathFinding, int mapWidth, int mapHeight)
        {
            Route = route;
            PathFinding = pathFinding;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
        }

        public static RouteDrawer Create(Route route, PathFinding pathFinding, int mapWidth, int mapHeight)
        {
            if (route == null || route.checkPoints == null)
            {
                //MainForm.Instance.Log("Create WinformRouteDrawer Failed, Invalid Route", MainForm.LogType.Warning);
                return null;
            }

            return new RouteDrawer(route, pathFinding, mapWidth, mapHeight);
        }


        public void Draw(Bitmap bitmap)
        {
            for (int i = -1; i <= Route.checkPoints.Count; i++)
            {
                DrawMoveLine(bitmap, i);
            }
            for (int i = -1; i <= Route.checkPoints.Count; i++)
            {
                DrawCheckPoint(bitmap, i);
            }
        }

        public void DrawCheckPoint(Bitmap bitmap, int checkPointIndex)
        {
            if (checkPointIndex < 0)
            {
                //DrawStartPosition
                Point point = GetPoint(Route.startPosition, Route.spawnOffset);
                DrawMovePosition(bitmap, point);
            }
            else if (checkPointIndex < Route.checkPoints.Count)
            {
                //DrawCheckPoints
                CheckPoint checkPoint = Route.checkPoints[checkPointIndex];
                if (checkPoint.SimpleType == CheckPoint.Type.MOVE)
                {
                    Point point = GetPoint(checkPoint.position, checkPoint.reachOffset);
                    DrawMovePosition(bitmap, point);
                }
                else if (checkPoint.SimpleType == CheckPoint.Type.WAIT)
                {
                    Point prevPoint = GetPrevMovePoint(checkPointIndex);
                    DrawWaitPosition(bitmap, prevPoint, checkPoint.time);
                }
            }
            else
            {
                //DrawEndPosition
                Point point = GetPoint(Route.endPosition, default);
                DrawMovePosition(bitmap, point);
            }
        }

        public void DrawMoveLine(Bitmap bitmap, int checkPointIndex)
        {
            if (checkPointIndex < 0)
            {
                return;
            }

            int prevIndex = GetPrevMoveIndex(checkPointIndex);
            bool needPathFinding = Route.motionMode == MotionType.WALK;
            Color color = GlobalDefine.LINE_COLOR;

            Position prevPosition;
            Offset prevOffset;

            if (prevIndex < 0)
            {
                prevPosition = Route.startPosition;
                prevOffset = Route.spawnOffset;
            }
            else
            {
                CheckPoint prevCheckPoint = Route.checkPoints[prevIndex];
                prevPosition = prevCheckPoint.position;
                prevOffset = prevCheckPoint.reachOffset;
            }

            Position curPosition;
            Offset curOffset;

            if (checkPointIndex < Route.checkPoints.Count)
            {
                CheckPoint curCheckPoint = Route.checkPoints[checkPointIndex];
                curPosition = curCheckPoint.position;
                curOffset = curCheckPoint.reachOffset;
                if (curCheckPoint.type == CheckPointType.APPEAR_AT_POS)
                {
                    color = Color.FromArgb(color.A / 4, color.R, color.G, color.B);
                    needPathFinding = false;
                }
                else if (curCheckPoint.type != CheckPointType.MOVE && curCheckPoint.type != CheckPointType.PATROL_MOVE)
                {
                    return;
                }
            }
            else
            {
                curPosition = Route.endPosition;
                curOffset = default;
            }

            if (prevPosition == curPosition)
            {
                return;
            }

            if (needPathFinding)
            {
                //PathFinding
                List<Vector2Int> path = PathFinding.GetPath(prevPosition, curPosition);

                //移除路径中的非拐点
                for (int i = 1; i < path.Count - 1; i++)
                {
                    Vector2Int prevDir = path[i] - path[i - 1];
                    Vector2Int nextDir = path[i + 1] - path[i];
                    if (Vector2.Dot(prevDir, nextDir) != 0)
                    {
                        path.RemoveAt(i);
                        i--;
                    }
                }

                //若两个点之间无障碍，则移除两个点之间的所有路径点
                for (int i = 0; i < path.Count - 2; i++)
                {
                    int removeCount = 0;
                    for (int j = i + 2; j < path.Count; j++)
                    {
                        Vector2 startPos = path[i];
                        Vector2 endPos = path[j];

                        if (i == 0)
                        {
                            startPos += prevOffset;
                        }
                        if (j == path.Count - 1)
                        {
                            endPos += curOffset;
                        }

                        if (Helper.HasCollider(startPos, endPos, PathFinding.IsBarrier))
                        {
                            break;
                        }
                        removeCount++;
                    }
                    path.RemoveRange(i + 1, removeCount);
                }

                if (path.Count > 2)
                {
                    //color = Color.FromArgb(color.A / 2, color.R, color.G, color.B);
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Position position = path[i];
                        Offset offset = default;
                        Position nextPosition = path[i + 1];
                        Offset nextOffset = default;
                        if (i == 0)
                        {
                            offset = prevOffset;
                        }
                        if (i == path.Count - 2)
                        {
                            nextOffset = curOffset;
                        }
                        Point point = GetPoint(position, offset);
                        Point nextPoint = GetPoint(nextPosition, nextOffset);
                        DrawUtil.DrawLine(bitmap, point, nextPoint, color, GlobalDefine.LINE_WIDTH);
                        if (ShowRouteLength)
                        {
                            float deltaCol = (position.col + offset.x) - (nextPosition.col + nextOffset.x);
                            float deltaRow = (position.row + offset.y) - (nextPosition.row + nextOffset.y);
                            float length = (float)Math.Sqrt(deltaCol * deltaCol + deltaRow * deltaRow);
                            Rectangle rectangle = new Rectangle(
                                Math.Min(point.X, nextPoint.X), 
                                Math.Min(point.Y, nextPoint.Y), 
                                Math.Max(Math.Abs(point.X - nextPoint.X), GlobalDefine.TILE_PIXLE), 
                                Math.Max(Math.Abs(point.Y - nextPoint.Y), GlobalDefine.TILE_PIXLE / 2));
                            DrawUtil.DrawString(bitmap, $"({length:f2})", rectangle, GlobalDefine.LENGTH_FONT, GlobalDefine.LENGTH_COLOR);
                        }
                    }
                    return;
                }
            }

            Point prevPoint = GetPoint(prevPosition, prevOffset);
            Point curPoint = GetPoint(curPosition, curOffset);
            DrawUtil.DrawLine(bitmap, prevPoint, curPoint, color, GlobalDefine.LINE_WIDTH);
            if (ShowRouteLength)
            {
                float deltaCol = (prevPosition.col + prevOffset.x) - (curPosition.col + curOffset.x);
                float deltaRow = (prevPosition.row + prevOffset.y) - (curPosition.row + curOffset.y);
                float length = (float)Math.Sqrt(deltaCol * deltaCol + deltaRow * deltaRow);
                Rectangle rectangle = new Rectangle(
                    Math.Min(prevPoint.X, curPoint.X), 
                    Math.Min(prevPoint.Y, curPoint.Y), 
                    Math.Max(Math.Abs(prevPoint.X - curPoint.X), GlobalDefine.TILE_PIXLE), 
                    Math.Max(Math.Abs(prevPoint.Y - curPoint.Y), GlobalDefine.TILE_PIXLE / 2));
                DrawUtil.DrawString(bitmap, $"{length:f2}", rectangle, GlobalDefine.LENGTH_FONT, GlobalDefine.LENGTH_COLOR);
            }
        }

        private void DrawMovePosition(Bitmap bitmap, Point point)
        {
            DrawUtil.DrawPoint(bitmap, point, GlobalDefine.LINE_COLOR, GlobalDefine.POINT_RADIUS);
        }

        private void DrawWaitPosition(Bitmap bitmap, Point point, float time)
        {
            int length = GlobalDefine.TILE_PIXLE;
            Rectangle rectangle = new Rectangle(point.X - length, point.Y - length, 2 * length, 2 * length);
            DrawUtil.FillCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, GlobalDefine.CIRCLE_COLOR);
            DrawUtil.DrawCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, GlobalDefine.LINE_COLOR, GlobalDefine.CIRCLE_EDGE_WIDTH);
            DrawUtil.DrawString(bitmap, time + "s", rectangle, GlobalDefine.TIME_FONT, GlobalDefine.TEXT_COLOR);
        }

        private Point GetPrevMovePoint(int checkPointIndex)
        {
            int prevIndex = GetPrevMoveIndex(checkPointIndex);
            Position position = Route.startPosition;
            Offset offset = Route.spawnOffset;
            if (prevIndex >= 0)
            {
                CheckPoint prevCheckPoint = Route.checkPoints[prevIndex];
                position = prevCheckPoint.position;
                offset = prevCheckPoint.reachOffset;
            }
            return GetPoint(position, offset);
        }

        private int GetPrevMoveIndex(int checkPointIndex)
        {
            int prevIndex = -1;
            while (checkPointIndex > 0)
            {
                checkPointIndex--;
                CheckPoint prevCheckPoint = Route.checkPoints[checkPointIndex];
                if (prevCheckPoint.SimpleType == CheckPoint.Type.MOVE)
                {
                    prevIndex = checkPointIndex;
                    break;
                }
            }
            return prevIndex;
        }

        private Point GetPoint(Position position, Offset offset)
        {
            return Helper.PositionToPoint(position, offset, MapHeight);
        }
    }
}
