using System;
using System.Collections.Generic;

namespace ArknightsMap
{

    public interface IDrawerView<T> where T : IDrawer
    {
        public T GetDrawer();
    }
}