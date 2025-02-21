using System;
using System.Collections.Generic;
using System.Data;

namespace ArknightsMapViewer
{
    public class EnemySpawnView : IMapData, IDrawerView<RouteDrawer>, IMapDataView<Route>, ISpawnAction
    {
        public string EnemyKey { get; set; }
        public DbData EnemyData { get; set; }

        public float SpawnTime { get; set; }
        public int TotalSpawnIndex { get; set; }
        public Route Route { get; set; }
        public int RouteIndex { get; set; }

        public int TotalWave { get; set; }
        public int WaveIndex { get; set; }
        public int SpawnIndexInWave { get; set; }

        public string HiddenGroup { get; set; }
        public string RandomSpawnGroupKey { get; set; }
        public string RandomSpawnGroupPackKey { get; set; }
        public int Weight { get; set; }
        public int TotalWeight { get; set; }

        public bool BlockFragment { get; set; }
        public bool BlockWave { get; set; }
        public bool LastEnemyInWave { get; set; }
        public bool LastEnemyInFragment { get; set; }
        public float MaxTimeWaitingForNextWave { get; set; }

        public RouteDrawer RouteDrawer { get; set; }

        public Route GetData() => Route;
        public RouteDrawer GetDrawer() => RouteDrawer;
        public string ActionKey => EnemyKey;
        public float ActionTime => SpawnTime;

        public int CompareTo(ISpawnAction other)
        {
            (IComparable, IComparable)[] comparer = new (IComparable, IComparable)[]
            {
                (WaveIndex, other.WaveIndex),
                (ActionTime, other.ActionTime),
                (SpawnIndexInWave, other.SpawnIndexInWave),
                (ActionKey, other.ActionKey),
            };

            foreach (var item in comparer)
            {
                if (item.Item1 == null)
                {
                    if (item.Item2 == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else if (item.Item2 == null)
                {
                    return 1;
                }
                else if (item.Item1.Equals(item.Item2))
                {
                    continue;
                }
                return item.Item1.CompareTo(item.Item2);
            }

            return 0;
        }

        public string ToSimpleString()
        {
            string text = $"{((EnemyData != null && !string.IsNullOrEmpty(EnemyData.name)) ? EnemyData.name : EnemyKey)} {SpawnTime}s";
            text = StringHelper.GetSpawnActionString(text, this);
            return text;
        }

        public string ToString(bool showEnemyData)
        {
            string text =
                $"EnemyKey: {EnemyKey}\n";


            if (showEnemyData && EnemyData != null)
            {
                text += $"EnemyData:\n{EnemyData}\n\n";
            }

            text +=
                $"SpawnTime: {SpawnTime}\n" +
                //$"SpawnIndex: {TotalSpawnIndex}\n" +
                $"RouteIndex: {RouteIndex}\n" +
                $"HiddenGroup: {HiddenGroup}\n" +
                $"RandomSpawnGroupKey: {RandomSpawnGroupKey}\n" +
                $"RandomSpawnGroupPackKey: {RandomSpawnGroupPackKey}\n";

            text += TotalWeight > 0 ? $"Weight: {Weight}/{TotalWeight}\n" : $"Weight: {Weight}\n";

            text +=
                $"BlockFragment: {BlockFragment}\n" +
                $"BlockWave: {BlockWave}\n" +
                $"MaxTimeWaitingForNextWave: {MaxTimeWaitingForNextWave}\n";

            return text;
        }

        public override string ToString()
        {
            return ToString(false);
        }
    }
}
