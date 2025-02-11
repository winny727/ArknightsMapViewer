using System.Reflection;

namespace ArknightsMap
{
    public static class StringHelper
    {
        public static string GetObjFieldValueString(object obj)
        {
            string text = "";
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                text += $"{field.Name}: {field.GetValue(obj)}\n";
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
