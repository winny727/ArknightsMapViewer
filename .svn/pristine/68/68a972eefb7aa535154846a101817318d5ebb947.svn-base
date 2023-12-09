using System;
using System.Collections.Generic;
using ArknightsMap;
using System.Windows.Forms;
using System.Drawing;

namespace ArknightsMapViewer
{
    public class WinformRouteDrawer : IRouteDrawer
    {
        public PictureBox PictureBox { get; private set; }
        public Route Route { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        public WinformRouteDrawer(PictureBox pictureBox, Route route, int mapWidth, int mapHeight)
        {
            PictureBox = pictureBox;
            Route = route;
            MapWidth = mapWidth;
            MapHeight = mapHeight;
        }

        public void InitCanvas()
        {
            Bitmap bitmap = new Bitmap(PictureBox.BackgroundImage.Width, PictureBox.BackgroundImage.Height);
            PictureBox.Image?.Dispose();
            PictureBox.Image = bitmap;
        }

        public void RefreshCanvas()
        {
            PictureBox.Refresh();
        }

        public void DrawRoute()
        {
            InitCanvas();
            for (int i = -1; i <= Route.checkPoints.Count; i++)
            {
                DrawCheckPoint(i);
            }
            RefreshCanvas();
        }

        public void DrawCheckPoint(int checkPointIndex)
        {
            if (checkPointIndex < 0)
            {
                //DrawStartPosition
                Point point = GetPoint(Route.startPosition, Route.spawnOffset);
                DrawMovePosition(point);
            }
            else if(checkPointIndex< Route.checkPoints.Count)
            {
                CheckPoint checkPoint = Route.checkPoints[checkPointIndex];
                if (checkPoint.SimpleType == CheckPoint.Type.MOVE)
                {
                    Point point = GetPoint(checkPoint.position, checkPoint.reachOffset);
                    DrawMoveLine(checkPointIndex);
                    DrawMovePosition(point);
                }
                else if (checkPoint.SimpleType == CheckPoint.Type.WAIT)
                {
                    Point prevPoint = GetPrevMovePoint(checkPointIndex);
                    DrawWaitPosition(prevPoint, checkPoint.time);
                }
            }
            else
            {
                //DrawEndPosition
                Point point = GetPoint(Route.endPosition, default);
                DrawMoveLine(Route.checkPoints.Count);
                DrawMovePosition(point);
            }
        }

        private void DrawMovePosition(Point point)
        {
            Bitmap bitmap = (Bitmap)PictureBox.Image;
            DrawUtil.DrawPoint(bitmap, point, GlobalDefine.LINE_COLOR, GlobalDefine.LINE_WIDTH * 2);
        }

        private void DrawMoveLine(int checkPointIndex)
        {
            int prevIndex = GetPrevMoveIndex(checkPointIndex);
            bool needPathFinding = Route.motionMode == MotionMode.WALK;
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
            }
            else
            {
                curPosition = Route.endPosition;
                curOffset = default;
            }

            if (needPathFinding)
            {
                //TODO
            }

            Point prevPoint = GetPoint(prevPosition, prevOffset);
            Point curPoint = GetPoint(curPosition, curOffset);

            Bitmap bitmap = (Bitmap)PictureBox.Image;
            DrawUtil.DrawLine(bitmap, prevPoint, curPoint, color, GlobalDefine.LINE_WIDTH);
        }

        private void DrawWaitPosition(Point point, float time)
        {
            Bitmap bitmap = (Bitmap)PictureBox.Image;
            int length = GlobalDefine.TILE_PIXLE;
            Rectangle rectangle = new Rectangle(point.X - length, point.Y - length, 2 * length, 2 * length);
            DrawUtil.FillCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, GlobalDefine.CIRCLE_COLOR);
            DrawUtil.DrawCircle(bitmap, point, GlobalDefine.CIRCLE_RADIUS, GlobalDefine.LINE_COLOR, GlobalDefine.LINE_WIDTH);
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
            int x = (int)((position.col + offset.x + 0.5f) * GlobalDefine.TILE_PIXLE);
            int y = (int)((MapHeight - position.row - 1 - offset.y + 0.5f) * GlobalDefine.TILE_PIXLE);
            return new Point(x, y);
        }
    }
}
