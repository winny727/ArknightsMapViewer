using System;
using System.Collections.Generic;
using ArknightsMap;

namespace ArknightsMapViewer
{
    public class LevelViewData
    {
        public LevelData LevelData { get; set; }
        public IMapDrawer MapDrawer { get; set; }
    }
}
