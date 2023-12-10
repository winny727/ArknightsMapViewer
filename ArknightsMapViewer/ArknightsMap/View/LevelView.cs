using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class LevelView
    {
        public string Name { get; set; }
        public LevelData LevelData { get; set; }
        public IMapDrawer MapDrawer { get; set; }
    }
}
