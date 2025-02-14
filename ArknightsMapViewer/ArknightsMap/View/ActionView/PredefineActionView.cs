using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class PredefineActionView : ActionView, IDrawerView<IPredefineDrawer>
    {
        public IPredefineDrawer GetDrawer() => (IPredefineDrawer)Drawer;
    }
}
