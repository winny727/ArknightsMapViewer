using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace ArknightsMapViewer
{
    public static class StringHelper
    {
        public static string GetObjFieldValueString(object obj)
        {
            string text = "";
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                object val = field.GetValue(obj);
                if (val != null)
                {
                    text += $"{field.Name}: {val}\n";
                }
            }
            return text;
        }

        public static string GetDbDataValueString(object obj)
        {
            string text = "";
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                DbData.Data value = (DbData.Data)field.GetValue(obj);
                if (value != null && value.m_defined)
                {
                    text += $"{field.Name}: {value}\n";
                }
            }
            return text;
        }

        public static string GetSpawnActionString(string name, ISpawnAction spawnAction)
        {
            string text = name ?? "";
            if (spawnAction.TotalWave > 1)
            {
                text = $"[{spawnAction.WaveIndex}_{spawnAction.SpawnIndexInWave}] " + text;
            }
            else
            {
                text = $"[{spawnAction.SpawnIndexInWave}] " + text;
            }
            if (!string.IsNullOrEmpty(spawnAction.HiddenGroup))
            {
                text += $" {spawnAction.HiddenGroup}";
            }

            string randomSpawnGroupKey = spawnAction.RandomSpawnGroupKey;
            string randomSpawnGroupPackKey = spawnAction.RandomSpawnGroupPackKey;
            bool hasRandomSpawnGroupKey = !string.IsNullOrEmpty(randomSpawnGroupKey);
            bool hasRandomSpawnGroupPackKey = !string.IsNullOrEmpty(randomSpawnGroupPackKey);

            if (hasRandomSpawnGroupKey || hasRandomSpawnGroupPackKey)
            {
                text += " ";
            }
            if (hasRandomSpawnGroupKey)
            {
                int weight = spawnAction.Weight;
                int totalWeight = spawnAction.TotalWeight;
                string weightText = weight < totalWeight ? $"{weight}/{totalWeight}" : weight.ToString();
                text += $"({randomSpawnGroupKey}:{weightText})";
            }
            if (hasRandomSpawnGroupPackKey)
            {
                text += $"[{randomSpawnGroupPackKey}]";
            }
            return text;
        }

        public static void AppendDataString(ref string text, string title, DbData.Data data)
        {
            text ??= "";
            if (data != null && data.m_defined)
            {
                text += $"{title}: {data}\n";
            }
        }

        public static void AppendArrayDataString<T>(ref string text, string title, T[] datas)
        {
            text ??= "";
            if (datas != null && datas.Length > 0)
            {
                string str = "";
                foreach (var item in datas)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        str += ", ";
                    }
                    str += item;
                }
                text += $"{title}: [{str}]\n";
            }
        }
    }
}
