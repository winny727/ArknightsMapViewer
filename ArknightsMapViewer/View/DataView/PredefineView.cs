using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class PredefineView : IMapData, IDrawerView<PredefineDrawer>, IMapDataView<Predefine.PredefineInst>, ISpawnAction
    {
        public Predefine.PredefineInst Predefine { get; set; }
        public string PredefineKey { get; set; }
        public bool IsCard { get; set; }
        public CharacterData PredefineData { get; set; }
        public float ActivateTime { get; set; } = -1;

        public int TotalWave { get; set; }
        public int WaveIndex { get; set; }
        public int SpawnIndexInWave { get; set; }
        public string HiddenGroup { get; set; }
        public string RandomSpawnGroupKey { get; set; }
        public string RandomSpawnGroupPackKey { get; set; }
        public int Weight { get; set; }
        public PredefineDrawer PredefineDrawer { get; set; }

        public Predefine.PredefineInst GetData() => Predefine;
        public PredefineDrawer GetDrawer() => PredefineDrawer;
        public string ActionKey => PredefineKey;
        public float ActionTime => ActivateTime;

        public int CompareTo(ISpawnAction other)
        {
            (IComparable, IComparable)[] comparer = new (IComparable, IComparable)[]
            {
                (WaveIndex, other.WaveIndex),
                (ActionTime, other.ActionTime),
                (SpawnIndexInWave, other.SpawnIndexInWave),
                (ActionKey, other.ActionKey),
                (HiddenGroup, other.HiddenGroup),
                (RandomSpawnGroupKey, other.RandomSpawnGroupKey),
                (RandomSpawnGroupPackKey, other.RandomSpawnGroupPackKey),
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
            return ToSimpleString(true);
        }

        public string ToSimpleString(bool isSpawn)
        {
            string text = (string.IsNullOrEmpty(PredefineKey) || Predefine == null) ? PredefineKey : (Predefine.alias ?? Predefine.inst.characterKey);
            if (isSpawn)
            {
                text = (PredefineData != null && !string.IsNullOrEmpty(PredefineData.name)) ? PredefineData.name : text;
            }

            if (IsCard)
            {
                text += " (Card)";
            }

            if (isSpawn)
            {
                text += $" {ActivateTime}s";
            }

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
                text += $" w:{Weight}";
            }
            return text;
        }

        public override string ToString()
        {
            string text = $"PredefineKey: {PredefineKey}";

            if (IsCard)
            {
                text += " (Card)";
            }

            text += "\n";

            if (Predefine != null)
            {
                text += $"PredefineData:\n{Predefine}";
            }

            if (ActivateTime >= 0)
            {
                text +=
                    $"ActivateTime: {ActivateTime}\n" +
                    $"HiddenGroup: {HiddenGroup}\n" +
                    $"RandomSpawnGroupKey: {RandomSpawnGroupKey}\n" +
                    $"RandomSpawnGroupPackKey: {RandomSpawnGroupPackKey}\n" +
                    $"Weight: {Weight}\n";
            }

            return text;
        }
    }
}
