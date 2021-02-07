using System;
using System.Collections.Generic;
using UnityEngine;

public class GridPathNode
{
    private enum Mask { TestBlocked = 1, Blocked = 2, Closed = 4 };

    private readonly int x;
    private readonly int z;
    private int gCost;
    private int hCost;
    private GridPathNode parent;
    private Mask mask;

    public int X { get => x; }
    public int Z { get => z; }
    public int FCost { get => gCost + hCost; }
    public int GCost { get => gCost; set => gCost = value; }
    public int HCost { set => hCost = value; }
    public GridPathNode Parent { get => parent; set => parent = value; }
    public bool HasTestBlocked
    {
        get { return (mask & Mask.TestBlocked) != 0; }
        set { if (value) { mask |= Mask.TestBlocked; } else { mask &= ~Mask.TestBlocked; } }
    }
    public bool IsBlocked
    {
        get { return (mask & Mask.Blocked) != 0; }
        set { if (value) { mask |= Mask.Blocked; } else { mask &= ~Mask.Blocked; } }
    }
    public bool IsClosed
    {
        get { return (mask & Mask.Closed) != 0; }
        set { if (value) { mask |= Mask.Closed; } else { mask &= ~Mask.Closed; } }
    }

    public GridPathNode(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}

public class GridPathPriorityQueue
{
    private GridPathNode[] nodeHeap;
    private int count;
    private int capacity;

    public GridPathPriorityQueue(int capacity = 256)
    {
        this.nodeHeap = new GridPathNode[capacity];
        this.count = 0;
        this.capacity = capacity;
    }
    private void Grow()
    {
        capacity <<= 1;
        var newNodeHeap = new GridPathNode[capacity];
        nodeHeap.CopyTo(newNodeHeap, 0);
        nodeHeap = newNodeHeap;
    }
    private void HeapifyDown(int i, int length)
    {
        int lowest = i;
        int left = i * 2 + 1;
        int right = i * 2 + 2;
        if (left < length && nodeHeap[left].FCost < nodeHeap[lowest].FCost)
        {
            lowest = left;
        }
        if (right < length && nodeHeap[right].FCost < nodeHeap[lowest].FCost)
        {
            lowest = right;
        }
        if (lowest != i)
        {
            var tmp = nodeHeap[i];
            nodeHeap[i] = nodeHeap[lowest];
            nodeHeap[lowest] = tmp;
            HeapifyDown(lowest, length);
        }
    }
    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int j = (i - 1) / 2; //parent
            if (nodeHeap[j].FCost <= nodeHeap[i].FCost)
            {
                break;
            }
            var tmp = nodeHeap[j];
            nodeHeap[j] = nodeHeap[i];
            nodeHeap[i] = tmp;
            i = j;
        }
    }
    public void Push(GridPathNode node)
    {
        if (count == capacity)
        {
            Grow();
        }
        nodeHeap[count] = node;
        HeapifyUp(count);
        count++;
    }
    public GridPathNode Pop()
    {
        if (count == 0)
        {
            return null;
        }
        count--;
        var node = nodeHeap[0];
        nodeHeap[0] = nodeHeap[count];
        HeapifyDown(0, count);
        return node;
    }
    public void Clear()
    {
        for (int i = 0; i < count; i++)
        {
            nodeHeap[i] = null;
        }
        count = 0;
    }
}

public class GridPath
{
    public class Node
    {
        private readonly int x;
        private readonly int z;
        private Node prev;
        private Node next;

        public int X { get => x; }
        public int Z { get => z; }
        public Node Prev { get => prev; }
        public Node Next { get => next; }

        public Node(int x, int z)
        {
            this.x = x;
            this.z = z;
            prev = this;
            next = this;
        }
        public void Insert(Node p, Node n)
        {
            Debug.Assert(prev == this && next == this);
            p.next = this;
            n.prev = this;
            prev = p;
            next = n;
        }
        public void Erase()
        {
            prev.next = next;
            next.prev = prev;
            prev = this;
            next = this;
        }
    }
    private Node head = new Node(-1, -1);

    public Node Head { get => head; }
    public Node goalNode;

    public void PushFront(Node n)
    {
        n.Insert(head, head.Next);
    }
    public void PushBack(Node n)
    {
        n.Insert(head.Prev, head);
    }
}

public class GridPathFinder
{
    private int[] neighbors = { 0, 1, 0, -1, 1, 0, -1, 0, -1, 1, 1, 1, -1, -1, 1, -1 };
    private int gridX;
    private int gridZ;
    private GridPathNode[] nodes;
    private GridPathPriorityQueue openQueue;
    private List<GridPathNode> closedQueue;

