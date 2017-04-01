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

        public Vector3 calculateCircumsphere()
        {
            // http://mathworld.wolfram.com/Circumsphere.html
            Vector3 center = new Vector3();
            center.x = Dx() / (2* a());
            center.y = Dy() / (2 * a());
            center.z = Dz() / (2 * a());

            if(Mathf.Abs(Dx()) < 0.001f || Mathf.Abs(Dy()) < 0.001f || Mathf.Abs(Dz()) < 0.001f)
            {
                Debug.Log("Determinant zero");
                return new Vector3();
            }
            return center;
        }

        private float a()
        {
            Matrix4x4 a = new Matrix4x4();
            for(int row = 0; row < 4; row++)
            {
                a[row, 0] = GetPosition(row).x;
                a[row, 1] = GetPosition(row).y;
                a[row, 2] = GetPosition(row).z;
                a[row, 3] = 1.0f;
            }
            return a.determinant;
        }

        private float Dx()
        {
            Matrix4x4 D = new Matrix4x4();
            for (int row = 0; row < 4; row++)
            {
                D[row, 0] = GetPosition(row).x* GetPosition(row).x+ GetPosition(row).y* GetPosition(row).y+ GetPosition(row).z* GetPosition(row).z;
                D[row, 1] = GetPosition(row).y;
                D[row, 2] = GetPosition(row).z;
                D[row, 3] = 1.0f;
            }
            return D.determinant;
        }

        private float Dy()
        {
            Matrix4x4 D = new Matrix4x4();
            for (int row = 0; row < 4; row++)
            {
                D[row, 0] = GetPosition(row).x * GetPosition(row).x + GetPosition(row).y * GetPosition(row).y + GetPosition(row).z * GetPosition(row).z;
                D[row, 1] = GetPosition(row).x;
                D[row, 2] = GetPosition(row).z;
                D[row, 3] = 1.0f;
            }
            return -D.determinant;
        }

        private float Dz()
        {
            Matrix4x4 D = new Matrix4x4();
            for (int row = 0; row < 4; row++)
            {
                D[row, 0] = GetPosition(row).x * GetPosition(row).x + GetPosition(row).y * GetPosition(row).y + GetPosition(row).z * GetPosition(row).z;
                D[row, 1] = GetPosition(row).x;
                D[row, 2] = GetPosition(row).y;
                D[row, 3] = 1.0f;
            }
            return D.determinant;
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
        visualizeEdges(voronoi);
        //visualizeCells(voronoi);

        //visualizeVertices(delaunay);
        //visualizeEdges(delaunay);
    }

    public void visualizeEdges(ITriangulation<Vector3Vertex, DefaultTriangulationCell<Vector3Vertex> > triangulation)
    {
        /*  
            
            foreach (var edge in voronoiMesh.Edges)
            {
                calculateCircumsphere
                var from = edge.Source.Circumcenter;
                var to = edge.Target.Circumcenter;
                drawingCanvas.Children.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Black });
            }

            foreach (var cell in voronoiMesh.Vertices)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (cell.Adjacency[i] == null)
                    {
                        var from = cell.Circumcenter;
                        var t = cell.Vertices.Where((_, j) => j != i).ToArray();
                        var factor = 100 * IsLeft(t[0].ToPoint(), t[1].ToPoint(), from) * IsLeft(t[0].ToPoint(), t[1].ToPoint(), Center(cell));
                        var dir = new Point(0.5 * (t[0].Position[0] + t[1].Position[0]), 0.5 * (t[0].Position[1] + t[1].Position[1])) - from;
                        var to = from + factor * dir;
                        drawingCanvas.Children.Add(new Line { X1 = from.X, Y1 = from.Y, X2 = to.X, Y2 = to.Y, Stroke = Brushes.Black });
                    }                    
                }
            }

            ShowVertices(Vertices);
            drawingCanvas.Children.Add(new Rectangle { Width = drawingCanvas.ActualWidth, Height = drawingCanvas.ActualHeight, Stroke = Brushes.Black, StrokeThickness = 3 });
        }
        */



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
            Vector3 from = edge.Source.calculateCircumsphere();
            Vector3 to = edge.Target.calculateCircumsphere();

            drawLine(from, to, ColorAssistant.getDivergingColor(0));
        }

        /*
        int cellIdx = 0;
        foreach(var cell in triangulation.Vertices)
        {
            foreach(var neighbor in cell.Adjacency)
            {
                if (neighbor == null) continue;
                Vector3 from = cell.calculateCircumsphere();
                Vector3 to = neighbor.calculateCircumsphere();

                drawLine(from, to, ColorAssistant.getQualitativeColor(cellIdx));
            }
            cellIdx++;
        }
        */

        /*
        foreach (var cell in triangulation.Vertices)
        {
            for (int i = 0; i < 4; i++)
            {
                if (cell.Adjacency[i] == null)
                {
                    Vector3 from = cell.calculateCircumsphere();
                    var t = cell.Vertices.Where((_, j) => j != i).ToArray();
                    float factor = 100.0f ;
                    Vector3 dir = new Vector3((float)(0.5f * (t[0].Position[0] + t[1].Position[0])), (float)(0.5f * (t[0].Position[1] + t[1].Position[1])), (float)(0.5f * (t[0].Position[2] + t[1].Position[2]))) - from;
                    Vector3 to = from + factor * dir;
                    drawLine(from, to, ColorAssistant.getDivergingColor(1));
                }
            }
        }
        */

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
