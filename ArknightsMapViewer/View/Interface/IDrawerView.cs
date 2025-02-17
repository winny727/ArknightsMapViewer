using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public interface IDrawerView<T> where T : IDrawer
    {
        public T GetDrawer();
    }
}