using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    public static class EnumerableExtensions
    {
        public static T TakeRandom<T>(this IEnumerable<T> items)
        {
            return items.OrderBy(a => Guid.NewGuid()).First();
        }
    }
    public static class PagedListExtensions
    {
        public static PagedList<T> ToPagedList<T>(this IQueryable<T> source, int page, int pageSize)
        {
            return new PagedList<T>(source, page, pageSize);
        }
        public static Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int page, int pageSize)
        {
            var task = new Task<PagedList<T>>(() => ToPagedList(source, page, pageSize));
            task.Start();
            return task;
        }
    }
}