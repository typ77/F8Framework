using System;
using System.Collections.Generic;

namespace F8Framework.Core
{
    /// <summary>
    /// 最小堆实现（泛型版本）
    /// </summary>
    /// <typeparam name="T">堆中元素类型</typeparam>
    public class MinHeap<T>
    {
        private T[] _heap;       // 存储堆的数组
        private int _size;       // 当前堆大小
        private readonly IComparer<T> _comparer;  // 元素比较器

        // 默认初始容量
        private const int DefaultCapacity = 16;

        /// <summary>
        /// 堆中元素数量
        /// </summary>
        public int Count => _size;

        /// <summary>
        /// 构造最小堆
        /// </summary>
        /// <param name="capacity">初始容量</param>
        /// <param name="comparer">自定义比较器（可选）</param>
        public MinHeap(int capacity = DefaultCapacity, IComparer<T> comparer = null)
        {
            if (capacity < 1) capacity = DefaultCapacity;
            _heap = new T[capacity];
            _comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// 插入新元素到堆中
        /// </summary>
        /// <param name="item">要插入的元素</param>
        public void Insert(T item)
        {
            // 扩容检查
            if (_size == _heap.Length)
            {
                Array.Resize(ref _heap, _heap.Length * 2);
            }

            _heap[_size] = item;
            BubbleUp(_size);  // 上浮调整
            _size++;
        }

        /// <summary>
        /// 移除并返回堆顶最小元素
        /// </summary>
        /// <returns>堆顶最小元素</returns>
        public T RemoveMin()
        {
            if (_size == 0)
                throw new InvalidOperationException("Heap is empty");

            var min = _heap[0];
            _size--;
            _heap[0] = _heap[_size];  // 将最后一个元素移到堆顶
            BubbleDown(0);  // 下沉调整
            return min;
        }

        /// <summary>
        /// 获取堆顶最小元素（不删除）
        /// </summary>
        /// <returns>堆顶最小元素</returns>
        public T PeekMin()
        {
            if (_size == 0)
                throw new InvalidOperationException("Heap is empty");
            return _heap[0];
        }

        // 上浮调整（从指定索引开始向上调整堆结构）
        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) / 2;  // 父节点索引
                if (_comparer.Compare(_heap[index], _heap[parentIndex]) >= 0)
                    break;  // 已满足最小堆性质，停止调整

                // 交换父子节点
                T temp = _heap[index];
                _heap[index] = _heap[parentIndex];
                _heap[parentIndex] = temp;
                index = parentIndex;
            }
        }

        // 下沉调整（从指定索引开始向下调整堆结构）
        private void BubbleDown(int index)
        {
            while (true)
            {
                var leftChild = 2 * index + 1;
                var rightChild = 2 * index + 2;
                var smallest = index;

                // 比较左子节点
                if (leftChild < _size && _comparer.Compare(_heap[leftChild], _heap[smallest]) < 0)
                    smallest = leftChild;

                // 比较右子节点
                if (rightChild < _size && _comparer.Compare(_heap[rightChild], _heap[smallest]) < 0)
                    smallest = rightChild;

                if (smallest == index)
                    break;  // 已满足最小堆性质，停止调整

                // 交换当前节点与最小子节点
                T temp = _heap[index];
                _heap[index] = _heap[smallest];
                _heap[smallest] = temp;
                index = smallest;
            }
        }
        /// <summary>
        /// 清空堆中所有元素（保留底层数组容量）
        /// </summary>
        public void Clear()
        {
            _size = 0; // 直接重置大小，底层数组元素会被后续插入覆盖
        }
    }
}