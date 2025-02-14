using System;
using System.Collections.Generic;

namespace ArknightsMap
{
    public class PredefineActionView : ActionView, IData, IDrawerView<IPredefineDrawer>
    {
        public IPredefineDrawer GetDrawer() => (IPredefineDrawer)Drawer;

        public override string ToString()
        {
            string text =
                $"{nameof(Action.key)}: {Action.key}\n" +
                $"{nameof(Action.hiddenGroup)}: {Action.hiddenGroup}\n" +
                $"{nameof(Action.randomSpawnGroupKey)}: {Action.randomSpawnGroupKey}\n" +
                $"{nameof(Action.randomSpawnGroupPackKey)}: {Action.randomSpawnGroupPackKey}\n" +
                $"{nameof(Action.weight)}: {Action.weight}\n" +
                $"{((IPredefineDrawer)Drawer).Predefine}";

            return text;
        }
    }
}
