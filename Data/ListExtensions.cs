using System.Reflection;

namespace Core;

public static class ListExtensions
{
    /// <summary>
    /// Access the raw backing array of a List<T> using reflection.
    /// This allows for inplace modification of the list's internal storage.
    /// Incredibly useful for structs to avoid boxing/unboxing nuances.
    /// </summary>
    /// <returns>A refrence to the lists backing field</returns>
    /// <exception cref="InvalidOperationException"> An exception thrown if the raw backing field isn't found.</exception>
    public static T[] BackingField<T>(this List<T> list)
    {
        FieldInfo? fieldInfo = typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);

        //In the rare case microsoft renames the backing field, we should throw an exception rather than returning null.
        if (fieldInfo == null)
        {
            throw new InvalidOperationException("Could not find the '_items' field in List<T>.");
        }

        var listRaw = fieldInfo.GetValue(list) as T[];

        if(listRaw == null)
        {
            throw new InvalidOperationException("Could not retrieve the raw array from the list.");
        }

        return listRaw;
    }
}