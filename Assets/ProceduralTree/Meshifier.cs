using UnityEngine;
using System.Collections;

public class Meshifier {

    public YaoGraph tree;
    public int nElements;

    public Mesh generatedMesh;

        public Meshifier(YaoGraph t, int n)
    {
        tree = t;
        nElements = n;
        generateMesh();
    }

    private void generateMesh()
    {
        generatedMesh = new Mesh();

        int nVertices = tree.getNodeCount()* 2 * nElements;

        Vector3[] vertices  = new Vector3[nVertices];
        int[]     triangles = new int[6];
        Vector3[] normals = new Vector3[nVertices];
        Vector2[] uv = new Vector2[nVertices];

        //TODO generateBranchNode for each pair of nodes connected by a branch
        // TODO fill points


        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.normals = normals;
        generatedMesh.uv = uv;
    }

    private void generateBranchNode(YaoNode n1, YaoNode n2)
    {
        float thickness_radius = n1.isStem;
        // TODO do circular angle iterations
    }
}
