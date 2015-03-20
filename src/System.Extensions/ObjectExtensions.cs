using System.Collections.Generic;
using System.Threading.Tasks;

namespace System
{
    public static class ObjectExtensions
    {
        public static T As<T>(this object item)
                  where T : class
        {
            return item as T;
        }

        public static Task<T> AsAsync<T>(this object item)
             where T : class
        {
            var result = item as T;
            return Task.FromResult(result);
        }

       
    }
}
