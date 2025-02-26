using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class LevelView
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public LevelData LevelData { get; set; }
        public SpawnView SpawnView { get; set; }
        public TimelineSimulator TimelineSimulator { get; set; }
        public MapDrawer MapDrawer { get; set; }
    }
}
