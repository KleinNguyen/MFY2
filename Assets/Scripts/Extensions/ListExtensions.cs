using System.Collections.Generic;
using UnityEngine;

public static class ListExtensions
{
    public static T Draw<T>(this List<T> list)
    {
        if (list.Count == 0) return default;

        int randomIndex = Random.Range(0, list.Count);
        T selectedItem = list[randomIndex];

        // Remove the drawn value from the source list.
        list.Remove(selectedItem);
        return selectedItem;
    }
}
