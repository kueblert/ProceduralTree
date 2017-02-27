using UnityEngine;
using System.Collections;

public class BallBSpline {

    private int _n;

    private Vector4[]  _balls; // 3d position + radius
    private float[] _u;


    private Vector4[,] _d;

    public BallBSpline()
    {
        _n = 2;
        int nControlPoints = 7;

        _balls = new Vector4[nControlPoints];
        _balls[0] = new Vector4(0, 0, 0, 0.10f);
        _balls[1] = new Vector4(0, 0, 1, 0.20f);
        _balls[2] = new Vector4(0, 1, 1, 0.20f);
        _balls[3] = new Vector4(1, 1, 1, 0.30f);
        _balls[4] = new Vector4(1, 0, 0, 0.40f);
        _balls[5] = new Vector4(1, 1, 0, 0.30f);
        _balls[6] = new Vector4(0, 1, 0, 0.20f);

        _d = new Vector4[nControlPoints + _n, _n+1];

        _u = new float[nControlPoints+ _n];
        for(int i=1; i < nControlPoints + _n; i++)
        {
            _u[i] = (float)i;
        }

    }

    // http://www.idav.ucdavis.edu/education/CAGDNotes/Deboor-Cox-Calculation/Deboor-Cox-Calculation.html
    // https://en.wikipedia.org/wiki/De_Boor's_algorithm
    private Vector4 deBoorsAlgorithm( float x, ref Vector4 dc, ref Vector4 ddc)
    {
        int l = (int)x; // find u's interval that contains x.

        //Debug.Log("deBoors init");
        for (int i=l- _n;  i <= l; i++) {
            //Debug.Log("N0("+i+","+x+")");
            if(i >= 0) { 
            _d[i, 0] = N0(i, x);
            }
        }

        //Debug.Log("deBoors recursion");
        for (int k = 1; k <= _n; k++) {
            for (int i = l - _n + k; i <=l; i++) {
                //Debug.Log("_d[" + i + "," + k + "]");
                if(i >= 1) { 
                _d[i, k] = (1 - alpha(k, i, x)) * _d[i - 1, k - 1] + alpha(k, i, x) * _d[i, k - 1];
                }
            }
        }
        // calculate first derivative (responsible for the correct center of the circle)
        //Vector4 _dLplus1Kminus1 = (1 - alpha(_n-1, l+1, x)) * _d[l, _n - 2] + alpha(_n - 1, l + 1, x) * _d[l+1, _n - 2];
        //Debug.Log("_u[" + (l + _n) + "], length " + _u.Length);

        // https://en.wikipedia.org/wiki/B-spline
        //dc = (_n - 1) * (-_d[l, _n-1] / (_u[l + _n - 1] - _u[l]) + _d[l-1, _n - 1] / (_u[l + _n] - _u[l-1]) );

        // http://folk.uio.no/in329/nchap3.pdf
        //dc = _n * (-_d[l, _n - 1] / (_u[l + _n] - _u[l]) + _d[l - 1, _n - 1] / (_u[l + _n - 1 ] - _u[l - 1]));

        // calculate second derivative (responsible for the orientation of the circle)
        //dc = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        //ddc = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        

        dc = _d[l, _n -1];
        ddc = _d[l-1, _n -1];
        return _d[l,_n];
    }



    private Vector4 N0(int i, float x)
    {
        //Debug.Log("_balls[" + i + "] of " + _balls.Length);
        // resolve edge cases (beginning and end of spline)
        i = Mathf.Min(Mathf.Max(0, i), _balls.Length-1);
        return _balls[i];
    }

    private float alpha(int k, int i, float x)
    {
        //Debug.Log("_u[" + i + "] _u[" + (i + _n + 1 - k) + "] of " + _u.Length);

        // Resolve edge cases (beginning and end of spline)
        int i1 = i;
        int i2 = i + _n + 1 - k;
        i1 = Mathf.Min(Mathf.Max(0, i1), _u.Length - 1);
        i2 = Mathf.Min(Mathf.Max(0, i2), _u.Length - 1);
        i2 = i2 - (_n + 1 - k);
        i = Mathf.Min(i1, i2);
        return (x - _u[i]) / (_u[i + _n + 1 - k] - _u[i]);
    }

    private Vector3[] calculateHull(float t)
    {
        Vector4 dc = new Vector4();
        Vector4 ddc = new Vector4();
        Vector4 c = deBoorsAlgorithm(t, ref dc, ref ddc);

        int nSegments = 10;
        int currentSegment = 0;
        Vector3[] segments = new Vector3[nSegments];
        for (float theta = 0.0f; theta < 2*Mathf.PI; theta+= Mathf.PI * 2 / nSegments) {
            //segments[currentSegment] = calculatePointOnCircle(c, dc, ddc, theta);
            segments[currentSegment] = calculatePointOnCircle(new Vector3(c.x, c.y, c.z), new Vector3(dc.x, dc.y, dc.z), c.w, theta);
            currentSegment++;
                }
        return segments;
    }

