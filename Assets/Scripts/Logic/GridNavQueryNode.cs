using System.Collections.Generic;

enum GridNavNodeFlags { Open = 0x01, Closed = 0x02, Blocked = 0x04 };

class GridNavQueryNode
{
    public int index;
    public float gCost;
    public float fCost;
    public GridNavQueryNode parent;
    public int flags;
}

class GridNavQueryNodePool
{
    private GridNavQueryNode[] nodes;
    private int count;
    private Dictionary<int, GridNavQueryNode> nodeIndexes;

    public GridNavQueryNodePool(int maxNodes)
    {
        nodes = new GridNavQueryNode[maxNodes];
        for (int i = 0; i < maxNodes; i++)
        {
            nodes[i] = new GridNavQueryNode();
        }
        count = 0;
        nodeIndexes = new Dictionary<int, GridNavQueryNode>();
    }
    public void Clear()
    {
        count = 0;
        nodeIndexes.Clear();
    }
    public GridNavQueryNode GetNode(int index)
    {
        if (!nodeIndexes.TryGetValue(index, out var node))
        {
            if (count >= nodes.Length)
            {
                return null;
            }
            node = nodes[count++];
            node.index = index;
            node.gCost = 0;
            node.fCost = 0;
            node.parent = null;
            node.flags = 0;
            nodeIndexes.Add(index, node);
        }
        return node;
    }
}

class GridNavQueryPriorityQueue
{
    private GridNavQueryNode[] heap;
    private int count;
    private int capacity;

    public GridNavQueryPriorityQueue(int capacity = 1024)
    {
        this.heap = new GridNavQueryNode[capacity];
        this.count = 0;
        this.capacity = capacity;
    }
    public void Push(GridNavQueryNode node)
    {
        if (count == capacity)
        {
            capacity <<= 1;
            var newHeap = new GridNavQueryNode[capacity];
            heap.CopyTo(newHeap, 0);
            heap = newHeap;
        }
        heap[count] = node;
        HeapifyUp(count);
        count++;
    }
    public void Modify(GridNavQueryNode node)
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
    public GridNavQueryNode Pop()
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