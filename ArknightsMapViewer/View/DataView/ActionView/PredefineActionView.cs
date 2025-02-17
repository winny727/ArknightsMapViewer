using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class PredefineActionView : ActionView, IMapData, IDrawerView<PredefineDrawer>
    {
        public PredefineDrawer GetDrawer() => (PredefineDrawer)Drawer;

        public override string ToString()
        {
            string text =
                $"{nameof(Action.key)}: {Action.key}\n" +
                $"{nameof(Action.hiddenGroup)}: {Action.hiddenGroup}\n" +
                $"{nameof(Action.randomSpawnGroupKey)}: {Action.randomSpawnGroupKey}\n" +
                $"{nameof(Action.randomSpawnGroupPackKey)}: {Action.randomSpawnGroupPackKey}\n" +
                $"{nameof(Action.weight)}: {Action.weight}\n" +
                $"{((PredefineDrawer)Drawer).Predefine}";

            return text;
        }
    }
}
