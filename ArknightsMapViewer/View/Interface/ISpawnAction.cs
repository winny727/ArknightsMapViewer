using System;
using System.Collections.Generic;
using System.Drawing;

namespace ArknightsMapViewer
{
    public interface ISpawnAction : IComparable<ISpawnAction>
    {
        public string ActionKey { get; }
        public float ActionTime { get; }
        public string HiddenGroup { get; }
        public string RandomSpawnGroupKey { get; }
        public string RandomSpawnGroupPackKey { get; }
        public string ToSimpleString();
    }
}