using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using System.Diagnostics;

public class Dijkstra
{

    //List<int> unvisited;
    PriorityQueueYao unvisitedNodes;
    public YaoGraph graph;

    public Dijkstra(YaoGraph p_graph, Vector3 stem)
    {
        graph = p_graph;

        setStemNodes(stem);

        visit();
    }

    private int setStemNodes(Vector3 stem)
    {
        int stemNodeId = graph.rootNodeId;
        Assert.AreNotEqual(stemNodeId, -1);
        //Debug.Log("Stem Node: " + stemNodeId);
        graph[stemNodeId] = new YaoNode(stemNodeId, -1, 0.0f, 0, Vector3.up, graph.getNeighborhoodSize());
        graph.data.samples[stemNodeId] = stem;


        unvisitedNodes = new PriorityQueueYao();
        // all non-stem nodes
        foreach (YaoNode node in graph)
        {
            Assert.AreNotEqual(node.ID, -1);
            unvisitedNodes.enqueue(node);
        }
        return stemNodeId;
    }

    private void visit()
    {
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();

        // preallocate memory
        //int nodeIDIdx = -1;
        //int nodeID = -1;
        YaoNode currentNode = null;
        float connectionDistance = 0.0f;

        while (!unvisitedNodes.isEmpty() ) //unvisited.Count > 0)
        {
            //Stopwatch iterationWatch = new Stopwatch();
            //iterationWatch.Start();
            // find greedy shortest path for next iteration
            //Stopwatch profiler = new Stopwatch();
            //profiler.Start();
            //nodeIDIdx = findSmallestUnvisitedIdx();
            //nodeID = unvisited[nodeIDIdx];
            currentNode = unvisitedNodes.dequeue();
            //Debug.Log("Visiting " + nodeID + " left:" + unvisited.Count);
            //UnityEngine.Debug.Log("findSmallestUnvisited:" + profiler.ElapsedMilliseconds + "ms");
            // Recalculate distances
            foreach (int neighborID in currentNode.outgoingEdges)
            {
                //Assert.IsTrue(unvisited.Contains(neighborID));
                if (neighborID != -1)
                {
                    connectionDistance = currentNode.distanceFromSource + distance(currentNode, graph[neighborID]);
                    if (connectionDistance < graph[neighborID].distanceFromSource)
                    {
                        graph[neighborID].distanceFromSource = connectionDistance;
                        graph[neighborID].hopsFromSource = currentNode.hopsFromSource + 1;
                        graph[neighborID].parentID = currentNode.ID;
                        unvisitedNodes.update(graph[neighborID]);
                    }
                }
            }
            //UnityEngine.Debug.Log("foreach.neighbor:" + profiler.ElapsedMilliseconds + "ms");

            finalize(currentNode.ID);
            //UnityEngine.Debug.Log("finalize:" + profiler.ElapsedMilliseconds + "ms");
            //UnityEngine.Debug.Log("Iteration:" + iterationWatch.ElapsedMilliseconds + "ms");
        }

        UnityEngine.Debug.Log("Dijkstra total:" + stopWatch.ElapsedMilliseconds + "ms");

    }

    // From n1 to n2 (this is not symmetric due to the guiding vector!)
    private float distance(YaoNode n1, YaoNode n2)
    {
        Vector3 vectorAlongEdge = graph.data.samples[n1.ID] - graph.data.samples[n2.ID];
        return (vectorAlongEdge).magnitude * (1 - Vector3.Dot(n1.guidingVector, vectorAlongEdge.normalized));
    }
    /*
    // returns the index in the unvisited list of the element with smallest distance towards source.
    private int findSmallestUnvisitedIdx()
    {
        Assert.IsTrue(unvisited.Count != 0);

        float smallestVal = float.MaxValue;
        int smallestIdx = 0;
        for (int i = 0; i < unvisited.Count; i++)
        {
            if (graph[unvisited[i]].distanceFromSource <= smallestVal)
            {
                smallestVal = graph[unvisited[i]].distanceFromSource;
                smallestIdx = i;
            }
        }
        return smallestIdx;
    }
    */
    void finalize(int nodeID)
    {
        //Assert.AreNotEqual(nodeID, -1);
        //Assert.IsTrue(nodeID >= 0 && nodeID < graph.getIDUpperBound());
        //Assert.AreEqual(graph[nodeID].ID, nodeID);
        // First (stem) node has no parent
        if (graph[nodeID].parentID != -1)
        {
            //    Assert.IsTrue(graph[nodeID].parentID >= 0 && graph[nodeID].parentID < graph.getIDUpperBound());

            fixGuidingVector(graph[nodeID], graph[graph[nodeID].parentID]);
        }
        else
        {
            graph[nodeID].guidingVector = Vector3.up;
        }
    }

    void fixGuidingVector(YaoNode node, YaoNode parent)
    {
        //Stopwatch profiler = new Stopwatch();
        //profiler.Start();
        Vector3 rotationAxis = Vector3.Cross(Vector3.up, parent.guidingVector);
        //UnityEngine.Debug.Log("rotationAxis:" + profiler.ElapsedMilliseconds + "ms"); profiler.Start();
        float rotationAngle = getRotationAngle(node.hopsFromSource);
        //UnityEngine.Debug.Log("rotationAngle:" + profiler.ElapsedMilliseconds + "ms"); profiler.Start();
        node.guidingVector = Quaternion.AngleAxis(rotationAngle, rotationAxis) * parent.guidingVector;
        //UnityEngine.Debug.Log("guidingVector:" + profiler.ElapsedMilliseconds + "ms"); profiler.Start();
        //node.guidingVector = node.guidingVector.normalized;
    }

    private float getRotationAngle(int nHops)
    {
        // range is 0-1 on x and y, so we need to scale here.
        //return rotationFunction.Evaluate(nHops / 60.0f)* 8.0f;
        if (nHops < 5) return 4.0f;
        else return -4.0f;
    }


    public void visualize()
    {
        foreach (YaoNode node in graph)
        {
            if (node.parentID != -1)
            {
                Vector3 pos1 = graph.data.samples[node.ID];
                Vector3 pos2 = graph.data.samples[node.parentID];
                drawLine(pos1, pos2, Color.red);
            }
        }

    }

    private void drawLine(Vector3 start, Vector3 end, Color c, float width = 0.03f, string name = "line")
    {
        GameObject myLine = new GameObject(name);
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        myLine.GetComponent<Renderer>().material.color = c;
        lr.material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
        lr.material.color = c;
        lr.SetWidth(width, width);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}