    private void drawHull()
    {
        Vector4 dc = new Vector4();
        Vector4 ddc = new Vector4();
        Vector4 c = new Vector4(); ;
        Vector4 previousC = new Vector4();
        Vector4 previousDC = new Vector4();

        int nSegments = 10;

        for (float x = 1.0f; x < 7.0f; x += 0.01f)
        {
            c = deBoorsAlgorithm(x, ref dc, ref ddc);
            if (x ==1.0f) { previousC = c;  continue; }
            if(x == 1.1f) { previousC = c; previousDC = (c- previousC); continue; }

            //dc  = c - previousC;
            //ddc = dc - previousDC;

            // calculate hull

            // update measures
            previousC = c;
            previousDC = dc;


            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(c.x, c.y, c.z);
            sphere.transform.localScale = new Vector3(c.w*2, c.w * 2, c.w * 2);
            
            int currentSegment = 0;
            Vector3[] segments = new Vector3[nSegments];
            for (float theta = 0.0f; theta < 2 * Mathf.PI; theta += Mathf.PI * 2 / nSegments)
            {
                segments[currentSegment] = calculatePointOnCircle(c, dc.normalized, ddc.normalized, theta);
                currentSegment++;
            }

            LineRenderer lr = initLine(Color.blue, 0.03f, "polyline");
            for (int i = 0; i < segments.Length; i++)
            {
                extendLine(segments[i], lr, i);
            }
            extendLine(segments[0], lr, segments.Length); // close the circle
            
        }
    }


    private Vector3 calculatePointOnCircle(Vector3 center, Vector3 normal, float radius, float theta)
    {
        Vector3 basis1, basis2;
        Vector3 dir = Vector3.left;// new Vector3(0.3f, 0.3f, 0.3f).normalized;
        // choose the two basis vectors, any that are both orthogonal to the normal vector are fine.
        if (Vector3.Dot(dir, normal) > 0.00001f)  // check for numerical stability
        {
            basis1 = Vector3.Cross(dir, normal).normalized;
        }
        else
        {
            basis1 = Vector3.Cross(Vector3.up, normal).normalized;
        }
        basis2 = Vector3.Cross(basis1, normal).normalized;

        return center + radius * (basis1 * Mathf.Cos(theta) + basis2 * Mathf.Sin(theta));
    }

    private Vector3 calculatePointOnCircle(Vector4 c, Vector4 dc, Vector4 ddc, float theta)
    {
        Vector3 c_mid = new Vector3(c.x, c.y, c.z);
        Vector3 dc_mid = new Vector3(dc.x, dc.y, dc.z);
        return c_mid + lambda(c, dc) * dc_mid + rho(c, dc) * (calcN(c, ddc) * Mathf.Cos(theta) + calcM(c, ddc) * Mathf.Sin(theta));
    }

    private float lambda(Vector4 c, Vector4 dc)
    {
        return (-dc.w * c.w) / (dc.x*dc.x+dc.y*dc.y+dc.z*dc.z);
    }

    private float rho(Vector4 c, Vector4 dc)
    {
        // This should be +/- so that we get two points on the circle
        return Mathf.Sqrt(c.w*c.w*((1-dc.w*dc.w)/(dc.magnitude* dc.magnitude)));
    }

    private Vector3 calcN(Vector4 c, Vector4 ddc)
    {
        return new Vector3(ddc.x, ddc.y, ddc.z)*c.w;
    }

    private Vector3 calcM(Vector4 c, Vector4 ddc)
    {
        return Vector3.Cross(new Vector3(c.x, c.y, c.z), calcN(c, ddc));
    }


    public void visualizeSurface()
    {
        //drawHull();
        
        for( float x = 1.0f; x < 7.0f; x += 0.1f)
        {
            Vector3[] p = calculateHull(x);
            LineRenderer lr = initLine(Color.blue, 0.03f, "polyline");
            for(int i=0; i < p.Length; i++) { 
            extendLine(p[i], lr, i);
            }
            extendLine(p[0], lr, p.Length); // close the circle
        }
        
    }

    public void visualize()
    {
        LineRenderer lr = initLine(Color.red, 0.03f, "polyline");
        int pointCount = 0;

        for (float x = 1.0f; x < 7.0f; x += 0.1f)
        {
            Vector4 dx = new Vector4(); Vector4 ddx = new Vector4();
            Vector4 p = deBoorsAlgorithm(x, ref dx, ref ddx);
            extendLine(new Vector3(p.x, p.y, p.z), lr, pointCount);
            pointCount++;
        }

        int ballidx = 1;
        foreach (Vector3 ball in _balls)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(ballidx / 20.0f, ballidx / 20.0f, ballidx / 20.0f);
            sphere.transform.position = ball;
            sphere.name = "Control Point";
            Color col = new Color(0 / 255.0f, 109 / 255.0f, 44 / 255.0f);
            setColor(sphere, col);
            ballidx++;
        }
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

    private LineRenderer initLine(Color c, float width = 0.03f, string name = "line")
    {
        GameObject myLine = new GameObject(name);
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        myLine.GetComponent<Renderer>().material.color = c;
        lr.material = new Material(Shader.Find("Standard"));
        lr.material.color = c;
        lr.SetWidth(width, width);

        return lr;
    }

    private void extendLine(Vector3 pos, LineRenderer lr, int count)
    {
        lr.SetVertexCount(count + 1);
        lr.SetPosition(count, pos);
    }

}
