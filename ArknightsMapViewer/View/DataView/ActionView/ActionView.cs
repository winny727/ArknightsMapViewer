using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public abstract class ActionView : IMapDataView<Action>
    {
        public Action Action { get; set; }
        public IDrawer Drawer { get; set; }
        public Action GetData() => Action;
    }
}
