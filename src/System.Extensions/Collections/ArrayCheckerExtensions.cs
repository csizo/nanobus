using System.Diagnostics.Contracts;

namespace System.Collections
{
    public static class ArrayCheckerExtensions
    {
        public static bool EqualsTo<T>(this T[] array, T[] array2) where T : IEquatable<T>
        {
            if (ReferenceEquals(array, array2))
            {
                return true;
            }

            if (array == null || array2 == null)
            {
                return false;
            }

            if (array.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (!array[i].Equals(array2[i]))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
