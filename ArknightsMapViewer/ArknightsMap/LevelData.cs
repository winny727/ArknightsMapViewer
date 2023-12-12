using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ArknightsMap
{
    [Serializable]
    public class LevelData
    {
        public LevelData(RawLevelData rawLevelData)
        {
            options = rawLevelData.options;

            var rawMap = rawLevelData.mapData.map;
            int mapHeight = rawMap.Length;
            int mapWidth = rawMap.Length > 0 ? rawMap[0].Length : 0;

            //TODO row顺序
            map = new Tile[mapWidth, mapHeight];
            for (int row = 0; row < mapHeight; row++)
            {
                for (int col = 0; col < mapWidth; col++)
                {
                    map[col, row] = rawLevelData.mapData.tiles[rawMap[mapHeight - row - 1][col]];
                }
            }


            routes = new List<Route>();
            for (int i = 0; i < rawLevelData.routes.Count; i++)
            {
                if (rawLevelData.routes[i].motionMode != MotionMode.E_NUM)
                {
                    routes.Add(rawLevelData.routes[i]);
                }
            }

            extraRoutes = new List<Route>();
            for (int i = 0; i < rawLevelData.extraRoutes.Count; i++)
            {
                if (rawLevelData.extraRoutes[i].motionMode != MotionMode.E_NUM)
                {
                    extraRoutes.Add(rawLevelData.extraRoutes[i]);
                }
            }

            waves = new List<Wave>(rawLevelData.waves);
            if (rawLevelData.branches != null)
            {
                branches = new Dictionary<string, Branch>(rawLevelData.branches);
            }
        }

        public Options options;
        public Tile[,] map;
        public List<Route> routes;
        public List<Route> extraRoutes;
        public List<Wave> waves;
        public Dictionary<string, Branch> branches;
    }

    [Serializable]
    public class RawLevelData
    {
        public Options options;
        public MapData mapData;
        public List<Route> routes;
        public List<Route> extraRoutes;
        public List<Wave> waves;
        public Dictionary<string, Branch> branches;
    }

    [Serializable]
    public struct Options
    {
        public int characterLimit;
        public int maxLifePoint;
        public int initialCost;
        public int maxCost;
        public float costIncreaseTime;
        public float maxPlayTime;

        public override string ToString()
        {
            return
                $"characterLimit: {characterLimit}\n" + 
                $"maxLifePoint: {maxLifePoint}\n" + 
                $"initialCost: {initialCost}\n" + 
                $"maxCost: {maxCost}\n" + 
                $"costIncreaseTime: {costIncreaseTime}\n" + 
                $"maxPlayTime: {maxPlayTime}";
        }
    }

    [Serializable]
    public class MapData
    {
        public int[][] map;
        public List<Tile> tiles;
        public int width;
        public int height;
    }

    [Serializable]
    public struct Tile
    {
        public string tileKey; //格子类型
        public HeightType heightType; //高台/地面
        public BuildableType buildableType; //可部署类型
        public PassableMask passableMask; //可通过类型

        public override string ToString()
        {
            return
                $"tileKey: {tileKey}\n" +
                $"heightType: {heightType}\n" +
                $"buildableType: {buildableType}\n" +
                $"passableMask: {passableMask}";
        }
    }

    [Serializable]
    public class Route
    {
        public MotionMode motionMode;
        public Position startPosition;
        public Position endPosition;
        public Offset spawnRandomRange;
        public Offset spawnOffset;
        public List<CheckPoint> checkPoints;

        public override string ToString()
        {
            return
                $"motionMode: {motionMode}\n" +
                $"startPosition: {startPosition}\n" +
                $"endPosition: {endPosition}\n" +
                $"spawnRandomRange: {spawnRandomRange}\n" +
                $"spawnOffset: {spawnOffset}\n" +
                $"checkPoints: {checkPoints.Count}";
        }
    }

    [Serializable]
    public struct CheckPoint
    {
        public enum Type
        {
            NONE,
            MOVE,
            WAIT,
        }

        public CheckPointType type;
        public float time;
        public Position position;
        public Offset reachOffset;
        public bool randomizeReachOffset;
        public float reachDistance;

        public Type SimpleType
        {
            get
            {
                switch (type)
                {
                    case CheckPointType.DISAPPEAR:
                        return Type.NONE;
                    case CheckPointType.MOVE:
                    case CheckPointType.APPEAR_AT_POS:
                    case CheckPointType.PATROL_MOVE:
                        return Type.MOVE;
                    case CheckPointType.WAIT_CURRENT_FRAGMENT_TIME:
                    case CheckPointType.WAIT_FOR_SECONDS:
                    case CheckPointType.WAIT_CURRENT_WAVE_TIME:
                    case CheckPointType.WAIT_BOSSRUSH_WAVE:
                        return Type.WAIT;
                    default:
                        break;
                }
                return Type.NONE;
            }
        }

        public string ToSimpleString()
        {
            string text = type.ToString();
            if (SimpleType == Type.MOVE)
            {
                text += $" {position}";
            }
            else if (SimpleType == Type.WAIT)
            {
                text += $" {time}s";
            }
            return text;
        }

        public override string ToString()
        {
            return
                $"type: {type}\n" +
                $"position: {position}\n" +
                $"reachOffset: {reachOffset}\n" +
                $"randomizeReachOffset: {randomizeReachOffset}\n" +
                $"reachDistance: {reachDistance}";
        }
    }

    [Serializable]
    public class Wave
    {
        public float preDelay;
        public float postDelay;
        public float maxTimeWaitingForNextWave;
        public List<Fragment> fragments;
    }

    [Serializable]
    public class Fragment
    {
        public float preDelay;
        public List<Action> actions;
    }

    [Serializable]
    public class Action
    {
        public ActionType actionType;
        public string key;
        public int count;
        public float preDelay;
        public float interval;
        public int routeIndex;
        public bool blockFragment;
        public bool isUnharmfulAndAlwaysCountAsKilled;
        public string hiddenGroup;
        public string randomSpawnGroupKey;
        public string randomSpawnGroupPackKey;
        public RandomType randomType;
        public int weight; //权重
        public bool dontBlockWave;
        public bool isValid;
    }

    [Serializable]
    public class Branch
    {
        public List<Fragment> phases;
    }

    [Serializable]
    public struct Position
    {
        public int col;
        public int row;

        public override string ToString()
        {
            return $"({col},{row})";
        }

        public static bool operator ==(Position pos1, Position pos2)
        {
            return pos1.col == pos2.col && pos1.row == pos2.row;
        }

        public static bool operator !=(Position pos1, Position pos2)
        {
            return !(pos1 == pos2);
        }
    }

    [Serializable]
    public struct Offset
    {
        public float x;
        public float y;

        public override string ToString()
        {
            return $"({x},{y})";
        }
    }

    #region Enum

    public enum HeightType
    {
        LOWLAND,
        HIGHLAND,
    }

    public enum BuildableType
    {
        NONE,
        MELEE,
        RANGED,
        ALL,
    }

    [Flags]
    public enum PassableMask
    {
        NONE = 0,
        WALK_ONLY = 1,
        FLY_ONLY = 2,
        ALL = 3,
    }

    public enum MotionMode
    {
        E_NUM = -1, //跳过？
        WALK,
        FLY,
    }

    //NOTE: 来源于所有地图文件全局搜索统计，顺序来源PRTS
    public enum CheckPointType
    {
        MOVE = 0, //移动
        WAIT_FOR_SECONDS = 1, //停驻(到位置后计时)
        WAIT_FOR_PLAY_TIME = 2, //停驻(全局计时)
        WAIT_CURRENT_FRAGMENT_TIME = 3, //停驻(兵团行动开始后计时)
        WAIT_CURRENT_WAVE_TIME = 4, //停驻(波次行动开始后计时)	
        DISAPPEAR = 5, //进入传送
        APPEAR_AT_POS = 6, //离开传送
        ALERT = 7, //警报
        PATROL_MOVE = 8, //巡逻：存在巡逻路径点时，敌方单位将始终留在战场上沿闭合路径移动，除非其因某些特殊能力退场
        WAIT_BOSSRUSH_WAVE = 9, //引航者试炼-休整
    }

    //NOTE: 顺序未知，来源于所有地图文件全局搜索统计 TODO 顺序
    public enum ActionType
    {
        SPAWN,
        STORY,
        DISPLAY_ENEMY_INFO,
        PREVIEW_CURSOR,
        ACTIVATE_PREDEFINED,
        PLAY_OPERA,
        TRIGGER_PREDEFINED,
        PLAY_BGM,
        BATTLE_EVENTS,
    }

    //出现统计：0,1,2
    public enum RandomType
    {
        ALWAYS,
    }

    #endregion
}
