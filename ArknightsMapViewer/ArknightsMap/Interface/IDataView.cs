using System;
using System.Collections.Generic;

namespace ArknightsMap
{

    public interface IDataView<T> where T : IData
    {
        public T GetData();
    }
}