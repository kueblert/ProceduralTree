using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PoissonDiskSampling2 : PoissonSampling
{
    // Implementation according to
    // http://devmag.org.za/2009/05/03/poisson-disk-sampling/
    // based upon
    // http://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf

    public Grid3D2 _grid;

    public int _maxPointCount;
    public float _minDist;

    private int _k = 30;
    private RandomQueue<int> _activeList;

    // debug
    private Vector3 consideredSample;
    private Vector3 newSample;
    private GameObject _visualization;

    private class RandomQueue<T> : List<T>
    {
        public T pop()
        {
            int randomIndex = (int)Mathf.Floor(UnityEngine.Random.value * (this.Count - 1));
            T value = this[randomIndex];
            this.RemoveAt(randomIndex);
            return value;
        }
    }

    public PoissonDiskSampling2(float width, float height, float depth, float minDist, int k = 30, int maxPointCount = 10000)
     : base(new Vector3(0, 0, 0), new Vector3(width, height, depth))
    {
        _minDist = minDist;
        _k = k;
        _maxPointCount = maxPointCount;

        float cellSize = _minDist / Mathf.Sqrt(3.0f);      // sqrt of dimension, in this case 3D

        _grid = new Grid3D2(new Triple(
        Mathf.CeilToInt(_size.x / cellSize),                   // grid width
        Mathf.CeilToInt(_size.y / cellSize),                  // grid height
        Mathf.CeilToInt(_size.z / cellSize)), cellSize);       // grid depth

        //RandomQueue works like a queue, except that it
        //pops a random element from the queue instead of
        //the element at the head of the queue
        _activeList = new RandomQueue<int>();
    }

    Vector3 generateRandomPointAround(Vector3 p)
    { //non-uniform, leads to denser packing.
        float r1 = UnityEngine.Random.value; //random point between 0 and 1
        float r2 = UnityEngine.Random.value;
        float r3 = UnityEngine.Random.value;
        //random radius between mindist and 2* mindist
        float radius = _minDist * (r1 + 1);
        //random angle
        float angle1 = 2 * Mathf.PI * r2;
        float angle2 = 2 * Mathf.PI * r3;
        //the new point is generated around the point (x, y, z)
        float newX = p.x + radius * Mathf.Cos(angle1) * Mathf.Sin(angle2);
        float newY = p.y + radius * Mathf.Sin(angle1) * Mathf.Sin(angle2);
        float newZ = p.z + radius * Mathf.Cos(angle2);
        return new Vector3(newX, newY, newZ);
    }

    bool inNeighbourhood(Vector3 point)
    {
        float sqrDist = _minDist * _minDist;
        //get the neighbourhood if the point in the grid
        // check between minDist and 2 * mindist implies 2 cells to each side.
        // Thus we need a subgrid of size 2*2+1 (2 to both sides + 1 for the center)
        int neighbourhoodSize = 5;
        Grid3D2 cubeAroundPoint = _grid.getSubgrid(point, neighbourhoodSize);

        // check the distance
        foreach (int c in cubeAroundPoint)
        {
            if (c != Grid3D2.invalid) { 
                if ((_grid.samples[c] - point).sqrMagnitude < sqrDist)
                {
                    return true;
                }
            }
        }
        return false;
    }

    void initialize(float width, float height, float depth)
    {

        // generate the first point randomly
        // and update

        Vector3 firstPoint = new Vector3(UnityEngine.Random.value * width, UnityEngine.Random.value * height, UnityEngine.Random.value * depth);
        newSample = firstPoint;
        //update containers
        int idx = _grid.addSample(firstPoint);
        _activeList.Add(idx);
    }

    void iteration()
    {
        //generate other points from points in queue.
        if (_activeList.Count > 0 && _grid.nSamples < _maxPointCount)
        {
            // Select random point
            int currentIdx = _activeList.pop();
            Vector3 point = _grid[currentIdx];
            consideredSample = point;
            // if we want less points than k (which is usually fixed to 30), don't try more.
            int k = Mathf.Min(_k, _maxPointCount);
            bool pointInserted = false;
            for (int i = 0; i < k; i++)
            {
                Vector3 newPoint = generateRandomPointAround(point);
                //check that the point is in the image region
                //and no points exists in the point's neighbourhood
                if (inCube(newPoint, _size.x, _size.y, _size.z) && !inNeighbourhood(newPoint))
                {
                    newSample = newPoint;
                    //update containers
                    
                    int idx = _grid.addSample(newPoint);
                    _activeList.Add(idx);
                    pointInserted = true;
                    break;
                }
            }
            // reinsert the sample, if a new point was generated
            if (pointInserted)
            {
                _activeList.Add(currentIdx);
            }

        }
        else{
            // Resize to actual number of samples generated
            finalize();
        }
        
        
    }

    private void finalize()
    {
        var obj = _grid.samples;
        Array.Resize(ref obj, _grid.nSamples);
        _grid.samples = obj;
    }

    bool inCube(Vector3 sample, float width, float height, float depth)
    {
        return sample.x >= 0 && sample.x < width && sample.y >= 0 && sample.y < height && sample.z >= 0 && sample.z < depth;
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

    public override Vector3[] sample()
    {
        while (_activeList.Count > 0 || _grid.nSamples == 0)
        {
            if (_grid.nSamples == 0)
            {
                initialize(_size.x, _size.y, _size.z);
            }
            else
            {
                iteration();
            }
            Debug.Log("nSamples: " + _grid.nSamples + ", process list: " + _activeList.Count);
        }
        finalize();

        UnityEngine.Debug.Log("Generated " + _grid.samples.Length + " samples.");
        return _grid.samples;
    }

    public override void visualize()
    {
        GameObject.Destroy(_visualization);
        _visualization = new GameObject("Sampling");

        visualizeSamples();
        //_grid.visualize();
    }

    private void visualizeSamples()
    {

        for (int i = 0; i < _grid.nSamples; i++)
        {
            Vector3 sample = _grid.samples[i];
            // draw a sphere at the sample location
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.position = sample;
            sphere.name = "sample";
            Color col = ColorAssistant.getQualitativeColor(0);
            setColor(sphere, col);
            sphere.transform.SetParent(_visualization.transform);
        }

        // throwing dart around this one
        GameObject sphere3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere3.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        sphere3.transform.position = consideredSample;
        sphere3.name = "Considered Sample";
        Color col3 = ColorAssistant.getQualitativeColor(2);
        setColor(sphere3, col3);
        sphere3.transform.SetParent(_visualization.transform);

        // generated this one
        GameObject sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere2.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        sphere2.transform.position = newSample;
        sphere2.name = "New Sample";
        Color col2 = ColorAssistant.getQualitativeColor(1);
        setColor(sphere2, col2);
        sphere2.transform.SetParent(_visualization.transform);
    }

}
