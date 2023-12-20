using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ArknightsMap
{
    public class LevelReader
    {
        public RawLevelData RawLevelData { get; private set; }
        public LevelData LevelData { get; private set; }
        public bool IsValid { get; private set; }
        public string ErrorMsg { get; private set; }

        public LevelReader(string levelJson)
        {
            if (TryParseLevelJson(levelJson, out RawLevelData rawLevelData, out string errorMsg))
            {
                if (IsRawLevelDataValid(rawLevelData))
                {
                    IsValid = true;
                    RawLevelData = rawLevelData;
                    LevelData = new LevelData(rawLevelData);
                }
            }
            else
            {
                IsValid = false;
                ErrorMsg = errorMsg;
            }
        }

        private bool TryParseLevelJson(string jsonStr, out RawLevelData rawLevelData, out string errorMsg)
        {
            rawLevelData = default;
            errorMsg = default;

            bool isSuccess = false;
            if (!string.IsNullOrEmpty(jsonStr))
            {
                try
                {
                    rawLevelData = JsonConvert.DeserializeObject<RawLevelData>(jsonStr);
                    isSuccess = true;
                }
                catch (Exception ex)
                {
                    errorMsg = "Json Deserialize Error: " + ex.Message;
                }
            }
            return isSuccess;
        }

        private bool IsRawLevelDataValid(RawLevelData rawLevelData)
        {
            if (rawLevelData == null)
            {
                return false;
            }

            MapData mapData = rawLevelData.mapData;
            if (mapData == null || mapData.tiles == null || mapData.tiles.Count <= 0)
            {
                return false;
            }

            if (rawLevelData.routes == null || rawLevelData.routes.Count <= 0)
            {
                return false;
            }

            if (rawLevelData.waves == null || rawLevelData.waves.Count <= 0)
            {
                return false;
            }

            return true;
        }
    }
}
