
using System.Collections.Generic;
using System;
using System.Reflection;
using System.IO;

public static class ArrayExt
{
	public static void ForEach<T>(this T[] array, System.Action<T, int> action)
	{
		for (int i = 0; i < array.Length; i++)
		{
			var item = array[i];
			action((T)item, i);
		}
	}
	public static void ForEach<T>(this T[] array, System.Action<T> action)
	{
		for (int i = 0; i < array.Length; i++)
		{
			var item = array[i];
			action((T)item);
		}
	}

	public static T[] Add<T>(this T[] array, T item)
	{
		if(array == null)
			array = new T[]{};
        List<T> tmpList = new List<T>(array);
        tmpList.Add(item);
		array = tmpList.ToArray();
		return array;
	}

	public static void Clear<T>(this T[] array)
	{
		System.Array.Clear(array, 0, array.Length);
	}
}

public static class ListExt
{
	public static List<T> Clone<T>(this List<T> list)
	{
		var clone = new List<T>(list);
		return clone;
	}
}

public static class DictionaryExt
{
	public static Dictionary<TKey, TValue> Add<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
	{
		if(dic.ContainsKey(key))
			dic[key] = value;
		else
			dic.Add(key, value);
		return dic;
	}

	/// <summary>
	/// 序列化方式
	/// </summary>
	public static T DeepCopy<T>(this T obj, int serializeMode)
	{
		//1. 采用XML序列化和反序列化
		if (serializeMode == 1)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				var xmlSerializer = new System.Xml.Serialization.XmlSerializer(obj.GetType());  //无法序列化 System.Collections.Generic.Dictionary`
				xmlSerializer.Serialize(ms, obj);
				ms.Seek(0, SeekOrigin.Begin);
				return (T)xmlSerializer.Deserialize(ms);
			}
		}
		//2. 采用数据契约序列化和反序列化
		else if (serializeMode == 2)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				var serializer = new System.Runtime.Serialization.DataContractSerializer(obj.GetType());
				serializer.WriteObject(ms, obj);
				ms.Seek(0, SeekOrigin.Begin);
				return (T)serializer.ReadObject(ms);
			}
		}
		//3. 采用二进制格式序列化和反序列化
		else if (serializeMode == 3)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				binaryFormatter.Serialize(ms, obj);
				ms.Seek(0, SeekOrigin.Begin);
				T asdsa = (T)binaryFormatter.Deserialize(ms);
				return (T)asdsa;
			}
		}
		else { throw new ArgumentException("serializeMode 参数不在支持的范围内"); }
	}

	/* 利用反射实现深拷贝*/
	public static T DeepCopy<T>(this T _object)
	{
		Type type = _object.GetType();

		// 如果是字符串或值类型则直接返回
		if (_object is string || type.IsValueType) return _object;
		// 如果是数组
		if (type.IsArray)
		{
			Type elementType = Type.GetType(type.FullName.Replace("[]", string.Empty));
			var array = _object as Array;
			Array copied = Array.CreateInstance(elementType, array.Length);
			for (int i = 0; i < array.Length; i++)
			{
				copied.SetValue(DeepCopy(array.GetValue(i)), i);
			}
			return (T)Convert.ChangeType(copied, _object.GetType());
		}

		T retval = Activator.CreateInstance<T>();

		PropertyInfo[] properties = _object.GetType().GetProperties(
			BindingFlags.Public | BindingFlags.NonPublic
			| BindingFlags.Instance | BindingFlags.Static);
		foreach (var property in properties)
		{
			var propertyValue = property.GetValue(_object);
			if (propertyValue == null)
				continue;
			property.SetValue(retval, DeepCopy(propertyValue), null);
		}

		return retval;
	}
}