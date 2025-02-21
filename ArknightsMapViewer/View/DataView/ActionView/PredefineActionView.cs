using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public class PredefineActionView : ActionView, IMapData, IDrawerView<PredefineDrawer>
    {
        public bool IsCard { get; set; }
        public PredefineDrawer GetDrawer() => (PredefineDrawer)Drawer;

        public override string ToString()
        {
            string text = $"{nameof(Action.key)}: {Action.key}";

            if (IsCard)
            {
                text += " (Card)";
            }

            text +=
                $"\n{nameof(Action.hiddenGroup)}: {Action.hiddenGroup}\n" +
                $"{nameof(Action.randomSpawnGroupKey)}: {Action.randomSpawnGroupKey}\n" +
                $"{nameof(Action.randomSpawnGroupPackKey)}: {Action.randomSpawnGroupPackKey}\n" +
                $"{nameof(Action.weight)}: {Action.weight}";

            if (((PredefineDrawer)Drawer).Predefine != null)
            {
                text += $"\n{((PredefineDrawer)Drawer).Predefine}";
            }

            return text;
        }
    }
}
