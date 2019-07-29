using System;
using System.Collections.Generic;

namespace WPFComputationalGeometry.Source.Models
{
    internal static class HeapMethods
    {
        internal static void Swap<T>(this T[] array, int i, int j)
        {
            T t = array[i];
            array[i] = array[j];
            array[j] = t;
        }

        internal static bool GreaterOrEqual<T>(this IComparer<T> comparer, T x, T y)
        {
            return comparer.Compare(x, y) >= 0;
        }

        internal static void Sink<T>(this T[] heap, int i, int count, IComparer<T> comparer, int shift)
        {
            int num = count + shift;
            while (true)
            {
                int num2 = i + shift;
                int num3 = 2 * i + shift;
                if (num3 > num)
                {
                    break;
                }
                int num4 = num3 + 1;
                bool flag = num4 <= num;
                T x = heap[num2];
                T t = heap[num3];
                T y = flag ? heap[num4] : default(T);
                if (comparer.GreaterOrEqual(x, t) && (!flag || comparer.GreaterOrEqual(x, y)))
                {
                    return;
                }
                int num5 = (!flag || comparer.GreaterOrEqual(t, y)) ? num3 : num4;
                heap.Swap(num2, num5);
                i = num5 - shift;
            }
        }

        internal static void Sift<T>(this T[] heap, int i, IComparer<T> comparer, int shift)
        {
            while (i > 1)
            {
                int num = i / 2 + shift;
                int num2 = i + shift;
                if (comparer.GreaterOrEqual(heap[num], heap[num2]))
                {
                    return;
                }
                heap.Swap(num, num2);
                i = num - shift;
            }
        }

        internal static void HeapSort<T>(this T[] heap, int startIndex, int count, IComparer<T> comparer)
        {
            int shift = startIndex - 1;
            int i = startIndex + count;
            int num = count;
            while (i > startIndex)
            {
                i--;
                num--;
                heap.Swap(startIndex, i);
                heap.Sink(1, num, comparer, shift);
            }
            Array.Reverse(heap, startIndex, count);
        }
    }
}
