using System;
using System.Collections.Generic;
using System.Text;

public static class TypeUtility
{
    public static string GetSimpleName(this Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        StringBuilder builder = new StringBuilder();

        int index = type.Name.IndexOf('`');
        builder.Append(type.Name.Remove(index));
        builder.Append('<');
        Type[] args = type.GetGenericArguments();
        for (int i = 0; i < args.Length; i++)
        {
            if (i != 0)
            {
                builder.Append(',');
            }
            builder.Append(args[i].GetSimpleName());
        }
        builder.Append('>');

        return builder.ToString();
    }

    public static object CreateInstance(Type type)
    {
        return type == typeof(string) ? string.Empty : Activator.CreateInstance(type);
    }

    public static T CreateInstance<T>()
    {
        return (T)CreateInstance(typeof(T));
    }

    public static object GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    public static T GetDefaultValue<T>()
    {
        return (T)GetDefaultValue(typeof(T));
    }

    #region ConvertTo Ext

    public static T ConvertTo<T>(this object from, T defaultValue)
    {
        return (T)ConvertTo(from, typeof(T), false, defaultValue);
    }

    public static object ConvertTo(this object from, Type to, object defaultValue)
    {
        return ConvertTo(from, to, false, defaultValue);
    }

    public static T ConvertAutoTo<T>(this object from)
    {
        return (T)ConvertTo(from, typeof(T), true, GetDefaultValue(typeof(T)));
    }

    public static object ConvertAutoTo(this object from, Type to)
    {
        return ConvertTo(from, to, true, GetDefaultValue(to));
    }

    #endregion ConvertTo Ext

    public static object ConvertTo(this object from, Type to, bool autoDefault, object defaultValue)
    {
        try
        {
            if (!ConvertToChecker(from.GetType(), to))
            {
                return autoDefault ? CreateInstance(to) : defaultValue;
            }
            return ConvertTo(from, to);
        }
        catch
        {
            return autoDefault ? CreateInstance(to) : defaultValue;
        }
    }

    public static object ConvertTo(this object from, Type to)
    {
        if (to.IsEnum)
        {
            if (from is string str)
            {
                return Enum.Parse(to, str, true);
            }
            return Enum.ToObject(to, from);
        }

        return Convert.ChangeType(from, to);
    }

    public static bool ConvertToChecker(this Type from, Type to)
    {
        if (from == null || to == null)
        {
            return false;
        }

        // 总是可以隐式类型转换为 Object。
        if (to == typeof(object))
        {
            return true;
        }

        if (to.IsAssignableFrom(from))
        {
            return true;
        }

        if (typeof(IConvertible).IsAssignableFrom(from) &&
            typeof(IConvertible).IsAssignableFrom(to))
        {
            return true;
        }

        return false;
    }
}
