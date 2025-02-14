using System;
using System.Collections.Generic;
using System.Data;

namespace ArknightsMap
{
    public class EnemySpawnView : IData, IDrawerView<IRouteDrawer>, IDataView<Route>
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

        public string HiddenGroup { get; set; } //Filter
        public string RandomSpawnGroupKey { get; set; } //Filter
        public string RandomSpawnGroupPackKey { get; set; } //Filter
        public int Weight { get; set; } //Hint

        public bool BlockFragment { get; set; } //Hint
        public bool BlockWave { get; set; } //Hint
        public bool LastEnemyInWave { get; set; }
        public bool LastEnemyInFragment { get; set; }
        public float MaxTimeWaitingForNextWave { get; set; }

        public IRouteDrawer RouteDrawer { get; set; }

        public Route GetData() => Route;
        public IRouteDrawer GetDrawer() => RouteDrawer;

        public int CompareTo(EnemySpawnView other)
        {
            (IComparable, IComparable)[] comparer = new (IComparable, IComparable)[]
            {
                (SpawnTime, other.SpawnTime),
                (EnemyKey, other.EnemyKey),
                (HiddenGroup, other.HiddenGroup),
                (RandomSpawnGroupKey, other.RandomSpawnGroupKey),
                (RandomSpawnGroupPackKey, other.RandomSpawnGroupPackKey),
                (TotalSpawnIndex, other.TotalSpawnIndex),
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
            string text = $"{(EnemyData != null ? EnemyData.name.m_value : EnemyKey)} {SpawnTime}s";
            if (TotalWave > 1)
            {
                text = $"[{WaveIndex}_{SpawnIndexInWave}] " + text;
            }
            if (!string.IsNullOrEmpty(HiddenGroup))
            {
                text += $" {HiddenGroup}";
            }
            if (!string.IsNullOrEmpty(RandomSpawnGroupKey))
            {
                text += $" {RandomSpawnGroupKey}";
            }
            if (!string.IsNullOrEmpty(RandomSpawnGroupPackKey))
            {
                text += $" {RandomSpawnGroupPackKey}";
            }
            if (Weight > 0)
            {
                text += $" {Weight}%";
            }
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
                $"SpawnIndex: {TotalSpawnIndex}\n" +
                $"RouteIndex: {RouteIndex}\n" +
                $"HiddenGroup: {HiddenGroup}\n" +
                $"RandomSpawnGroupKey: {RandomSpawnGroupKey}\n" +
                $"RandomSpawnGroupPackKey: {RandomSpawnGroupPackKey}\n" +
                $"Weight: {Weight}\n" +
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
