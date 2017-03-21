using UnityEngine;

public class SplineInterpolation : MonoBehaviour {

    
    public int interpolationDensity = 200; // todo make this depend on length of the trajectory
    [Range(5, 20)]
    public int nCirclePolypoints = 10;
    [Range(0.0f, 5.0f)]
    public float CapPointiness = 1.0f;

    private Vector4[] keyballs;
    private Vector4[] interpolation, dinterpolation, ddinterpolation;

    // Use this for initialization
    void Start () {
        // Create the data to be fitted

        //initKeyballsRandom(3);
        //initKeyballsLinear(2);
        //initKeyballsDemo();
        initKeyballsSnake(5, 2);

        CubicSpline.FitParametric(keyballs, interpolationDensity, out interpolation, out dinterpolation, out ddinterpolation);

        visualizeKeypoints(keyballs);
        visualizeCenterLine();
        visualizeHull();
        //meshify();
        //testSphereCap();
    }

    private void initKeyballsRandom(int n)
    {
        keyballs = new Vector4[n];
        for (int i = 0; i < n; i++)
        {
            keyballs[i] = new Vector4(Random.Range(0.0f, 5.0f), Random.Range(0.0f, 5.0f), Random.Range(0.0f, 5.0f), Random.Range(0.1f, 0.7f));
        }
    }

    private void initKeyballsLinear(int n)
    {
        keyballs = new Vector4[n];
        for (int i = 0; i < n; i++)
        {
            keyballs[i] = new Vector4(i,i,i, Random.Range(0.1f, 0.7f));
        }
    }

    private void initKeyballsDemo()
    {
        keyballs = new Vector4[3];
        keyballs[0] = new Vector4(0, 0, 0, 0.1f);
        keyballs[1] = new Vector4(1, 0, 0, 0.3f);
        keyballs[2] = new Vector4(1, 1, 0, 0.2f);
    }

    private void initKeyballsSnake(int n, int prey)
    {
        keyballs = new Vector4[n];
        for (int i = 0; i < n; i++)
        {
            if(i== prey)
            {
                keyballs[i] = new Vector4(Mathf.Cos(i * Mathf.PI), i * 3, 0.8f, 0.8f);
            }
            else
            {
                keyballs[i] = new Vector4(Mathf.Cos(i * Mathf.PI), i * 3, 0, 0.5f);
            }
            
        }
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
                //addMarker(segments[j], vertexCounter, Color.green);
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
                    addTriangle(ref triangles, ref triangleIdx, firstPrevious + j % nCirclePolypoints, first + j % nCirclePolypoints, first + (1 + j) % nCirclePolypoints);
                    addTriangle(ref triangles, ref triangleIdx, firstPrevious + j % nCirclePolypoints, first + (1 + j) % nCirclePolypoints, firstPrevious + (1 + j) % nCirclePolypoints);
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
        addSphericalCap(ref m, new Vector3(start_c.x, start_c.y, start_c.z), new Vector3(start_dc.x, start_dc.y, start_dc.z), -1, start_c.w, firstBasis, ref triangleIdx, ref vertexCounter, 0);
        Debug.Log("Adding back cap");                                                                                                                                                  // the end of the tube points        - one ring
        addSphericalCap(ref m, new Vector3(end_c.x, end_c.y, end_c.z), new Vector3(end_dc.x, end_dc.y, end_dc.z), 1, end_c.w, lastBasis, ref triangleIdx, ref vertexCounter, vertexCounter - getSphericalCapSize()-nCirclePolypoints);

        GetComponent<MeshFilter>().mesh = m;

        
        m.name = "Ball-BSpline";
        m.RecalculateNormals();
        m.RecalculateBounds();
    }

