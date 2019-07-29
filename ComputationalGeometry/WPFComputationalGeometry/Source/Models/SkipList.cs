#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace WPFComputationalGeometry.Source.Models
{
    public class SkipList<T> : ICollection<T>
    {
        internal const byte HeightStep = 4;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly InvalidOperationException EmptyCollectionException =
            new InvalidOperationException("Collection is empty.");

        private readonly IComparer<T> _comparer;

        internal readonly Node Head;

        private readonly Random _random;

        internal readonly Node Tail;

        internal byte Height;

        protected internal Node LastFoundNode;

        public SkipList(IComparer<T> comparer = null)
        {
            if (comparer == null && !typeof (IComparable).IsAssignableFrom(typeof (T)) &&
                !typeof (IComparable<T>).IsAssignableFrom(typeof (T)))
            {
                throw new ArgumentException(@"Expected a comparer for types, which do not implement IComparable.",
                    nameof(comparer));
            }
            _comparer = comparer ?? Comparer<T>.Default;
            _random = new Random();
            Head = new Node(default(T), 4);
            Tail = new Node(default(T), 4);
            Reset();
        }

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public virtual void Clear()
        {
            Head.SetHeight(4);
            Tail.SetHeight(4);
            Reset();
        }

        public virtual bool Contains(T item)
        {
            var node = FindNode(item);
            LastFoundNode = node;
            return CompareNode(node, item) == 0;
        }

        public virtual void Add(T item)
        {
            var prev = FindNode(item);
            LastFoundNode = AddNewNode(item, prev);
        }

        public virtual bool Remove(T item)
        {
            var node = FindNode(item);
            if (CompareNode(node, item) != 0)
            {
                return false;
            }
            DeleteNode(node);
            if (LastFoundNode == node)
            {
                SetLastFoundNode(Head);
            }
            return true;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            var linkedList = new LinkedList<T>();
            for (var next = Head.GetNext(0); next != Tail; next = next.GetNext(0))
            {
                linkedList.AddLast(next.Item);
            }
            return linkedList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        public virtual T Peek()
        {
            if (Count == 0)
            {
                throw EmptyCollectionException;
            }
            return Head.GetNext(0).Item;
        }

        public virtual T GetFirst()
        {
            return Peek();
        }

        public virtual T GetLast()
        {
            if (Count == 0)
            {
                throw EmptyCollectionException;
            }
            return Tail.GetPrev(0).Item;
        }

        public virtual T Take()
        {
            if (Count == 0)
            {
                throw EmptyCollectionException;
            }
            var next = Head.GetNext(0);
            DeleteNode(next);
            if (LastFoundNode == next)
            {
                SetLastFoundNode(Head);
            }
            return next.Item;
        }

        public virtual T TakeLast()
        {
            if (Count == 0)
            {
                throw EmptyCollectionException;
            }
            var prev = Tail.GetPrev(0);
            DeleteNode(prev);
            if (LastFoundNode == prev)
            {
                SetLastFoundNode(Head);
            }
            return prev.Item;
        }

        public virtual T Floor(T item)
        {
            var node = FindNode(item);
            SetLastFoundNode(node);
            return node.Item;
        }

        public virtual T Ceiling(T item)
        {
            var node = FindNode(item);
            if (CompareNode(node, item) < 0)
            {
                node = node.GetNext(0);
            }
            SetLastFoundNode(node);
            return node.Item;
        }

        public virtual T Find(T item)
        {
            var node = FindNode(item);
            SetLastFoundNode(node);
            return node.Item;
        }

        public virtual T Next(T item)
        {
            var node = FindNode(item).GetNext(0);
            SetLastFoundNode(node);
            return node.Item;
        }

        public virtual T Previous(T item)
        {
            var node = FindNode(item).GetPrev(0);
            SetLastFoundNode(node);
            return node.Item;
        }

        public virtual IEnumerable<T> Range(T fromItem, T toItem, bool includeFromItem = true, bool includeToItem = true)
        {
            var node = FindNode(fromItem);
            var num = includeFromItem ? 0 : 1;
            while (CompareNode(node, fromItem) < num)
            {
                node = node.GetNext(0);
            }
            var linkedList = new LinkedList<T>();
            num = includeToItem ? 1 : 0;
            while (node != Tail && CompareNode(node, toItem) < num)
            {
                linkedList.AddLast(node.Item);
                node = node.GetNext(0);
            }
            return linkedList;
        }

        public virtual void CopyTo(Array array, int arrayIndex)
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
            var next = Head.GetNext(0);
            for (var i = arrayIndex; i < arrayIndex + Count; i++)
            {
                array.SetValue(next.Item, i);
                next = next.GetNext(0);
            }
        }

        private void Reset()
        {
            for (var i = 0; i < Head.Height; i++)
            {
                Head.SetNext(i, Tail);
                Tail.SetPrev(i, Head);
            }
            Count = 0;
            Height = 1;
            LastFoundNode = Head;
        }

        protected internal virtual void SetLastFoundNode(Node node)
        {
            LastFoundNode = node;
        }

        protected Node FindNode(T key)
        {
            var i = Height - 1;
            var node = Head;
            var lastFoundNode = LastFoundNode;
            if (lastFoundNode != Head)
            {
                int num;
                if ((num = CompareNode(lastFoundNode, key)) == 0)
                {
                    return lastFoundNode;
                }
                if (num < 0)
                {
                    node = lastFoundNode;
                    i = lastFoundNode.Height - 1;
                }
            }
            while (i >= 0)
            {
                var next = node.GetNext(i);
                int num;
                while ((num = CompareNode(next, key)) < 0)
                {
                    node = next;
                    next = next.GetNext(i);
                }
                if (num == 0)
                {
                    node = next;
                    break;
                }
                i--;
            }
            return node;
        }

        protected Node AddNewNode(T item, Node prev)
        {
            var next = prev.GetNext(0);
            var newNodeHeight = GetNewNodeHeight();
            var node = new Node(item, newNodeHeight);
            InsertNode(node, newNodeHeight, prev, next);
            Count++;
            return node;
        }

        private byte GetNewNodeHeight()
        {
            var b = Height;
            if (b < 32)
            {
                b += 1;
            }
            byte b2 = 1;
            while (_random.NextDouble() < 0.5 && b2 < b)
            {
                b2 += 1;
            }
            if (b2 > Height)
            {
                Height = b2;
                if (Head.Height < Height)
                {
                    b = (byte) Head.Height;
                    Head.SetHeight(b + 4);
                    Tail.SetHeight(b + 4);
                    while (b < Head.Height)
                    {
                        Head.SetNext(b, Tail);
                        Tail.SetPrev(b, Head);
                        b += 1;
                    }
                }
            }
            return b2;
        }

        private static void InsertNode(Node newNode, byte height, Node prev, Node next)
        {
            for (var i = 0; i < (int) height; i++)
            {
                while (prev.Height <= i)
                {
                    prev = prev.GetPrev(i - 1);
                }
                while (next.Height <= i)
                {
                    next = next.GetNext(i - 1);
                }
                newNode.SetPrev(i, prev);
                newNode.SetNext(i, next);
                prev.SetNext(i, newNode);
                next.SetPrev(i, newNode);
            }
        }

        protected void DeleteNode(Node node)
        {
            byte b = 0;
            while (b < node.Height)
            {
                var prev = node.GetPrev(b);
                var next = node.GetNext(b);
                while (prev.Height <= b)
                {
                    prev = prev.GetPrev(b - 1);
                }
                while (next.Height <= b)
                {
                    next = next.GetNext(b - 1);
                }
                prev.SetNext(b, next);
                next.SetPrev(b, prev);
                b += 1;
            }
            Count--;
            if (Height > 1 && 1 << Height > Count)
            {
                Height -= 1;
            }
        }

        protected int CompareNode(Node node, T key)
        {
            if (node == Head)
            {
                return -1;
            }
            if (node == Tail)
            {
                return 1;
            }
            return _comparer.Compare(node.Item, key);
        }

        [DebuggerDisplay("Node [{Item}] ({Height})")]
        protected internal class Node
        {
            private Node[] _next;

            private Node[] _prev;

            protected internal Node(T item, byte height)
            {
                Item = item;
                _next = new Node[height];
                _prev = new Node[height];
            }

            protected internal T Item { get; }

            protected internal int Height => _next.Length;

            protected internal Node GetNext(int level)
            {
                return _next[level];
            }

            protected internal void SetNext(int level, Node node)
            {
                _next[level] = node;
            }

            protected internal void SetPrev(int level, Node node)
            {
                _prev[level] = node;
            }

            protected internal Node GetPrev(int level)
            {
                return _prev[level];
            }

            protected internal void SetHeight(int height)
            {
                var array = new Node[height];
                var array2 = new Node[height];
                var num = Math.Min(_next.Length, height);
                for (var i = 0; i < num; i++)
                {
                    array[i] = _next[i];
                    array2[i] = _prev[i];
                }
                _next = array;
                _prev = array2;
            }
        }
    }
}