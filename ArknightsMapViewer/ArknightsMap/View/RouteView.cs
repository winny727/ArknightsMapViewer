using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class RouteView
    {
        public int RouteIndex { get; set; }
        public Route Route { get; set; }
        public IRouteDrawer RouteDrawer { get; set; }
    }
}
