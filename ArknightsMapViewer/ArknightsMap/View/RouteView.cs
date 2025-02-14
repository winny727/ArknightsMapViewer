using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class RouteView : IDrawerView<IRouteDrawer>, IDataView<Route>
    {
        public int RouteIndex { get; set; }
        public Route Route { get; set; }
        public IRouteDrawer RouteDrawer { get; set; }

        public Route GetData() => Route;
        public IRouteDrawer GetDrawer() => RouteDrawer;
    }
}
