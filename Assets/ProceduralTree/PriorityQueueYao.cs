using UnityEngine;
using System.Collections.Generic;

/**
 * A priority queue that sorts Yao nodes by their distance towards the source.
 * It allows for updating the priority weights (towards a lower distance only!).
 * As Dijkstra only lowers the distance to source, this is sufficient for this purpose.
 **/

public class PriorityQueueYao{

    private LinkedList<YaoNode> queue;

    public PriorityQueueYao()
    {
        queue = new LinkedList<YaoNode>();
    }

    public void enqueue(YaoNode node)
    {
        queue.AddLast(node);
        bubbleUp(queue.Last);
    }

    public void update(YaoNode node)
    {
        bubbleUp(queue.Find(node));
    }

    public YaoNode dequeue()
    {
        if (!isEmpty())
        {
            YaoNode n = queue.First.Value;
            queue.RemoveFirst();
            return n;
        }
        else
        {
            throw new System.Exception("Queue is empty");
        }
    }

    public bool isEmpty()
    {
        return queue.Count == 0;
    }

    private void bubbleUp(LinkedListNode<YaoNode> node)
    {
        if (queue.Count <= 1) return; // No need to sort
        LinkedListNode<YaoNode> target = node;
        bool targetFound = false;
        while (target != null && !targetFound)
        {
            if(node.Value.distanceFromSource <= target.Value.distanceFromSource)
            {
                target = target.Previous;
            }
            else
            {
                targetFound = true;
            }
        }
        
        queue.Remove(node);
        // insertion point found
        if (targetFound)
        {
            queue.AddAfter(target, node.Value);
        }
        else
        {
            // node is the new first element
            queue.AddFirst(node);
        }
    }
}
