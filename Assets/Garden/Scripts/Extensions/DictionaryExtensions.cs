using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DictionaryExtensions
{
    public static KeyValuePair<TKey, TValue>? GetPrevious<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
        TKey key)
    {
        if(dictionary == null || dictionary.Count <= 1)
        {
            return null;
        }
        KeyValuePair<TKey, TValue>? previous = null;
        foreach (var kvp in dictionary)
        {
            if (EqualityComparer<TKey>.Default.Equals(kvp.Key, key))
                return previous;
            previous = kvp;
        }
        return null;
    }
}
