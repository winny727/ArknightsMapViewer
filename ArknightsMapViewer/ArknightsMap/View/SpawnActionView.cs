using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class SpawnActionView
    {
        public Action SpawnAction { get; set; }
        public bool IsExtraRoute { get; set; }
        public IRouteDrawer RouteDrawer { get; set; }
    }
}
