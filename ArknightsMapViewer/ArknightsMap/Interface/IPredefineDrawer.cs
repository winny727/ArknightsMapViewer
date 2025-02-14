using System;
using System.Collections.Generic;

namespace ArknightsMap
{

    public interface IPredefineDrawer : IDrawer
    {
        public Predefine.PredefineInst Predefine { get; }
        public void DrawPredefine();
    }
}