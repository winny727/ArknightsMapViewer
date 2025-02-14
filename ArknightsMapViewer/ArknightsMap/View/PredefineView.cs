using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class PredefineView : IDrawerView<IPredefineDrawer>, IDataView<Predefine.PredefineInst>
    {
        public Predefine.PredefineInst Predefine { get; set; }
        public IPredefineDrawer PredefineDrawer { get; set; }

        public Predefine.PredefineInst GetData() => Predefine;
        public IPredefineDrawer GetDrawer() => PredefineDrawer;
    }
}