    public GridPathFinder(int gridX, int gridZ)
    {
        this.gridX = gridX;
        this.gridZ = gridZ;
        this.nodes = new GridPathNode[this.gridX * this.gridZ];
        for (int z = 0; z < gridZ; z++)
        {
            for (int x = 0; x < gridX; x++)
            {
                nodes[x + z * gridX] = new GridPathNode(x, z);
            }
        }
        this.openQueue = new GridPathPriorityQueue();
        this.closedQueue = new List<GridPathNode>();
    }
    private static int CalcDistance(int fromX, int fromZ, int toX, int toZ)
    {
        int x = Mathf.Abs(toX - fromX);
        int z = Mathf.Abs(toZ - fromZ);
        return x > z ? 14 * z + 10 * (x - z) : 14 * x + 10 * (z - x);
    }
    private bool IsNodeBlocked(GridPathNode node, Func<int, int, bool> checkBlockedFunc)
    {
        if (!node.HasTestBlocked)
        {
            node.HasTestBlocked = true;
            node.IsBlocked = checkBlockedFunc(node.X, node.Z);
        }
        return node.IsBlocked;
    }
    private bool IsNeighborWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> checkBlockedFunc)
    {
        Debug.Assert(unitSize > 0 && Mathf.Abs(snode.X - enode.X) <= 1 && Mathf.Abs(snode.Z - enode.Z) <= 1);
        var offset = unitSize / 2;
        if (snode.Z == enode.Z) //Horizontal
        {
            int x = enode.X + offset * (enode.X - snode.X);
            if (x < 0 || x >= gridX)
            {
                return false;
            }
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x + (enode.Z - offset + i) * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        if (snode.X == enode.X) //Vertical
        {
            int z = enode.Z + offset * (enode.Z - snode.Z);
            if (z < 0 || z >= gridZ)
            {
                return false;
            }
            for (int i = 0; i < unitSize; i++)
            {
                if (IsNodeBlocked(nodes[enode.X - offset + i + z * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
        { //Cross
            int x = enode.X + offset * (enode.X - snode.X);
            int z = enode.Z + offset * (enode.Z - snode.Z);
            if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
            {
                return false;
            }
            for (int i = 0; i <= unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x - i * (enode.X - snode.X) + z * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            for (int i = 1; i <= unitSize; i++)
            {
                if (IsNodeBlocked(nodes[x + (z - i * (enode.Z - snode.Z)) * gridX], checkBlockedFunc))
                {
                    return false;
                }
            }
            return true;
        }
    }
    private bool IsCrossWalkable(int unitSize, GridPathNode snode, GridPathNode enode, Func<int, int, bool> checkBlockedFunc)
    {
        int dx = enode.X - snode.X, dz = enode.Z - snode.Z;
        int nx = Mathf.Abs(dx), nz = Mathf.Abs(dz);
        int signX = dx > 0 ? 1 : -1, signZ = dz > 0 ? 1 : -1;

        int x = snode.X, z = snode.Z;
        for (int ix = 0, iz = 0; ix < nx || iz < nz;)
        {
            var t1 = (2 * ix + 1) * nz;
            var t2 = (2 * iz + 1) * nx;
            if (t1 < t2) //Horizontal
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * gridX], nodes[x + signX + z * gridX], checkBlockedFunc))
                {
                    return false;
                }
                x += signX;
                ix++;
            }
            else if (t1 > t2) //Vertical
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * gridX], nodes[x + (z + signZ) * gridX], checkBlockedFunc))
                {
                    return false;
                }
                z += signZ;
                iz++;
            }
            else //Cross
            {
                if (!IsNeighborWalkable(unitSize, nodes[x + z * gridX], nodes[x + signX + (z + signZ) * gridX], checkBlockedFunc))
                {
                    return false;
                }
                x += signX;
                z += signZ;
                ix++;
                iz++;
            }
        }
        return true;
    }
    public GridPath Search(int unitSize, int startX, int startZ, int goalX, int goalZ, int goalRadius, int searchRadius, int searchMaxNodes, Func<int, int, bool> checkBlockedFunc)
    {
        Debug.Assert(unitSize >= 3 && (unitSize & 1) == 1);
        Debug.Assert(startX - unitSize / 2 >= 0 && startX + unitSize / 2 < gridX && startZ - unitSize / 2 >= 0 && startZ + unitSize / 2 < gridZ);
        Debug.Assert(goalX >= 0 && goalX < gridX && goalZ >= 0 && goalZ < gridZ);
        Debug.Assert(goalRadius >= 0 && searchRadius >= 0 && searchMaxNodes >= 0);

        openQueue.Clear();
        foreach (var n in closedQueue)
        {
            n.HasTestBlocked = false;
            n.IsClosed = false;
        }
        closedQueue.Clear();

        var node = nodes[startX + startZ * gridX];
        node.IsClosed = true;
        closedQueue.Add(node);
        for (int i = 0; i < searchMaxNodes && node != null; i++)
        {
            if (CalcDistance(node.X, node.Z, goalX, goalZ) <= goalRadius * 14)
            {
                var snode = nodes[startX + startZ * gridX];
                var path = new GridPath();
                path.goalNode = new GridPath.Node(goalX, goalZ);
                while (node != snode)
                {
                    path.PushFront(new GridPath.Node(node.X, node.Z));
                    node = node.Parent;
                }
                path.PushFront(new GridPath.Node(startX, startZ));
                return path;
            }
            for (int j = 0; j < neighbors.Length; j += 2)
            {
                var x = node.X + neighbors[j];
                var z = node.Z + neighbors[j + 1];
                if (x < 0 || x >= gridX || z < 0 || z >= gridZ)
                {
                    continue;
                }
                var n = nodes[x + z * gridX];
                if (n.IsClosed)
                {
                    continue;
                }
                n.IsClosed = true;
                closedQueue.Add(n);
                if (searchRadius > 0 && CalcDistance(startX, startZ, x, z) > searchRadius * 14)
                {
                    continue;
                }
                if (!IsNeighborWalkable(unitSize, node, n, checkBlockedFunc))
                {
                    continue;
                }
                n.GCost = node.GCost + CalcDistance(node.X, node.Z, x, z);
                n.HCost = CalcDistance(x, z, goalX, goalZ);
                n.Parent = node;
                openQueue.Push(n);
            }
            node = openQueue.Pop();
        }
        return null;
    }
}