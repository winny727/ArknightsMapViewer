using System;
using System.Collections.Generic;
using ArknightsMapViewer;

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

            map = new Tile[mapWidth, mapHeight];
            for (int row = 0; row < mapHeight; row++)
            {
                for (int col = 0; col < mapWidth; col++)
                {
                    map[col, row] = rawLevelData.mapData.tiles[rawMap[mapHeight - row - 1][col]];
                }
            }

            routes = rawLevelData.routes;
            extraRoutes = rawLevelData.extraRoutes;

            enemyDbRefs = new Dictionary<string, DbData>();
            if (rawLevelData.enemyDbRefs != null)
            {
                for (int i = 0; i < rawLevelData.enemyDbRefs.Count; i++)
                {
                    DbData data;
                    EnemyDbRef enemyDbRef = rawLevelData.enemyDbRefs[i];
                    if (enemyDbRef.useDb)
                    {
                        data = GlobalDefine.EnemyDBData[enemyDbRef.id][enemyDbRef.level];
                    }
                    else
                    {
                        data = enemyDbRef.overwrittenData;
                    }
                    enemyDbRefs.Add(enemyDbRef.id, data);
                }
            }

            waves = rawLevelData.waves;
            branches = rawLevelData.branches;
        }

        public Options options;
        public Tile[,] map;
        public List<Route> routes;
        public List<Route> extraRoutes;
        public Dictionary<string, DbData> enemyDbRefs;
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
        public List<EnemyDbRef> enemyDbRefs;
        public List<Wave> waves;
        public Dictionary<string, Branch> branches;
    }

    [Serializable]
    public class Options : IData
    {
        public int characterLimit;
        public int maxLifePoint;
        public int initialCost;
        public int maxCost;
        public float costIncreaseTime;
        public float maxPlayTime;

        public override string ToString()
        {
            return StringHelper.GetObjFieldValueString(this);
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
    public class Tile : IData
    {
        public string tileKey; //格子类型
        public HeightType heightType; //高台/地面
        public BuildableType buildableType; //可部署类型
        public PassableMask passableMask; //可通过类型

        public override string ToString()
        {
            string text = $"{nameof(tileKey)}: {tileKey}";

            TileInfo tileInfo = GetTileInfo();
            if (tileInfo != null)
            {
                text +=
                    $"\n{nameof(tileInfo.name)}: {tileInfo.name}\n" +
                    $"{nameof(tileInfo.description)}: {tileInfo.description}\n" +
                    $"{nameof(tileInfo.isFunctional)}: {tileInfo.isFunctional}\n";
            }
            else
            {
                text += " (Undefined Tile)\n";
            }

            text +=
                $"{nameof(heightType)}: {heightType}\n" +
                $"{nameof(buildableType)}: {buildableType}\n" +
                $"{nameof(passableMask)}: {passableMask}\n";

            return text;
        }

        public TileInfo GetTileInfo()
        {
            if (!string.IsNullOrEmpty(tileKey) && GlobalDefine.TileInfo.TryGetValue(tileKey, out TileInfo tileInfo))
            {
                return tileInfo;
            }
            return null;
        }
    }

    [Serializable]
    public class Route : IData
    {
        public MotionType motionMode;
        public Position startPosition;
        public Position endPosition;
        public Offset spawnRandomRange;
        public Offset spawnOffset;
        public List<CheckPoint> checkPoints;
        public bool visitEveryTileCenter;
        public bool visitEveryNodeCenter;
        public bool visitEveryCheckPoint;

        public override string ToString()
        {
            return
                $"{nameof(motionMode)}: {motionMode}\n" +
                $"{nameof(startPosition)}: {startPosition}\n" +
                $"{nameof(endPosition)}: {endPosition}\n" +
                $"{nameof(spawnRandomRange)}: {spawnRandomRange}\n" +
                $"{nameof(spawnOffset)}: {spawnOffset}\n" +
                $"{(checkPoints != null ? $"{nameof(checkPoints)}: {checkPoints.Count}\n)" : "")}" +
                $"{nameof(visitEveryTileCenter)}: {visitEveryTileCenter}\n" +
                $"{nameof(visitEveryNodeCenter)}: {visitEveryNodeCenter}\n" +
                $"{nameof(visitEveryCheckPoint)}: {visitEveryCheckPoint}";
        }
    }

    [Serializable]
    public class CheckPoint : IData
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
            return StringHelper.GetObjFieldValueString(this);
        }
    }

    [Serializable]
    public class Wave : IData
    {
        public float preDelay;
        public float postDelay;
        public float maxTimeWaitingForNextWave;
        public List<Fragment> fragments;

        public override string ToString()
        {
            return
                $"{nameof(preDelay)}: {preDelay}\n" +
                $"{nameof(postDelay)}: {postDelay}\n" +
                $"{nameof(maxTimeWaitingForNextWave)}: {maxTimeWaitingForNextWave}\n";
        }
    }

    [Serializable]
    public class Fragment : IData
    {
        public float preDelay;
        public List<Action> actions;

        public override string ToString()
        {
            return
                $"{nameof(preDelay)}: {preDelay}\n";
        }
    }

    [Serializable]
    public class Action : IData
    {
        public ActionType actionType;
        public bool managedByScheduler;
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
        public RefreshType refreshType;
        public int weight; //权重
        public bool dontBlockWave;
        public bool isValid;
        //extraMeta

        public string ToSimpleString()
        {
            //EnemyData: gamedata/levels/enemydata/enemy_database.json

            string text = actionType.ToString();
            if (actionType == ActionType.SPAWN)
            {
                text += $" {key}";
            }
            return text;
        }

        public override string ToString()
        {
            return StringHelper.GetObjFieldValueString(this);
        }
    }

    [Serializable]
    public class Branch
    {
        public List<Fragment> phases;
    }


    [Serializable]
    public class EnemyDbRef
    {
        public bool useDb;
        public string id;
        public int level;
        public DbData overwrittenData;
    }

    [Serializable]
    public class DbData : IData
    {
        public abstract class Data
        {
            public bool m_defined;
        }

        [Serializable]
        public class Data<T> : Data, IData
        {
            public T m_value;
            public override string ToString()
            {
                return m_value.ToString();
            }
        }

        [Serializable]
        public class KeyValue : IData
        {
            public string key;
            public float value;
            public string valueStr;

            public override string ToString()
            {
                return $"{key}: {valueStr ?? value.ToString()}";
            }
        }

        [Serializable]
        public class Attribute : IData
        {
            public Data<int> maxHp;
            public Data<int> atk;
            public Data<int> def;
            public Data<float> magicResistance;
            public Data<int> cost;
            public Data<int> blockCnt;
            public Data<float> moveSpeed;
            public Data<float> attackSpeed;
            public Data<float> baseAttackTime;
            public Data<int> respawnTime;
            public Data<float> hpRecoveryPerSec;
            public Data<float> spRecoveryPerSec;
            public Data<int> maxDeployCount;
            public Data<int> massLevel;
            public Data<int> baseForceLevel;
            public Data<int> tauntLevel;
            public Data<float> epDamageResistance;
            public Data<float> epResistance;
            public Data<float> damageHitratePhysical;
            public Data<float> damageHitrateMagical;
            public Data<bool> stunImmune;
            public Data<bool> silenceImmune;
            public Data<bool> sleepImmune;
            public Data<bool> frozenImmune;
            public Data<bool> levitateImmune;
            public Data<bool> disarmedCombatImmune;
            public Data<bool> fearedImmune;

            public override string ToString()
            {
                return StringHelper.GetDbDataValueString(this);
            }
        }

        [Serializable]
        public class SkillData : IData
        {
            public string prefabKey;
            public int priority;
            public float cooldown;
            public float initCooldown;
            public int spCost;
            public KeyValue[] blackboard;

            public override string ToString()
            {
                string text =
                    $"{nameof(prefabKey)}: {prefabKey}, " +
                    $"{nameof(priority)}: {priority}, " +
                    $"{nameof(cooldown)}: {cooldown}, " +
                    $"{nameof(initCooldown)}: {initCooldown}, " +
                    $"{nameof(spCost)}: {spCost}\n";

                StringHelper.AppendArrayDataString(ref text, nameof(blackboard), blackboard);

                return text;
            }
        }

        [Serializable]
        public class SpData
        {
            public string spType;
            public int maxSp;
            public int initSp;
            public float increment;

            public override string ToString()
            {
                return StringHelper.GetObjFieldValueString(this);
            }
        }

        public Data<string> name;
        public Data<string> description;
        public Attribute attributes;
        public Data<BuildableType> applyWay;
        public Data<MotionType> motion;
        public Data<string[]> enemyTags;
        public Data<int> lifePointReduce;
        public Data<LevelType> levelType;
        public Data<float> rangeRadius;
        public Data<int> numOfExtraDrops;
        public Data<float> viewRadius;
        public Data<bool> notCountInTotal;
        public KeyValue[] talentBlackboard;
        public SkillData[] skills;
        public SpData spData;

        public override string ToString()
        {
            string text = "";
            StringHelper.AppendDataString(ref text, nameof(name), name);
            StringHelper.AppendDataString(ref text, nameof(description), description);
            text += $"{attributes}";
            StringHelper.AppendDataString(ref text, nameof(applyWay), applyWay);
            StringHelper.AppendDataString(ref text, nameof(motion), motion);

            if (enemyTags != null && enemyTags.m_defined)
            {
                StringHelper.AppendArrayDataString(ref text, nameof(enemyTags), enemyTags.m_value);
            }

            StringHelper.AppendDataString(ref text, nameof(lifePointReduce), lifePointReduce);
            StringHelper.AppendDataString(ref text, nameof(levelType), levelType);
            StringHelper.AppendDataString(ref text, nameof(rangeRadius), rangeRadius);
            StringHelper.AppendDataString(ref text, nameof(numOfExtraDrops), numOfExtraDrops);
            StringHelper.AppendDataString(ref text, nameof(viewRadius), viewRadius);

            StringHelper.AppendArrayDataString(ref text, nameof(talentBlackboard), talentBlackboard);
            StringHelper.AppendArrayDataString(ref text, nameof(skills), skills);

            if (spData != null)
            {
                text += $"{nameof(spData)}:\n{spData}\n";
            }

            return text;
        }

        public void InheritData(DbData data)
        {
            if (!name.m_defined && data.name.m_defined) name = data.name;
            if (!description.m_defined && data.description.m_defined) description = data.description;
            if (attributes == null && data.attributes != null) attributes = data.attributes;
            if (!applyWay.m_defined && data.applyWay.m_defined) applyWay = data.applyWay;
            if (!motion.m_defined && data.motion.m_defined) motion = data.motion;
            if (!enemyTags.m_defined && data.enemyTags.m_defined) enemyTags = data.enemyTags;
            if (!lifePointReduce.m_defined && data.lifePointReduce.m_defined) lifePointReduce = data.lifePointReduce;
            if (!levelType.m_defined && data.levelType.m_defined) levelType = data.levelType;
            if (!rangeRadius.m_defined && data.rangeRadius.m_defined) rangeRadius = data.rangeRadius;
            if (!numOfExtraDrops.m_defined && data.numOfExtraDrops.m_defined) numOfExtraDrops = data.numOfExtraDrops;
            if (!viewRadius.m_defined && data.viewRadius.m_defined) viewRadius = data.viewRadius;
            if (!viewRadius.m_defined && data.viewRadius.m_defined) viewRadius = data.viewRadius;
            if (!notCountInTotal.m_defined && data.notCountInTotal.m_defined) notCountInTotal = data.notCountInTotal;
            if (talentBlackboard == null && data.talentBlackboard != null) talentBlackboard = data.talentBlackboard;
            if (skills == null && data.skills != null) skills = data.skills;
            if (spData == null && data.spData != null) spData = data.spData;
        }
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
        LOWLAND     = 0,
        HIGHLAND    = 1,

        E_NUM,
    }

    public enum BuildableType
    {
        NONE    = 0,
        MELEE   = 1,
        RANGED  = 2,
        ALL     = 3,

        E_NUM,
    }

    [Flags]
    public enum PassableMask
    {
        NONE        = 0,
        WALK_ONLY   = 1,
        FLY_ONLY    = 2,
        ALL         = 3,

        E_NUM,
    }

    public enum MotionType
    {
        WALK    = 0,
        FLY     = 1,

        E_NUM,
    }

    //NOTE: 来源PRTS
    public enum CheckPointType
    {
        MOVE                        = 0, //移动
        WAIT_FOR_SECONDS            = 1, //停驻(到位置后计时)
        WAIT_FOR_PLAY_TIME          = 2, //停驻(全局计时)
        WAIT_CURRENT_FRAGMENT_TIME  = 3, //停驻(兵团行动开始后计时)
        WAIT_CURRENT_WAVE_TIME      = 4, //停驻(波次行动开始后计时)	
        DISAPPEAR                   = 5, //进入传送
        APPEAR_AT_POS               = 6, //离开传送
        ALERT                       = 7, //警报
        PATROL_MOVE                 = 8, //巡逻：存在巡逻路径点时，敌方单位将始终留在战场上沿闭合路径移动，除非其因某些特殊能力退场
        WAIT_BOSSRUSH_WAVE          = 9, //引航者试炼-休整

        E_NUM,
    }

    //NOTE: 来源PRTS
    public enum ActionType
    {
        SPAWN               = 0, //召唤
        PREVIEW_CURSOR      = 1 , //显示路径
        STORY               = 2, //显示剧情
        TUTORIAL            = 3,
        PLAY_BGM            = 4, //播放BGM
        DISPLAY_ENEMY_INFO  = 5, //显示敌人信息
        ACTIVATE_PREDEFINED = 6, //预部署单位生效(干员)
        PLAY_OPERA          = 7, //播放画面特效
        TRIGGER_PREDEFINED  = 8, //预部署单位生效(装置)
        BATTLE_EVENTS       = 9,

        WITHDRAW_PREDEFINED, //预撤回单位生效
        DIALOG, //对话

        E_NUM,
    }

    //出现统计：0,1,2
    public enum RandomType
    {
        NEVER,
        PER_DAY,
        PER_SETTLE_DAY,
        PER_SEASON,
        ALWAYS,
    }

    public enum RefreshType
    {
        NEVER,
        PER_DAY,
        PER_SETTLE_DAY,
        PER_SEASON,
        ALWAYS,
    }

    public enum LevelType
    {
        NORMAL,
        ELITE,
        BOSS,
    }

    #endregion
}
