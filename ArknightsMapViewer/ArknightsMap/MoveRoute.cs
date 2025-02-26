using System;
using System.Collections.Generic;
using System.Drawing;

namespace ArknightsMapViewer
{
    public class MoveRoute
    {
        private class RouteData
        {
            public Position Position;
            public Offset Offset;

            public Vector2 Location;
            public float WaitTime;
            public bool IsTeleport;

            public RouteData(Position position, Offset offset, bool isTeleport = false)
            {
                Position = position;
                Offset = offset;

                Location = Helper.PositionToVector2(position, offset);
                WaitTime = 0f;
                IsTeleport = isTeleport;
            }
        }

        private Route route;
        private PathFinding pathFinding;
        private List<RouteData> routeDatas = new List<RouteData>();

        public MoveRoute(Route route, PathFinding pathFinding)
        {
            this.route = route;
            this.pathFinding = pathFinding;
            InitMoveRoute();

            //PrintMoveRoute();
        }

        private void InitMoveRoute()
        {
            if (route == null)
            {
                return;
            }

            //startPosition
            routeDatas.Add(new RouteData(route.startPosition, route.spawnOffset));

            //checkPoints
            if (route.checkPoints != null && route.checkPoints.Count > 0)
            {
                for (int index = 0; index < route.checkPoints.Count; index++)
                {
                    CheckPoint checkPoint = route.checkPoints[index];
                    RouteData prevRoutePoint = routeDatas[routeDatas.Count - 1];
                    Position curPosition = checkPoint.position;
                    Offset curOffset = checkPoint.reachOffset;

                    if (checkPoint.SimpleType == CheckPoint.Type.MOVE)
                    {
                        if (checkPoint.type == CheckPointType.APPEAR_AT_POS)
                        {
                            routeDatas.Add(new RouteData(curPosition, curOffset, true));
                        }
                        else if (checkPoint.type == CheckPointType.MOVE || checkPoint.type == CheckPointType.PATROL_MOVE)
                        {
                            AddRouteData(prevRoutePoint.Position, curPosition, curOffset);
                        }
                    }
                    else if (checkPoint.SimpleType == CheckPoint.Type.WAIT)
                    {
                        prevRoutePoint.WaitTime = checkPoint.time;
                    }
                }
            }

            //endPosition
            AddRouteData(routeDatas[routeDatas.Count - 1].Position, route.endPosition, default);
        }

        private void AddRouteData(Position prevPosition, Position curPosition, Offset curOffset)
        {
            //PathFinding
            List<Vector2Int> path = pathFinding.GetPath(prevPosition, curPosition);
            List<Vector2Int> optimizedPath = Helper.OptimizePath(path, pathFinding.IsBarrier);

            if (optimizedPath != null && optimizedPath.Count > 2)
            {
                for (int i = 1; i < optimizedPath.Count; i++)
                {
                    Position position = optimizedPath[i];
                    Offset offset = default;
                    if (i == optimizedPath.Count - 1)
                    {
                        offset = curOffset;
                    }
                    routeDatas.Add(new RouteData(position, offset));
                }
                return;
            }

            routeDatas.Add(new RouteData(curPosition, curOffset, true));
        }

        public void PrintMoveRoute()
        {
            string text = "";
            foreach (RouteData routeData in routeDatas)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    text += "->";
                }
                text += routeData.Location;
                if (routeData.WaitTime > 0)
                {
                    text += $"({routeData.WaitTime}s)";
                }
            }
            Console.WriteLine(text);
            MainForm.Instance.Log(text, MainForm.LogType.Log);
        }
    }
}
