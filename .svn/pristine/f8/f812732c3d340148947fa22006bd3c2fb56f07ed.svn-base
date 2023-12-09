using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    [Serializable]
    public class LevelData
    {
        public LevelData(RawLevelData rawLevelData)
        {
            options = rawLevelData.options;

            var rawMap = rawLevelData.mapData.map;
            map = new Tile[rawMap.Length][];
            for (int i = 0; i < rawMap.Length; i++)
            {
                map[i] = new Tile[rawMap[i].Length];
                for (int j = 0; j < rawMap[i].Length; j++)
                {
                    map[i][j] = rawLevelData.mapData.tiles[rawMap[i][j]];
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
            branches = new Dictionary<string, Branch>(rawLevelData.branches);
        }

        public Options options;
        public Tile[][] map;
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
        public TileKey tileKey; //格子类型
        public HeightType heightType; //高台/地面
        public BuildableType buildableType; //可部署类型
        public PassableMask passableMask; //可通过类型
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

        public override string ToString()
        {
            string text = type.ToString();
            if (SimpleType == Type.MOVE)
            {
                text += $" {position}";
            }
            else if(SimpleType == Type.WAIT)
            {
                text += $" {time}s";
            }
            return text;
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
    }

    [Serializable]
    public struct Offset
    {
        public float x;
        public float y;

        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }

    #region Enum

    //NOTE: 顺序未知，来源于所有地图文件全局搜索统计
    public enum TileKey
    {
        tile_forbidden,
        tile_end,
        tile_road,
        tile_floor,
        tile_start,
        tile_wall,
        tile_hole,
        tile_corrosion,
        tile_grass,
        tile_flystart,
        tile_volcano,
        tile_fence,
        tile_defbreak,
        tile_icetur_lb,
        tile_icestr,
        tile_icetur_rb,
        tile_icetur_lt,
        tile_icetur_rt,
        tile_telin,
        tile_telout,
        tile_bigforce,
        tile_infection,
        tile_yinyang_switch,
        tile_yinyang_road,
        tile_rcm_crate,
        tile_healing,
        tile_deepwater,
        tile_deepsea,
        tile_wooden_wall,
        tile_defup,
        tile_gazebo,
        tile_empty,
        tile_yinyang_wall,
        tile_smog,
        tile_fence_bound,
        tile_rcm_operator,
        tile_poison,
        tile_volcano_emp,
        tile_creepf,
        tile_creep,
        tile_magic_circle_h,
        tile_magic_circle,
        tile_volspread,
        tile_corrosion_2,
        tile_mire,
        tile_reed,
        tile_reedw,
        tile_reedf,
        tile_stairs,
        tile_passable_wall,
        tile_passable_wall_forbidden,
        tile_grvtybtn,
        tile_ristar_road_forbidden,
        tile_ristar_road,
        tile_woodrd,
        tile_act27side,
        tile_green,
        tile_aircraft,
        tile_volcano_strife,
        tile_flowerf,
        tile_flower,
        tile_pollution_road,
        tile_pollution_wall,
    }

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
        NONE,
        WALK_ONLY,
        FLY_ONLY,
        ALL,
    }

    public enum MotionMode
    {
        E_NUM = -1, //跳过？
        WALK,
        FLY,
    }

    //NOTE: 顺序未知，来源于所有地图文件全局搜索统计
    public enum CheckPointType
    {
        MOVE,
        WAIT_CURRENT_FRAGMENT_TIME,
        WAIT_FOR_SECONDS,
        DISAPPEAR,
        APPEAR_AT_POS,
        WAIT_CURRENT_WAVE_TIME,
        PATROL_MOVE,
        WAIT_BOSSRUSH_WAVE,
    }

    //NOTE: 顺序未知，来源于所有地图文件全局搜索统计
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