    private void visualizeCenterLine()
    {
        LineRenderer lr = initLine(ColorAssistant.getQualitativeColor(2), 0.1f, "BSpline");
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
            Color col = ColorAssistant.getQualitativeColor(0);
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

            LineRenderer lr = initLine(ColorAssistant.getQualitativeColor(1), 0.03f, "SurfaceRing");
            for (int j = 0; j < segments.Length; j++)
            {
                extendLine(segments[j], lr, j);
            }
            extendLine(segments[0], lr, segments.Length); // close the circle
            
        }
    
}
	
	// Update is called once per frame
	void Update () {
        /*
        initKeyballsSnake(5);

        CubicSpline.FitParametric(keyballs, interpolationDensity, out interpolation, out dinterpolation, out ddinterpolation);

        visualizeKeypoints(keyballs);
        visualizeCenterLine();
        visualizeHull();
        */
    }


    private LineRenderer initLine(Color c, float width = 0.03f, string name = "line")
    {
        GameObject myLine = new GameObject(name);
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        myLine.GetComponent<Renderer>().material.color = c;
        lr.material = new Material(Shader.Find("Legacy Shaders/Bumped Specular"));
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
            materialColored = new Material(Shader.Find("Legacy Shaders/Bumped Specular"));
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

    private void addMarker(Vector3 point, int idx, Color col)
    {
        addMarker(point, ""+idx, col);
    }

    private void addMarker(Vector3 point, string label, Color col)
    {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = point;
            sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            sphere.name = "Cap "+label;
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
            textMeshComponent.text = label;
            textMeshComponent.characterSize = 0.02f;
            textMeshComponent.fontSize = 20;
    }

    private void addSphericalCap(ref Mesh m, Vector3 center, Vector3 normal, int spin, float radius, Vector3 basis, ref int triangleIdx, ref int VertexIdx, int connectToIdx)
    {
        //Debug.Log("starting Cap at: "+connectToIdx+", existing vertices: " + VertexIdx);

        Vector3[] points = m.vertices;
        Vector2[] uvs = m.uv;
        int[] triangles = m.triangles;
        
        for (int j = VertexIdx+1; j < points.Length; j++) points[j] = new Vector3(0, 0, 0);


        normal.Normalize();
        Vector3 basis2 = Vector3.Cross(normal, basis);
        Vector3 basis1 = Vector3.Cross(normal, basis2);

        bool tipPlaced = false; // the very tip of the spherical cap is just one point, not a whole circle.

        int i = 0;

        // forwards cap
        for (int angle1 = nCirclePolypoints / 2; angle1 >= 0; angle1--)
            {
            bool rowSet = false;
            for (int angle2 = 0; angle2 < nCirclePolypoints; angle2++)
            {
                float theta = Mathf.PI + 2 * Mathf.PI / nCirclePolypoints * angle2; // runs [0 2Pi]
                float omega = 2 * Mathf.PI / nCirclePolypoints * angle1; // runs [0 Pi]

                int thetaDegree = (int)((theta * 180 / Mathf.PI) % 360);
                int omegaDegree = (int)((omega * 180 / Mathf.PI) % 360);

                if (Mathf.Cos(omega) <= 0)
                {
                    continue;
                }
                //Debug.Log("T:" + theta * 180 / Mathf.PI + ", O:" + omega * 180 / Mathf.PI);
                // Place the tip point
                if (!tipPlaced && (angle1 == nCirclePolypoints / 2 || angle1 == 0))
                {
                        tipPlaced = true;
                        points[VertexIdx + i] = calculatePointOnSphere(center, normal, spin, radius, theta, omega, basis1, basis2);

                        for (int k=0; k < nCirclePolypoints; k++) {
                            
                            if (k == nCirclePolypoints - 1)
                            {
                                addTriangle(ref triangles, ref triangleIdx, connectToIdx + i + k, VertexIdx + i, connectToIdx + i + 1 + k - nCirclePolypoints, spin);
                            }
                            else
                            {
                                addTriangle(ref triangles, ref triangleIdx, connectToIdx + i + k, VertexIdx + i, connectToIdx + i + 1 + k, spin);
                            }

                        }

                        //addMarker(points[VertexIdx + i], i+"(" + thetaDegree + "," + omegaDegree + ")", ColorAssistant.getQualitativeColor(angle1));

                        uvs[VertexIdx + i] = new Vector2(angle2%2, angle1%2);
                        i++;
                    }
                    // all other sphere points
                    if (angle1 != nCirclePolypoints / 2 && angle1 != 0)
                    {
                        // generate the new point
                        points[VertexIdx + i] = calculatePointOnSphere(center, normal, spin, radius, theta, omega, basis1, basis2);

                        //addMarker(points[VertexIdx+i], i+"(" + thetaDegree + "," + omegaDegree + ")", ColorAssistant.getQualitativeColor(angle1));

                        uvs[VertexIdx + i] = new Vector2(angle2 % 2, angle1 % 2);
                    // connect points

                    // The first circle that connects cap and tube is special :-/

                    if (i != 0 )  // first point
                        {
                            addTriangle(ref triangles, ref triangleIdx, VertexIdx + i, connectToIdx + i, VertexIdx + i - 1, spin);
                        }
                    if (i != nCirclePolypoints - 1) // last point
                    {
                        addTriangle(ref triangles, ref triangleIdx, connectToIdx + i, VertexIdx + i, connectToIdx + i + 1, spin);
                    }

                    // close the first circle at the last point
                    if (i == nCirclePolypoints-1)
                    {
                        addTriangle(ref triangles, ref triangleIdx, VertexIdx, connectToIdx, VertexIdx + i, spin);
                        addTriangle(ref triangles, ref triangleIdx, connectToIdx, connectToIdx+i, VertexIdx + i, spin);
                    }

                    // is the row complete?
                        if(angle2 ==nCirclePolypoints-1)
                            rowSet = true;

                        i++;
                    }

                

            }
            if (rowSet)
            {
                

                connectToIdx = VertexIdx - nCirclePolypoints;
                //Debug.Log("Jump to: " + (connectToIdx+i) + " vertices: "+ (VertexIdx+i));
            }
        }
        
        m.vertices = points;
        m.triangles = triangles;
        m.uv = uvs;

        //Debug.Log("i was: " + i + " of cap size: " + getSphericalCapSize());
        VertexIdx += i; // index of next element to insert

    }

    private void addTriangle(ref int[] triangles, ref int triangleIdx, int c1, int c2, int c3, int spin = 1)
    {
        if (spin == 1)
        {
            triangles[triangleIdx + 0] = c1;
            triangles[triangleIdx + 1] = c2;
            triangles[triangleIdx + 2] = c3;
            triangleIdx += 3;
        }
        if (spin == -1)
        {
            triangles[triangleIdx + 2] = c1;
            triangles[triangleIdx + 1] = c2;
            triangles[triangleIdx + 0] = c3;
            triangleIdx += 3;
        }
    }

    private int getSphericalCapSize()
    {
        int nFullRings = Mathf.FloorToInt((nCirclePolypoints - 2) / 2);
        int nFullSphere = nFullRings * nCirclePolypoints + 2;
        return nFullSphere / 2;
    }

    private Vector3[] calculateSphere(Vector3 center, float radius)
    {
        Vector3[] points = new Vector3[getSphericalCapSize()*2]; // the full sphere consists of two caps
        for (int j = 0; j < points.Length; j++) points[j] = new Vector3(0, 0, 0);
        Vector3 basis1 = Vector3.up;
        Vector3 basis2 = Vector3.right;
        Vector3 normal = Vector3.forward;
        int spin = 1;


        bool tipPlaced = false; // the very tip of the spherical cap is just one point, not a whole circle.

        int i = 0;

        for (int angle1 = nCirclePolypoints / 2; angle1 >= 0; angle1--)
        {
            for (int angle2 = 0; angle2 < nCirclePolypoints; angle2++)
            {
                float theta = Mathf.PI + 2 * Mathf.PI / nCirclePolypoints * angle2; // runs [0 2Pi]
                float omega = 2 * Mathf.PI / nCirclePolypoints * angle1; // runs [0 Pi]

                int thetaDegree = (int)((theta * 180 / Mathf.PI) % 360);
                int omegaDegree = (int)((omega * 180 / Mathf.PI) % 360);

                //Debug.Log("T:" + theta * 180 / Mathf.PI + ", O:" + omega * 180 / Mathf.PI);
                // Place the tip point
                if (!tipPlaced && (angle1 == nCirclePolypoints / 2 || angle1 == 0)) 
                    {
                        tipPlaced = true;
                        points[i] = calculatePointOnSphere(center, normal, spin, radius, theta, omega, basis1, basis2);

                        addMarker(points[i], i+"(" + thetaDegree + "," + omegaDegree + ")", ColorAssistant.getQualitativeColor(angle1));
                        i++;
                    }
                    // all other sphere points
                    if (angle1 != nCirclePolypoints / 2 && angle1 != 0)
                    {
                        // generate the new point
                        points[i] = calculatePointOnSphere(center, normal, spin, radius, theta, omega, basis1, basis2);

                    
                        addMarker(points[i], i+"("+ thetaDegree+","+omegaDegree+")", ColorAssistant.getQualitativeColor(angle1));
                        tipPlaced = false;
                        i++;
                    }

            }
        }
        Debug.Log("Constructed "+i+"/"+ getSphericalCapSize()*2+" points.");
        return points;
    }

    private void testSphereCap()
    {
        // front cap
        calculateSphereCap(new Vector3(0, 0, -0.2f), Vector3.forward, -1.0f, 1.0f, Vector3.right);
        // back cap
        calculateSphereCap(new Vector3(0, 0,  0.2f), Vector3.forward,  1.0f, 1.0f, Vector3.right);
    }

    private Vector3[] calculateSphereCap(Vector3 center, Vector3 normal, float spin, float radius, Vector3 basis)
    {
        Vector3[] points = new Vector3[getSphericalCapSize()]; // the full sphere consists of two caps
        for (int j = 0; j < points.Length; j++) points[j] = new Vector3(0, 0, 0);

        normal.Normalize(); 
        Vector3 basis2 = Vector3.Cross(normal, basis);
        Vector3 basis1 = Vector3.Cross(normal, basis2);

        bool tipPlaced = false; // the very tip of the spherical cap is just one point, not a whole circle.

        int i = 0;

        for (int angle1 = nCirclePolypoints / 2; angle1 >= 0; angle1--)
        {
            for (int angle2 = 0; angle2 < nCirclePolypoints; angle2++)
            {
                float theta = Mathf.PI + 2 * Mathf.PI / nCirclePolypoints * angle2; // runs [0 2Pi]
                float omega = 2 * Mathf.PI / nCirclePolypoints * angle1; // runs [0 Pi]

                if (Mathf.Cos(omega) <= 0)
                {
                    continue;
                }

                int thetaDegree = (int)((theta * 180 / Mathf.PI) % 360);
                int omegaDegree = (int)((omega * 180 / Mathf.PI) % 360);

                //Debug.Log("T:" + theta * 180 / Mathf.PI + ", O:" + omega * 180 / Mathf.PI);
                // Place the tip point
                if (!tipPlaced && (angle1 == nCirclePolypoints / 2 || angle1 == 0))
                {
                    tipPlaced = true;
                    points[i] = calculatePointOnSphere(center, normal, spin, radius, theta, omega, basis1, basis2);

                    addMarker(points[i], i+"(" + thetaDegree + "," + omegaDegree + ")", ColorAssistant.getQualitativeColor(angle1));
                    i++;
                }
                // all other sphere points
                if (angle1 != nCirclePolypoints / 2 && angle1 != 0)
                {
                    // generate the new point
                    points[i] = calculatePointOnSphere(center, normal, spin, radius, theta, omega, basis1, basis2);


                    addMarker(points[i], i+"(" + thetaDegree + "," + omegaDegree + ")", ColorAssistant.getQualitativeColor(angle1));
                    tipPlaced = false;
                    i++;
                }

            }
        }
        Debug.Log("Constructed " + i + "/" + getSphericalCapSize() + " points.");
        return points;
    }

    private Vector3 calculatePointOnSphere(Vector3 center, Vector3 normal, float spin, float radius, float theta, float omega, Vector3 basis1, Vector3 basis2)
    {
        return center + radius * (basis1 * Mathf.Cos(theta) * Mathf.Sin(omega) + basis2 * Mathf.Sin(theta) * Mathf.Sin(omega) + Mathf.Pow(CapPointiness, 2.0f)*normal * spin * Mathf.Cos(omega) );
    }

}
