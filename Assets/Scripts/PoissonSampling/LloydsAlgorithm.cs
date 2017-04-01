using UnityEngine;
using System.Collections.Generic;
using MIConvexHull;
using System.Linq;

public class LloydsAlgorithm {

    public class Vector3Vertex : IVertex
    {

        public double[] Position { get; set; }

        public Vector3Vertex(Vector3 v)
        {
            Position = new double[3];
            Position[0] = v.x;
            Position[1] = v.y;
            Position[2] = v.z;
        }

        public Vector3 toVector3()
        {
            return new Vector3((float)Position[0], (float)Position[1], (float)Position[2]);
        }

    }

    public class Tetrahedron : TriangulationCell<Vector3Vertex, Tetrahedron>
    {
        public Tetrahedron()
        {

        }

        /// <summary>
        /// Helper function to get the position of the i-th vertex.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>Position of the i-th vertex</returns>
        Vector3 GetPosition(int i)
        {
            return Vertices[i].toVector3();
        }
       
        /// <summary>
        /// This function adds indices for a triangle representing the face.
        /// The order is in the CCW (counter clock wise) order so that the automatically calculated normals point in the right direction.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="k"></param>
        /// <param name="center"></param>
        /// <param name="indices"></param>
        void MakeFace(int i, int j, int k, Vector3 center, List<int> indices)
        {
            var u = GetPosition(j) - GetPosition(i);
            var v = GetPosition(k) - GetPosition(j);

            // compute the normal and the plane corresponding to the side [i,j,k]
            var n = Vector3.Cross(u, v);
            var d = -Vector3.Dot(n, center);

            // check if the normal faces towards the center
            var t = Vector3.Dot(n, GetPosition(i)) + d;
            if (t >= 0)
            {
                // swapping indices j and k also changes the sign of the normal, because cross product is anti-commutative
                indices.Add(k); indices.Add(j); indices.Add(i);
            }
            else
            {
                // indices are in the correct order
                indices.Add(i); indices.Add(j); indices.Add(k);
            }
        }

        /// <summary>
        /// Creates a model of the tetrahedron. Transparency is applied to the color.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="radius"></param>
        /// <returns>A model representing the tetrahedron</returns>
        public Mesh CreateModel(Color color, double radius)
        {

            var points = new List<Vector3>(Enumerable.Range(0, 4).Select(i => GetPosition(i)));

            // center = Sum(p[i]) / 4
            var center = points.Aggregate(new Vector3(), (a, c) => a + (Vector3)c) / (float)points.Count;

            var normals = new List<Vector3>();
            var indices = new List<int>();
            MakeFace(0, 1, 2, center, indices);
            MakeFace(0, 1, 3, center, indices);
            MakeFace(0, 2, 3, center, indices);
            MakeFace(1, 2, 3, center, indices);
            Mesh geometry = new Mesh();
            geometry.vertices = points.ToArray();
            geometry.triangles = indices.ToArray();
            
            return geometry;
        }
    }

    private GameObject _voronoi;
    private GameObject _delaunay;

    // Use this for initialization
    public LloydsAlgorithm(Vector3[] vertices) {

        // Convert data representation
        List<Vector3Vertex> vertexList = new List<Vector3Vertex>();
        foreach (Vector3 v in vertices)
        {
            vertexList.Add(new Vector3Vertex(v));
        }

        // perform triangulation
        var voronoi = Triangulation.CreateVoronoi<Vector3Vertex, Tetrahedron>(vertexList);
        //var delaunay = Triangulation.CreateDelaunay(vertexList);


        GameObject.Destroy(_voronoi);
        _voronoi = new GameObject("Voronoi");
        //GameObject.Destroy(_delaunay);
        //_delaunay = new GameObject("Delaunay");

        //visualizeVertices(voronoi);
        //visualizeEdges(voronoi);
        visualizeCells(voronoi);

        //visualizeVertices(delaunay);
        //visualizeEdges(delaunay);
    }

    public void visualizeEdges(ITriangulation<Vector3Vertex, DefaultTriangulationCell<Vector3Vertex> > triangulation)
    {
        // visualize
        foreach (var cell in triangulation.Cells)
        {
            for(int i=0; i < cell.Adjacency.Length; i++)
            {
                var F = cell.Adjacency[i];
                int sharedBoundaries = 0;
                // vertices shared with F:
                for(int j=0; j < cell.Vertices.Length; j++)
                {
                    if(i != j)
                    {
                        sharedBoundaries++;
                        //cell.Vertices[j]
                    }
                    
                }
                Debug.Log("Shared vertices: " + sharedBoundaries);
                
            }


//                    drawLine(lastPos, vertex.toVector3(), ColorAssistant.getQualitativeColor(0));

        }
    }

