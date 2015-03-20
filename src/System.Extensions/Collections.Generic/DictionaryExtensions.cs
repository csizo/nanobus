using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable CheckNamespace
namespace System.Collections.Generic
// ReSharper restore CheckNamespace
{
    public class RoundRobinList<T> : IEnumerable<T>
    {
        private readonly LinkedList<T> _list;
        private LinkedListNode<T> _current;

        public RoundRobinList(IEnumerable<T> collection)
        {
            _list = new LinkedList<T>(collection);
            _current = _list.First;
        }

        public RoundRobinList()
        {
            _list = new LinkedList<T>();
        }

        public void Add(T item)
        {
            lock (_list)
            {
                _list.AddLast(item);

                if (_current == null)
                    _current = _list.First;
            }
        }
        public void Add(IEnumerable<T> items)
        {
            lock (_list)
            {
                foreach (var item in items)
                {
                    _list.AddLast(item);
                }

                if (_current == null)
                    _current = _list.First;
            }
        }
        public void Remove(T item)
        {
            lock (_list) { _list.Remove(item); }
        }


        public T Next()
        {
            var value = _current.Value;
            lock (_list)
            {
                if (_current.Next != null)
                    _current = _current.Next;
                else
                    _current = _current.List.First;
            }

            return value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_list)
            {
                return new List<T>(_list).GetEnumerator();
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public static class Extensions
    {
        public static IList<ArraySegment<byte>> ToArraySegments(this byte[] bytes, int maxChunkSize = 65536)
        {
            //split the bytes to 65K chunks
            IList<ArraySegment<byte>> segments = new List<ArraySegment<byte>>();

            var chunkSize = Math.Min(maxChunkSize, bytes.Length);

            for (var offset = 0; offset < bytes.Length; offset += chunkSize)
            { 
                chunkSize = Math.Min(bytes.Length - offset, maxChunkSize);

                var segment = new ArraySegment<byte>(bytes, offset, chunkSize);
                segments.Add(segment);
            }

            return segments;
        }


        public static Task<List<T>> ToListAsync<T>(this IEnumerable<T> items)
        {
            var task = new Task<List<T>>(items.ToList);
            task.Start();
            return task;
        }

        public static Task<IList<T>> ToIListAsync<T>(this IEnumerable<T> items)
        {
            var task = new Task<IList<T>>(items.ToList);
            task.Start();
            return task;
        }

        public static IList<T> AsSnapshot<T>(this IList<T> items)
        {
            lock (items)
            {
                return new List<T>(items);
            }
        }
        public static ICollection<T> AsSnapshot<T>(this ICollection<T> items)
        {
            lock (items)
            {
                return new List<T>(items);
            }
        }
    }
    public class PageSetting : IEquatable<PageSetting>
    {
        public bool Equals(PageSetting other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PageSize == other.PageSize && PageIndex == other.PageIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PageSetting)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (PageSize * 397) ^ PageIndex;
            }
        }

        public static bool operator ==(PageSetting left, PageSetting right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PageSetting left, PageSetting right)
        {
            return !Equals(left, right);
        }

        public PageSetting(int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }

        public static readonly PageSetting All = new PageSetting(0, 0);
    }

    public class PagedList<T> : List<T>, IPagedList
    {
        public int TotalCount { get; private set; }
        public int PageCount { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }

        //TODO: make factory and no direct iqueryable dependency
        public PagedList(IQueryable<T> source, int page, int pageSize)
            : base(pageSize)
        {
            TotalCount = source.Count();
            PageCount = GetPageCount(pageSize, TotalCount);
            Page = page < 1 ? 0 : page - 1;
            PageSize = pageSize;

            AddRange(source.Skip(Page * PageSize).Take(PageSize).ToList());
        }

        internal PagedList(IEnumerable<T> source, int page, int pageSize, int totalCount, int pageCount)
            : base(pageSize)
        {
            TotalCount = totalCount;
            PageCount = pageCount;
            Page = page;
            PageSize = pageSize;
            AddRange(source);
        }

        private int GetPageCount(int pageSize, int totalCount)
        {
            if (pageSize == 0)
                return 0;

            var remainder = totalCount % pageSize;
            return (totalCount / pageSize) + (remainder == 0 ? 0 : 1);
        }
    }
    public interface IPagedList
    {
        int TotalCount { get; }
        int PageCount { get; }
        int Page { get; }
        int PageSize { get; }
    }

    public static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {

            var contains = dictionary.ContainsKey(key);
            if (contains) return false;

            lock (dictionary)
            {
                contains = dictionary.ContainsKey(key);
                if (contains) return false;

                dictionary.Add(key, value);
                return true;
            }
        }
        public static TValue AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory, Func<TKey, TValue, TValue> updateFactory)
        {

            TValue value;
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out value))
                {
                    value = updateFactory(key, value);
                    dictionary[key] = value;
                    return value;
                }
                else
                {
                    value = valueFactory(key);
                    dictionary.Add(key, value);
                }
            }
            return value;
        }
        public static bool TryRemove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            TValue v;

            if (dictionary.TryGetValue(key, out v))
                lock (dictionary)
                {
                    if (dictionary.TryGetValue(key, out v))
                    {
                        value = v;
                        return dictionary.Remove(key);
                    }
                }

            value = default(TValue);
            return false;
        }
        /// <summary>
        /// Gets or adds the entry to the specified dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueFactory">The value factory.</param>
        /// <returns></returns>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value)) return value;
            lock (dictionary)
            {
                if (dictionary.TryGetValue(key, out value)) return value;

                value = valueFactory(key);
                dictionary.Add(key, value);
            }
            return value;
        }
    }
}
