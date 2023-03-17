namespace OpenMedStack.SparkEngine.Disk;

using Interfaces;
using SparkEngine.Extensions;

internal static class Extensions
{
    public static string ToFileName(this IKey key)
    {
        return key.ToStorageKey().Replace('/', '_');
    }
}