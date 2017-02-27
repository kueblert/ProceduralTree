using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class YaoGraph : IEnumerable<YaoNode>
{
    // Yao 8 in the current implementation would not be fully connected and therefore unable to reach all nodes in the Dijkstra step.
    public enum ConnectionType { Yao6, Yao14 /*, Yao8 */ };
    public enum EdgeDir
    {
        LEFT, RIGHT, TOP, BOTTOM, FRONT, BEHIND,    // Yao6 edges
        LEFTTOPFRONT, RIGHTTOPFRONT, LEFTBOTFRONT, RIGHTBOTFRONT,     // Yao8 edges
        LEFTTOPBEHIND, RIGHTTOPBEHIND, LEFTBOTBEHIND, RIGHTBOTBEHIND, // Yao8 edges
        UNDEFINED
    };
    
    


    public YaoNode[] graph { get; set; }
    public int[] sample2Yao { get; set; }
    public Grid3D data { get; set; }
    public int xdim, ydim, zdim;
    public int rootNodeId;
    public ConnectionType connectivityType = ConnectionType.Yao14;
    private List<EdgeDir> edgeset;


    public int getIDUpperBound()
    {
        return sample2Yao.Length;
    }
    
    public int getNodeCount() { return graph.Length; }
    public int getNeighborhoodSize()
    {
        return edgeset.Count;
    }

    public YaoGraph(Grid3D grid, ConnectionType connectivity)
    {
        data = grid;
        connectivityType = connectivity;
        edgeset = getEdgeset();

        xdim = data.GetLength(0);
        ydim = data.GetLength(1);
        zdim = data.GetLength(2);

        graph = new YaoNode[data.samples.Length];
        sample2Yao = new int[data.samples.Length];

        int insertionIdx = 0;
        for (int x = 0; x < xdim; x++)
        {
            for (int y = 0; y < ydim; y++)
            {
                for (int z = 0; z < zdim; z++)
                {
                    if (grid[x, y, z] != -1)
                    {
                        graph[insertionIdx] = new YaoNode(grid[x, y, z], -1, float.MaxValue, int.MaxValue, new Vector3(0, 0, 0), edgeset.Count);
                        sample2Yao[grid[x, y, z]] = insertionIdx;
                        assignNeigbors(graph[insertionIdx], x, y, z);
                        insertionIdx++;
                    }
                }
            }
        }

        rootNodeId = grid[Mathf.FloorToInt(xdim / 2), 0, Mathf.FloorToInt(zdim / 2)];
    }

    // prepare graph nodes for the next Dijkstra iteration
    public void prepareNextIteration()
    {
        for(int i=0; i < graph.Length; i++)
        {
            if (graph[i].isStem!=-1) {
                graph[i].distanceFromSource = 0;
                graph[i].hopsFromSource = 0;
                    }
            else
            {
                graph[i].distanceFromSource = float.MaxValue;
                graph[i].hopsFromSource = int.MaxValue;
                graph[i].guidingVector = new Vector3(0, 0, 0);
            }
        }
    }

    public List<EdgeDir> getEdgeset()
    {
        List<EdgeDir> edges = new List<EdgeDir>();
        switch (connectivityType)
        {
            case ConnectionType.Yao6:
                edges.Add(EdgeDir.LEFT);
                edges.Add(EdgeDir.RIGHT);
                edges.Add(EdgeDir.TOP);
                edges.Add(EdgeDir.BOTTOM);
                edges.Add(EdgeDir.FRONT);
                edges.Add(EdgeDir.BEHIND);
                Assert.AreEqual(edges.Count, 6);
                break;
                /*
            case ConnectionType.Yao8:
                edges.Add(EdgeDir.LEFTTOPFRONT);
                edges.Add(EdgeDir.RIGHTTOPFRONT);
                edges.Add(EdgeDir.LEFTBOTFRONT);
                edges.Add(EdgeDir.RIGHTBOTFRONT);
                edges.Add(EdgeDir.LEFTTOPBEHIND);
                edges.Add(EdgeDir.RIGHTTOPBEHIND);
                edges.Add(EdgeDir.LEFTBOTBEHIND);
                edges.Add(EdgeDir.RIGHTBOTBEHIND);
                Assert.AreEqual(edges.Count, 8);
                break;
                */
            case ConnectionType.Yao14:
                edges.Add(EdgeDir.LEFT);
                edges.Add(EdgeDir.RIGHT);
                edges.Add(EdgeDir.TOP);
                edges.Add(EdgeDir.BOTTOM);
                edges.Add(EdgeDir.FRONT);
                edges.Add(EdgeDir.BEHIND);
                edges.Add(EdgeDir.LEFTTOPFRONT);
                edges.Add(EdgeDir.RIGHTTOPFRONT);
                edges.Add(EdgeDir.LEFTBOTFRONT);
                edges.Add(EdgeDir.RIGHTBOTFRONT);
                edges.Add(EdgeDir.LEFTTOPBEHIND);
                edges.Add(EdgeDir.RIGHTTOPBEHIND);
                edges.Add(EdgeDir.LEFTBOTBEHIND);
                edges.Add(EdgeDir.RIGHTBOTBEHIND);
                Assert.AreEqual(edges.Count, 14);
                break;
            default:
                throw new System.IndexOutOfRangeException("Connection type "+ connectivityType + " does not exist.");
        }
        return edges;
    }

    public static EdgeDir xyzToDir(int x, int y, int z)
    {
        if (x == -1 && y ==  0 && z ==  0) return EdgeDir.LEFT;
        if (x ==  1 && y ==  0 && z ==  0) return EdgeDir.RIGHT;
        if (x ==  0 && y == -1 && z ==  0) return EdgeDir.BOTTOM;
        if (x ==  0 && y ==  1 && z ==  0) return EdgeDir.TOP;
        if (x ==  0 && y ==  0 && z == -1) return EdgeDir.BEHIND;
        if (x ==  0 && y ==  0 && z ==  1) return EdgeDir.FRONT;

        if (x == -1 && y ==  1 && z ==  1) return EdgeDir.LEFTTOPFRONT;
        if (x ==  1 && y ==  1 && z ==  1) return EdgeDir.RIGHTTOPFRONT;
        if (x == -1 && y == -1 && z ==  1) return EdgeDir.LEFTBOTFRONT;
        if (x ==  1 && y == -1 && z ==  1) return EdgeDir.RIGHTBOTFRONT;
        if (x == -1 && y ==  1 && z == -1) return EdgeDir.LEFTTOPBEHIND;
        if (x ==  1 && y ==  1 && z == -1) return EdgeDir.RIGHTTOPBEHIND;
        if (x == -1 && y == -1 && z == -1) return EdgeDir.LEFTBOTBEHIND;
        if (x ==  1 && y == -1 && z == -1) return EdgeDir.RIGHTBOTBEHIND;

        return EdgeDir.UNDEFINED;
    }

    private Triple getNeighbor(Triple start, Triple dir, Grid3D grid, ref bool success)
    {
        Assert.IsFalse(dir.x == 0 && dir.y == 0 && dir.z == 0);

        Triple current = start + dir;

        if (checkIdx(current.x, xdim) && checkIdx(current.y, ydim) && checkIdx(current.z, zdim))
        {
            if (grid[current.x, current.y, current.z] != -1)
            {
                success = true;
                return current;
            }
        }
        // there is no occupied field in that direction
        success = false;
        return new Triple(-1, -1, -1);
    }

    private void assignNeigbors(YaoNode node, int x, int y, int z)
    {
        Triple start = new Triple(x, y, z);

        for (int xDir = -1; xDir <= 1; xDir++)
        {
            for (int yDir = -1; yDir <= 1; yDir++)
            {
                for (int zDir = -1; zDir <= 1; zDir++)
                {
                    EdgeDir direction = xyzToDir(xDir, yDir, zDir);
                    if (edgeset.Contains(direction)) // does the edgeset of the current connection type contain this edge?
                    {
                        Triple dir = new Triple(xDir, yDir, zDir);
                        bool success = false;
                        Triple neighbor = getNeighbor(start, dir, data, ref success);
                        if (success)
                        {
                            Assert.IsTrue(checkIdx(neighbor.x, xdim) && checkIdx(neighbor.y, ydim) && checkIdx(neighbor.z, zdim));
                            int edgeIdx = edgeDirToIdx(direction);
                            Assert.IsTrue(edgeIdx >= 0 && edgeIdx < edgeset.Count);
                            node.setEdge(edgeIdx, data[neighbor.x, neighbor.y, neighbor.z]);
                            //Debug.Log("Neighbor found " + neighbor + " in dir " + direction);
                        }
                        else
                        {
                            //Debug.Log("Neighbor not found");
                        }

                    }
                }
            }
        }
    }

    private int edgeDirToIdx(EdgeDir dir)
    {
        Assert.IsTrue(edgeset.Contains(dir));
        return edgeset.IndexOf(dir);
    }

    private bool checkIdx(int idx, int limit)
    {
        return idx >= 0 && idx < limit;
    }

    #region iterating

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<YaoNode> GetEnumerator()
    {
        for (int x = 0; x < graph.Length; x++)
        {
            yield return graph[x];
        }
    }

    #endregion

    public YaoNode this[int nodeId]
    {
        get
        {
            // indexer
            Assert.IsTrue(nodeId < sample2Yao.Length);
            Assert.IsTrue(nodeId >= 0);
            int coord = sample2Yao[nodeId];
            Assert.IsTrue(coord < graph.Length);
            return graph[coord];
        }
        set
        {
            Assert.IsTrue(nodeId < sample2Yao.Length && nodeId >= 0);
            int coord = sample2Yao[nodeId]; // keep the internal ordering and referencing intact
            int[] neighbors = this[nodeId].outgoingEdges;
            value.outgoingEdges = neighbors;
            Assert.IsTrue(coord < graph.Length);
            graph[coord] = value;
            sample2Yao[nodeId] = coord;
        }

    }


    public void visualize()
    {
        foreach (YaoNode node in this)
        {
            Vector3 pos1 = data.samples[node.ID];
            int nNeighbors = 0;
            for (int i = 0; i < node.outgoingEdges.Length; i++)
            {
                int neighborId = node.outgoingEdges[i];
                if (neighborId != -1)
                {
                    
                    Vector3 pos2 = data.samples[neighborId];
                    drawLine(pos1, pos2, Color.blue, 0.05f);
                    nNeighbors++;
                }
            }
            //if(nNeighbors < 3)
            //    UnityEngine.Debug.Log("Found " + nNeighbors + " neighbors");
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
