using UnityEngine;
using System.Collections;

public class SplineInterpolation : MonoBehaviour {

    public Vector4[] keyballs;
    public int interpolationDensity = 200; // todo make this depend on length of the trajectory
    public int nCirclePolypoints = 10;
    private Vector4[] interpolation, dinterpolation, ddinterpolation;

    // Use this for initialization
    void Start () {
        // Create the data to be fitted

        int nPoints = 2;
        keyballs = new Vector4[nPoints];
        for(int i=0; i < nPoints; i++)
        {
            //keyballs[i] = new Vector4(Random.Range(0.0f, 5.0f), Random.Range(0.0f, 5.0f), Random.Range(0.0f, 5.0f), Random.Range(0.1f, 0.5f));
            keyballs[i] = new Vector4(i,i,i, 0.4f);
        }

        CubicSpline.FitParametric(keyballs, interpolationDensity, out interpolation, out dinterpolation, out ddinterpolation);

        //visualizeKeypoints(keyballs);
        //visualizeCenterLine();
        //visualizeHull();
        meshify();
        //testSphereCap();
    }

    private void meshify()
    {
        Mesh m = new Mesh();
        Vector3[] vertices = new Vector3[interpolation.Length* nCirclePolypoints + 2 * getSphericalCapSize()];
        Vector2[] uvs = new Vector2[interpolation.Length * nCirclePolypoints + 2 * getSphericalCapSize()];
        int[] triangles = new int[3*nCirclePolypoints*2*(interpolation.Length-1) + 2 * getSphericalCapSize()*9];

        Vector3[] previousSegments;
        int vertexCounter = 0;
        int triangleIdx = 0;
        Vector3 lastBasis = Vector3.right;
        Vector3 firstBasis = Vector3.right;
        for (int i = 0; i < interpolation.Length; i++)
        {
            //Debug.Log("Circle " + (i + 1) + "/"+ interpolation.Length);
            Vector4 c = interpolation[i];
            Vector4 dc = dinterpolation[i];
            

            Vector3[] segments = calculateCircle(new Vector3(c.x, c.y, c.z), new Vector3(dc.x, dc.y, dc.z), c.w, ref lastBasis, nCirclePolypoints);
            if (i == 0) firstBasis = lastBasis; // save the first basis vector in order to determine a good cap

            for (int j=0; j < segments.Length; j++)
            {
                addMarker(segments[j], j);
                vertices[vertexCounter] = segments[j];
                uvs[vertexCounter] = new Vector2(j%2, i%2);
                vertexCounter++;
            }
            if(i != 0)
            {
                // connect previousSegments and segments
                int firstPrevious = vertexCounter - segments.Length * 2;
                int first = vertexCounter - segments.Length;
                for(int j=0; j < segments.Length; j++) {
                    triangles[triangleIdx] = firstPrevious + j % nCirclePolypoints;
                    triangles[triangleIdx+1] = first + j % nCirclePolypoints;
                    triangles[triangleIdx + 2] = first + (1 + j) % nCirclePolypoints;
                    triangleIdx += 3;
                    triangles[triangleIdx] = firstPrevious + j % nCirclePolypoints;
                    triangles[triangleIdx + 1] = first + (1 + j) % nCirclePolypoints;
                    triangles[triangleIdx + 2] = firstPrevious + (1 + j) % nCirclePolypoints;
                    triangleIdx += 3;
                }

            }
            previousSegments = segments;

        }
        m.vertices = vertices;
        m.triangles = triangles;
        m.uv = uvs;

        Vector4 start_c = interpolation[0];
        Vector4 start_dc = dinterpolation[0];
        Vector4 end_c = interpolation[interpolation.Length-1];
        Vector4 end_dc = dinterpolation[dinterpolation.Length-1];
        Debug.Log("Adding front cap");
        // inverse normal, as it points towardfs the direction of the spline and we wanna go backwards.
        addSphericalCap(ref m, new Vector3(start_c.x, start_c.y, start_c.z), -new Vector3(start_dc.x, start_dc.y, start_dc.z), start_c.w, firstBasis, ref triangleIdx, ref vertexCounter, 0);
        Debug.Log("Adding back cap");
        addSphericalCap(ref m, new Vector3(end_c.x, end_c.y, end_c.z), new Vector3(end_dc.x, end_dc.y, end_dc.z), end_c.w, lastBasis, ref triangleIdx, ref vertexCounter, vertexCounter - getSphericalCapSize());

        GetComponent<MeshFilter>().mesh = m;

        
        m.name = "Ball-BSpline";
        m.RecalculateNormals();
        m.RecalculateBounds();
    }

