using UnityEngine;
using System.Collections.Generic;

public class EndPointSelection {

    public static void select(YaoGraph paths, TreeIterationParam param)
    {
        List<YaoNode> candidates = new List<YaoNode>();

        for (int i = 0; i < paths.data.samples.Length; i++)
        {
            switch (param.endpointMethod)
            {
                case TreeIterationParam.PlacementType.VOLUME:
                    Ellipsoid e = param.volume.GetComponent<Ellipsoid>();
                    if(e!=null && e.testPoint(paths.data.samples[i]))
                    {
                        candidates.Add(paths[paths.sample2Yao[i]]);
                    }
                    if (e == null && param.volume.GetComponent<Collider>().bounds.Contains(paths.data.samples[i]))
                    {
                        candidates.Add(paths[paths.sample2Yao[i]]);
                    }
                    break;
                case TreeIterationParam.PlacementType.HOPS:
                    if (paths.graph[i].hopsFromSource == param.nHops)
                    {
                        candidates.Add(paths[paths.sample2Yao[i]]);
                    }
                    break;
            }
        }

            selectFromCandidates(paths, candidates, param);

    }

    private static void selectFromCandidates(YaoGraph paths, List<YaoNode> candidates, TreeIterationParam param)
    {
        int nBranches = param.nBranches;
        int iteration = param.iterationCounter;

        if (candidates.Count < nBranches)
        {
            throw new System.Exception("Not enough branches available with the number of hops from source requested.");
        }

        int selected = 0;
        while (selected < nBranches)
        {
            int r = Random.Range(0, candidates.Count);
            markAsStem(candidates[r], paths, iteration);
            candidates.RemoveAt(r);
            selected++;
        }
    }

    private static void markAsStem(YaoNode n, YaoGraph g, int iteration)
    {
        n.isStem = iteration;
        if (n.parentID != -1 && g[n.parentID].isStem==-1) // mark parent as stem as well, if it is nor already
        {
            markAsStem(g[n.parentID], g, iteration);
        }
    }

    public static void visualize(YaoGraph paths)
    {
        foreach (YaoNode node in paths)
        {
            if (node.isStem!=-1 && node.parentID != -1) // draw only stem nodes
            {
                
                Vector3 pos1 = paths.data.samples[node.ID];
                Vector3 pos2 = paths.data.samples[node.parentID];
                drawLine(pos1, pos2, Color.red, 0.2f/Mathf.Pow((node.isStem), 1));

                // Color iterations differently
                //if (node.isStem == 1) drawLine(pos1, pos2, Color.red);
                //if (node.isStem == 2) drawLine(pos1, pos2, Color.blue);
                //if (node.isStem == 3) drawLine(pos1, pos2, Color.green);
            }
        }

    }

    private static void drawLine(Vector3 start, Vector3 end, Color c, float width = 0.03f, string name = "line")
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
