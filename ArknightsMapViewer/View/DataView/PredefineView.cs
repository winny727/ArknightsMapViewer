using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class PredefineView : IMapData, IDrawerView<PredefineDrawer>, IMapDataView<Predefine.PredefineInst>
    {
        public Predefine.PredefineInst Predefine { get; set; }
        public string PredefineKey { get; set; }
        public float ActivateTime { get; set; } = -1;
        public string HiddenGroup { get; set; }
        public string RandomSpawnGroupKey { get; set; }
        public string RandomSpawnGroupPackKey { get; set; }
        public int Weight { get; set; }
        public PredefineDrawer PredefineDrawer { get; set; }

        public Predefine.PredefineInst GetData() => Predefine;
        public PredefineDrawer GetDrawer() => PredefineDrawer;

        public string ToSimpleString()
        {
            string text = string.IsNullOrEmpty(PredefineKey) ? PredefineKey : (Predefine.alias ?? Predefine.inst.characterKey);
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
            string text =
                $"PredefineKey: {PredefineKey}\n" +
                $"PredefineData:\n{Predefine}";

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
