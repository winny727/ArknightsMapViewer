using System;
using System.Collections.Generic;

namespace ArknightsMapViewer
{
    [Serializable]
    public class StageInfo
    {
        public string stageId;
        public string levelId; //文件路径
        public string zoneId;
        public string code;
        public string name;
        public string description;

        public override string ToString()
        {
            return $"[{code}] {name} ({stageId})";
        }
    }
}
