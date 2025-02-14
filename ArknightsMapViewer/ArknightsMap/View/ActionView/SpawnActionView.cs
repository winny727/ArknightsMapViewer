using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class SpawnActionView : ActionView, IDrawerView<IRouteDrawer>
    {
        public bool IsExtraRoute { get; set; }
        public IRouteDrawer GetDrawer() => (IRouteDrawer)Drawer;
    }
}
