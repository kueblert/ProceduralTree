using UnityEngine;
using System.Collections;

public class YaoNode
{
    public int ID;                      // sample index number
    public int parentID;
    public float distanceFromSource;
    public int hopsFromSource;
    public Vector3 guidingVector;
    public int[] outgoingEdges;
    public int isStem = -1; // -1 for non-stem

    public YaoNode(int p_ID, int p_parent, float distance, int hops, Vector3 guide, int neighborhoodSize, int stem = -1)
    {
        ID = p_ID;
        parentID = p_parent;
        distanceFromSource = distance;
        hopsFromSource = hops;
        guidingVector = guide;
        isStem = stem;
        outgoingEdges = new int[neighborhoodSize];
        for (int i = 0; i < outgoingEdges.Length; i++) outgoingEdges[i] = -1;
    }

    public void setEdge(int edgeIdx, int neighbor_id)
    {
        outgoingEdges[edgeIdx] = neighbor_id;
    }

}
