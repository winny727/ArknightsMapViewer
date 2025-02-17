using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class SpawnActionView : ActionView, IDrawerView<RouteDrawer>
    {
        public bool IsExtraRoute { get; set; }
        public RouteDrawer GetDrawer() => (RouteDrawer)Drawer;
    }
}
