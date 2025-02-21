using System;
using System.Collections.Generic;
using System.Drawing;

namespace ArknightsMapViewer
{
    public interface ISpawnAction : IComparable<ISpawnAction>
    {
        public string ActionKey { get; }
        public float ActionTime { get; }
        public int TotalWave { get; }
        public int WaveIndex { get; }
        public int SpawnIndexInWave { get; }
        public string HiddenGroup { get; }
        public string RandomSpawnGroupKey { get; }
        public string RandomSpawnGroupPackKey { get; }
        public int Weight { get; }
        public int TotalWeight { get; set; }
        public string ToSimpleString();
    }
}