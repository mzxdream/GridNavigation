using System.Collections.Generic;

namespace GridNav
{
    enum NavNodeFlags { Open = 0x01, Closed = 0x02, Blocked = 0x04 };

    class NavQueryNode
    {
        public int x;
        public int z;
        public float gCost;
        public float fCost;
        public NavQueryNode parent;
        public int flags;
    }

    class NavQueryNodePool
    {
        private NavQueryNode[] nodes;
        private int count;
        private Dictionary<int, NavQueryNode> nodeIndexes;

        public NavQueryNodePool(int maxNodes)
        {
            nodes = new NavQueryNode[maxNodes];
            for (int i = 0; i < maxNodes; i++)
            {
                nodes[i] = new NavQueryNode();
            }
            count = 0;
            nodeIndexes = new Dictionary<int, NavQueryNode>();
        }
        public void Clear()
        {
            count = 0;
            nodeIndexes.Clear();
        }
        public NavQueryNode GetNode(int x, int z)
        {
            var index = NavDef.SquareIndex(x, z);
            if (!nodeIndexes.TryGetValue(index, out var node))
            {
                if (count >= nodes.Length)
                {
                    return null;
                }
                node = nodes[count++];
                node.x = x;
                node.z = z;
                node.gCost = 0;
                node.fCost = 0;
                node.parent = null;
                node.flags = 0;
                nodeIndexes.Add(index, node);
            }
            return node;
        }
    }

    class NavQueryPriorityQueue
    {
        private NavQueryNode[] heap;
        private int count;
        private int capacity;

        public NavQueryPriorityQueue(int capacity = 1024)
        {
            this.heap = new NavQueryNode[capacity];
            this.count = 0;
            this.capacity = capacity;
        }
        public void Push(NavQueryNode node)
        {
            if (count == capacity)
            {
                capacity <<= 1;
                var newHeap = new NavQueryNode[capacity];
                heap.CopyTo(newHeap, 0);
                heap = newHeap;
            }
            heap[count] = node;
            HeapifyUp(count);
            count++;
        }
        public void Modify(NavQueryNode node)
        {
            for (int i = 0; i < count; ++i)
            {
                if (heap[i] == node)
                {
                    HeapifyUp(i);
                    return;
                }
            }
        }
        public bool IsEmpty()
        {
            return count == 0;
        }
        public NavQueryNode Pop()
        {
            if (count == 0)
            {
                return null;
            }
            count--;
            var node = heap[0];
            heap[0] = heap[count];
            HeapifyDown(0, count);
            return node;
        }
        public void Clear()
        {
            for (int i = 0; i < count; i++)
            {
                heap[i] = null;
            }
            count = 0;
        }
        private void HeapifyUp(int i)
        {
            while (i > 0)
            {
                int j = (i - 1) / 2; //parent
                if (heap[j].fCost <= heap[i].fCost)
                {
                    break;
                }
                var tmp = heap[j];
                heap[j] = heap[i];
                heap[i] = tmp;
                i = j;
            }
        }
        private void HeapifyDown(int i, int length)
        {
            int lowest = i;
            int left = i * 2 + 1;
            int right = i * 2 + 2;
            if (left < length && heap[left].fCost < heap[lowest].fCost)
            {
                lowest = left;
            }
            if (right < length && heap[right].fCost < heap[lowest].fCost)
            {
                lowest = right;
            }
            if (lowest != i)
            {
                var tmp = heap[i];
                heap[i] = heap[lowest];
                heap[lowest] = tmp;
                HeapifyDown(lowest, length);
            }
        }
    }
}