#region

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace WPFComputationalGeometry.Source.Models
{
    public class PriorityQueue<T> : ICollection<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly InvalidOperationException EmptyCollectionException =
            new InvalidOperationException("Collection is empty.");

        private readonly IComparer<T> _comparer;

        internal T[] Heap;

        private int _shrinkBound;

        public PriorityQueue(IComparer<T> comparer = null) : this(10, comparer)
        {
        }

        public PriorityQueue(int capacity, IComparer<T> comparer = null)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), @"Expected capacity greater than zero.");
            }
            if (comparer == null && !typeof (IComparable).IsAssignableFrom(typeof (T)) &&
                !typeof (IComparable<T>).IsAssignableFrom(typeof (T)))
            {
                throw new ArgumentException(@"Expected a comparer for types, which do not implement IComparable.",
                    nameof(comparer));
            }
            _comparer = comparer ?? Comparer<T>.Default;
            _shrinkBound = capacity/4;
            Heap = new T[capacity];
        }

        public int Capacity => Heap.Length;

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public virtual IEnumerator<T> GetEnumerator()
        {
            var array = new T[Count];
            CopyTo(array, 0);
            return ((IEnumerable<T>) array).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void Add(T item)
        {
            if (Count == Capacity)
            {
                GrowCapacity();
            }
            Heap[Count++] = item;
            Heap.Sift(Count, _comparer, -1);
        }

        public virtual void Clear()
        {
            Heap = new T[10];
            Count = 0;
        }

        public virtual bool Contains(T item)
        {
            return GetItemIndex(item) >= 0;
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("Insufficient space in destination array.");
            }
            Array.Copy(Heap, 0, array, arrayIndex, Count);
            array.HeapSort(arrayIndex, Count, _comparer);
        }

        public virtual bool Remove(T item)
        {
            var itemIndex = GetItemIndex(item);
            switch (itemIndex)
            {
                case -1:
                    return false;
                case 0:
                    Take();
                    break;
                default:
                    RemoveAt(itemIndex + 1, -1);
                    break;
            }
            return true;
        }

        public virtual T Take()
        {
            if (Count == 0)
            {
                throw EmptyCollectionException;
            }
            var result = Heap[0];
            Count--;
            Heap.Swap(0, Count);
            Heap[Count] = default(T);
            Heap.Sink(1, Count, _comparer, -1);
            if (Count <= _shrinkBound && Count > 10)
            {
                ShrinkCapacity();
            }
            return result;
        }

        public virtual T Peek()
        {
            if (Count == 0)
            {
                throw EmptyCollectionException;
            }
            return Heap[0];
        }

        protected internal void RemoveAt(int index, int shift)
        {
            var num = index + shift;
            Count--;
            Heap.Swap(num, Count);
            Heap[Count] = default(T);
            var num2 = index/2 + shift;
            if (_comparer.GreaterOrEqual(Heap[num], Heap[num2]))
            {
                Heap.Sift(index, _comparer, shift);
                return;
            }
            Heap.Sink(index, Count, _comparer, shift);
        }

        protected internal int GetItemIndex(T item)
        {
            for (var i = 0; i < Count; i++)
            {
                if (_comparer.Compare(Heap[i], item) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private void GrowCapacity()
        {
            var num = Capacity*2;
            Array.Resize(ref Heap, num);
            _shrinkBound = num/4;
        }

        private void ShrinkCapacity()
        {
            var num = Capacity/2;
            Array.Resize(ref Heap, num);
            _shrinkBound = num/4;
        }
    }
}