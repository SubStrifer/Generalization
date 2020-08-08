using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CollectionsExtension
{
    public static T RandomElement<T>(this HashSet<T> hashSet)
    {
        List<T> elements = Enumerable.ToList(hashSet);
        return elements[Random.Range(0, elements.Count)];
    }

    public static T RandomElement<T>(this List<T> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    public static TKey RandomKey<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
    {
        List<TKey> elements = Enumerable.ToList(dictionary.Keys);
        return elements[Random.Range(0, elements.Count)];
    }
}