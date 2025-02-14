using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public abstract class ActionView : IDataView<Action>
    {
        public Action Action { get; set; }
        public IDrawer Drawer { get; set; }
        public Action GetData() => Action;
    }
}
