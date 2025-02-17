using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    public interface IMapDataView<T> where T : IMapData
    {
        public T GetData();
    }
}