    private void visualizeCenterLine()
    {
        LineRenderer lr = initLine(Color.green, 0.1f, "BSpline");
        for (int i = 0; i < interpolation.Length; i++)
        {
            extendLine(new Vector3(interpolation[i].x, interpolation[i].y, interpolation[i].z), lr, i);
        }
    }

    private void visualizeKeypoints(Vector4[] points)
    {
        foreach (Vector4 ball in points)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(ball.w * 2, ball.w * 2, ball.w * 2); // radius to diameter
            sphere.transform.position = new Vector3(ball.x, ball.y, ball.z);
            sphere.name = "Keypoint";
            Color col = Color.red;
            setColor(sphere, col);
            sphere.transform.parent = gameObject.transform;
        }
    }

    private void visualizeHull()
    {
        for (int i = 0; i < interpolation.Length; i++)
        {

            Vector4 c = interpolation[i];
            Vector4 dc = dinterpolation[i];
            Vector3 lastBasis = Vector3.right;

            Vector3[] segments = calculateCircle(new Vector3(c.x, c.y, c.z), new Vector3(dc.x, dc.y, dc.z), c.w, ref lastBasis, nCirclePolypoints);

            LineRenderer lr = initLine(Color.blue, 0.03f, "SurfaceRing");
            for (int j = 0; j < segments.Length; j++)
            {
                extendLine(segments[j], lr, j);
            }
            extendLine(segments[0], lr, segments.Length); // close the circle
            
        }
    
}
	
	// Update is called once per frame
	void Update () {
                
	}


    private LineRenderer initLine(Color c, float width = 0.03f, string name = "line")
    {
        GameObject myLine = new GameObject(name);
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        myLine.GetComponent<Renderer>().material.color = c;
        lr.material = new Material(Shader.Find("Standard"));
        lr.material.color = c;
        lr.SetWidth(width, width);

        myLine.transform.parent = gameObject.transform;
        return lr;
    }

    private void extendLine(Vector3 pos, LineRenderer lr, int count)
    {
        lr.SetVertexCount(count + 1);
        lr.SetPosition(count, pos);
    }

    private void setColor(GameObject o, Color c)
    {
        Material materialColored;
        if (c.a < 1)
        {
            materialColored = new Material(Shader.Find("Legacy Shaders/Transparent/VertexLit"));
        }
        else
        {
            materialColored = new Material(Shader.Find("Unlit/Color"));
        }
        materialColored.color = c;
        o.GetComponent<Renderer>().material = materialColored;
    }

    private Vector3[] calculateCircle(Vector3 center, Vector3 normal, float radius, ref Vector3 lastBasis, int nSegments)
    {
        int currentSegment = 0;
        Vector3[] segments = new Vector3[nSegments];
        for(int i=0; i < nSegments; i++) {
            float theta = (Mathf.PI * 2.0f / nSegments) * i;
        //for (float theta = 0.0f; theta < 2 * Mathf.PI; theta += Mathf.PI * 2.0f / nSegments)
        //{
            segments[i] = calculatePointOnCircle(center, normal.normalized, radius, theta, ref lastBasis);
            currentSegment++;
        }
        return segments;
    }

    private Vector3 calculatePointOnCircle(Vector3 center, Vector3 normal, float radius, float theta, ref Vector3 lastBasis)
    {
        // Find two basis vectors. Theoretically any that are orthogonal to the normal vector should do.
        // However, they must not "jump" between circles. Otherwise the mesh will get torn up.
        Vector3 basis1, basis2;
        
            basis1 = Vector3.Cross(normal, Vector3.Cross(lastBasis, normal));
        if(basis1.magnitude < 0.0001) // normal and lastBasis are colinear => try another one
        {
            basis1 = Vector3.Cross(Vector3.up, normal);
            Debug.Log("Fallback 1");
        }
        if (basis1.magnitude < 0.0001) // normal and lastBasis are still colinear => orthogonal one has to work
        {
            basis1 = Vector3.Cross(Vector3.right, normal);
            Debug.Log("Fallback 2");
        }
        basis1.Normalize();

        basis2 = Vector3.Cross(basis1, normal).normalized;

        lastBasis = basis1;
        return center + radius * (basis1 * Mathf.Cos(theta) + basis2 * Mathf.Sin(theta));
    }

    private void testSphereCap()
    {
        Vector3[] p  = calculateSphereCap(new Vector3(0, 0, 0), Vector3.forward, 1.0f);
        int i = 0;
        foreach(Vector3 point in p)
        {
            addMarker(point, i);
            i++;
        }
    }

    private void addMarker(Vector3 point, int idx)
    {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            sphere.name = "Cap";
            Color col = Color.green;
            setColor(sphere, col);
            sphere.transform.parent = gameObject.transform;

            GameObject text = new GameObject();
            text.transform.position = point;
            text.transform.parent = sphere.transform;
            TextMesh textMeshComponent = text.AddComponent<TextMesh>();
            //MeshRenderer meshRendererComponent = text.AddComponent<MeshRenderer>();

            // Create an array of materials for the MeshRenderer component (it works according to the inspector)
            //meshRendererComponent.materials = new Material[1];

            // Set the text string of the TextMesh component (it works according to the inspector)
            textMeshComponent.text = "" + idx;
            textMeshComponent.characterSize = 0.02f;
            textMeshComponent.fontSize = 20;
    }

    private void addSphericalCap(ref Mesh m, Vector3 center, Vector3 normal, float radius, Vector3 basis, ref int triangleIdx, ref int VertexIdx, int connectToIdx)
    {
        Debug.Log("starting Cap at: "+connectToIdx+", existing vertices: " + VertexIdx);

        Vector3[] points = m.vertices;
        Vector2[] uvs = m.uv;
        int[] triangles = m.triangles;
        
        for (int j = VertexIdx+1; j < points.Length; j++) points[j] = new Vector3(0, 0, 0);


        normal.Normalize(); normal = -normal;
        Vector3 basis2 = Vector3.Cross(normal, basis);
        Vector3 basis1 = Vector3.Cross(normal, basis2);

        bool tipPlaced = false; // the very tip of the spherical cap is just one point, not a whole circle.

        int i = 0;

        // forwards cap
        //for (int angle1 = nCirclePolypoints / 2; angle1 >= 0; angle1--)
        // backwards cap
        for (int angle1 = 0; angle1 <= nCirclePolypoints / 2; angle1++)
            {
            bool rowSet = false;
            for (int angle2 = 0; angle2 < nCirclePolypoints; angle2++)
            {
                float theta = Mathf.PI + 2 * Mathf.PI / nCirclePolypoints * angle2;
                float omega = 2 * Mathf.PI / nCirclePolypoints * angle1;
                
                if (Mathf.Cos(omega) <= 0)
                {
                    //Debug.Log("T:" + theta * 180 / Mathf.PI + ", O:" + omega * 180 / Mathf.PI);
                    // Place the tip point
                    if (!tipPlaced && (angle1 == nCirclePolypoints / 2)) // || angle1 == (nCirclePolypoints / 2) - 1
                    {
                        tipPlaced = true;
                        points[VertexIdx + i] = calculatePointOnSphere(center, normal, radius, theta, omega, basis1, basis2);

                        for (int k=0; k < nCirclePolypoints; k++) {
                            
                        triangles[triangleIdx + 2] = connectToIdx + i + k;
                        triangles[triangleIdx + 1] = VertexIdx + i;
                            if (k == nCirclePolypoints - 1)
                            {
                                triangles[triangleIdx + 0] = connectToIdx + i + 1 + k- nCirclePolypoints;
                            }
                            else
                            {
                                triangles[triangleIdx + 0] = connectToIdx + i + 1 + k;
                            }

                            triangleIdx += 3;
                        }

                        addMarker(points[VertexIdx + i], i);


                        uvs[VertexIdx + i] = new Vector2(angle2%2, angle1%2);
                        i++;
                    }
                    // all other sphere points
                    if (angle1 != nCirclePolypoints / 2)
                    {
                        // generate the new point
                        points[VertexIdx + i] = calculatePointOnSphere(center, normal, radius, theta, omega, basis1, basis2);

                        addMarker(points[VertexIdx + i], i);

                        uvs[VertexIdx + i] = new Vector2(angle2 % 2, angle1 % 2);
                        // connect points
                        // first non-cap point
                        //TODO will always connect to the end, not to the start
                        // points[i] points[getSphericalCapSize() + i] points[getSphericalCapSize()+ i + 1]
                        //Debug.Log("Tri: " + (connectToIdx + i) + ", " + (VertexIdx + i) + ", " + (connectToIdx + i + 1));
                        triangles[triangleIdx + 2] = connectToIdx + i;
                        triangles[triangleIdx + 1] = VertexIdx + i;
                        triangles[triangleIdx + 0] = connectToIdx + i + 1;
                        triangleIdx += 3;

                        // points[i-1] points[getSphericalCapSize() + i] points[i]
                        if (i > 0 )
                        {
                            //Debug.Log("Tri: " + (VertexIdx + i) + ", " + (connectToIdx + i) + ", " + (VertexIdx + i - 1));
                            triangles[triangleIdx + 2] = VertexIdx + i;
                            triangles[triangleIdx + 1] = connectToIdx + i;
                            triangles[triangleIdx + 0] = VertexIdx + i - 1;
                            triangleIdx += 3;
                        }

                        //}
                        if(angle2 ==nCirclePolypoints-1)
                            rowSet = true;

                        i++;
                    }

                }

            }
            if (rowSet)
            {
                

                connectToIdx = VertexIdx - nCirclePolypoints;
                Debug.Log("Jump to: " + (connectToIdx+i) + " vertices: "+ (VertexIdx+i));
            }
        }
        
        m.vertices = points;
        m.triangles = triangles;
        m.uv = uvs;

        Debug.Log("i was: " + i + " of cap size: " + getSphericalCapSize());
        VertexIdx += i+1; // index of next element to insert

    }

    private int getSphericalCapSize()
    {

        if ((nCirclePolypoints / 2) % 2 == 0)
        {
            return (nCirclePolypoints / 2) * (nCirclePolypoints / 2) - (nCirclePolypoints / 2 - 1) - (nCirclePolypoints / 2);
        }
        else
        {
            return (nCirclePolypoints ) * (nCirclePolypoints / 2) - (nCirclePolypoints / 2 - 1);
        }

        
    }

    private Vector3[] calculateSphereCap(Vector3 center, Vector3 normal, float radius)
    {
        Vector3[] points = new Vector3[getSphericalCapSize()];
        for (int j = 0; j < points.Length; j++) points[j] = new Vector3(0, 0, 0);
        Vector3 basis1 = Vector3.up;
        Vector3 basis2 = Vector3.right;
        normal = Vector3.forward;
        bool tipPlaced = false; // the very tip of the spherical cap is just one point, not a whole circle.

        int i = 0;
        
            for (int a2 = nCirclePolypoints / 2; a2 >=0; a2--)
            {
            for (int a1 = 0; a1 < nCirclePolypoints; a1++)
            {
                float theta = 2 * Mathf.PI / nCirclePolypoints * a1;
                float omega = 2 * Mathf.PI / nCirclePolypoints * a2;
                if(Mathf.Cos(omega)>= 0) {
                    // Place the tip point
                    if (!tipPlaced && (a2 == 0 || a2 == (nCirclePolypoints/2)-1))
                    {
                        tipPlaced = true;
                        points[i] = calculatePointOnSphere(center, normal, radius, theta, omega, basis1, basis2);
                        i++;
                    }
                    // all other sphere points
                    if (a2 != 0) {
                        points[i] = calculatePointOnSphere(center, normal, radius, theta, omega, basis1, basis2);
                        i++;
                    }
                    
                }
                
            }
        }
        Debug.Log(i);
        return points;
    }

    private Vector3 calculatePointOnSphere(Vector3 center, Vector3 normal, float radius, float theta, float omega, Vector3 basis1, Vector3 basis2)
    {
        return center + radius * (basis1 * Mathf.Cos(theta) * Mathf.Sin(omega) + basis2 * Mathf.Sin(theta) * Mathf.Sin(omega) + normal * Mathf.Cos(omega) );
    }

}