    public void visualizeVertices(ITriangulation<Vector3Vertex,DefaultTriangulationCell<Vector3Vertex> > triangulation)
    {
        // visualize
        foreach (var cell in triangulation.Cells)
        {
            foreach (var vertex in cell.Vertices)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = vertex.toVector3();
                sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                sphere.transform.SetParent(_delaunay.transform);
                setColor(sphere, ColorAssistant.getQualitativeColor(0));
            }
        }
    }

    public void visualizeCells(VoronoiMesh<Vector3Vertex, Tetrahedron, VoronoiEdge<Vector3Vertex, Tetrahedron>> triangulation)
    {
        Tetrahedron cell = triangulation.Vertices.First<Tetrahedron>();
        //foreach (var cell in triangulation.Vertices)
        //{
            Mesh faces = cell.CreateModel(ColorAssistant.getDivergingColor(0), 1.0f);
            faces.RecalculateNormals();
            faces.RecalculateBounds();
            GameObject o = new GameObject("cell");
            o.AddComponent<MeshFilter>();
            o.GetComponent<MeshFilter>().mesh = faces;
            o.AddComponent<MeshRenderer>();
        //}
    }

    public void visualizeVertices(VoronoiMesh<Vector3Vertex, Tetrahedron, VoronoiEdge<Vector3Vertex, Tetrahedron>> triangulation)
    {
        // visualize
        foreach (var cell in triangulation.Vertices)
        {
            foreach (var vertex in cell.Vertices)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = vertex.toVector3();
                sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                sphere.transform.SetParent(_voronoi.transform);
                setColor(sphere, ColorAssistant.getQualitativeColor(1));
            }
        }
    }

    public void visualizeEdges(VoronoiMesh<Vector3Vertex, Tetrahedron, VoronoiEdge<Vector3Vertex, Tetrahedron>> triangulation)
    {
        // visualize
        foreach (var edge in triangulation.Edges)
        {
            var sourceCell = edge.Source;

            int c = 0; Vector3 lastPos = new Vector3(0,0,0);

            foreach (var vertex in sourceCell.Vertices)
            {
                if(c > 0) {
                    drawLine(lastPos, vertex.toVector3(), ColorAssistant.getQualitativeColor(1));

                        }
                lastPos = vertex.toVector3();
                c++;
            }
        }

        // both contain exactly 4 vertices
        foreach (var edge in triangulation.Edges)
        {
            var sourceCell = edge.Source;
            var targetCell = edge.Target;


            for (int i=0; i < sourceCell.Vertices.Length; i++)
            {
                    drawLine(sourceCell.Vertices[i].toVector3(), targetCell.Vertices[i].toVector3(), ColorAssistant.getQualitativeColor(2));
            }
        }

        // match via adjacency
        foreach (var cell in triangulation.Vertices)
        {
            for (int i = 0; i < cell.Adjacency.Length; i++)
            {
                var F = cell.Adjacency[i];
                int sharedBoundaries = 0;
                // vertices shared with F:
                for (int j = 0; j < cell.Vertices.Length; j++)
                {
                    if (i != j)
                    {
                        sharedBoundaries++;
                        //cell.Vertices[j]
                    }

                }
                Debug.Log("Shared vertices: " + sharedBoundaries);

            }
        }

    }

    private void drawLine(Vector3 start, Vector3 end, Color c, float width = 0.03f, string name = "grid")
    {
        GameObject myLine = new GameObject(name);
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        myLine.GetComponent<Renderer>().material.color = c;
        lr.material = new Material(Shader.Find("Particles/Alpha Blended"));
        lr.SetColors(c, c);
        lr.material.color = c;
        lr.SetWidth(width, width);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        myLine.transform.SetParent(_voronoi.transform);
    }

    /**
* Sets a color and renderer, depending on whether the color contains an alpha channel or not
**/
    private void setColor(GameObject o, Color c)
    {
        Material materialColored;
        if (c.a < 1)
        {
            materialColored = new Material(Shader.Find("Legacy Shaders/Transparent/VertexLit"));
        }
        else
        {
            materialColored = new Material(Shader.Find("Standard"));
        }
        materialColored.color = c;
        o.GetComponent<Renderer>().material = materialColored;
    }

}
