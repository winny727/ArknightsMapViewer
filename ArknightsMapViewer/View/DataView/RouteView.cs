using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class RouteView : IDrawerView<RouteDrawer>, IMapDataView<Route>
    {
        public int RouteIndex { get; set; }
        public Route Route { get; set; }
        public RouteDrawer RouteDrawer { get; set; }

        public Route GetData() => Route;
        public RouteDrawer GetDrawer() => RouteDrawer;
    }
}
