using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PoissonDiskSampling : PoissonSampling
{
    // Implementation according to
    // http://devmag.org.za/2009/05/03/poisson-disk-sampling/
    // based upon
    // http://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf

    public Grid3D _data;
    private int _maxPointCount;
    private float _minDist;
    private int _k = 30;

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

    public PoissonDiskSampling(float width, float height, float depth, float minDist, int k = 30, int maxPointCount = 10000) :
    base(new Vector3(0, 0, 0), new Vector3(width, height, depth))
    {
        _minDist = minDist;
        _k = k;
        _maxPointCount = maxPointCount;
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

    bool inNeighbourhood(Grid3D grid, Vector3 point)
    {
        float sqrDist = _minDist * _minDist;
        //get the neighbourhood if the point in the grid
        // check between minDist and 2 * mindist implies 2 cells to each side.
        // Thus we need a subgrid of size 2*2+1 (2 to both sides + 1 for the center)
        Grid3D cellsAroundPoint = cubeAroundPoint(grid, point, 5);

        // check the distance
        foreach (int c in cellsAroundPoint)
        {
            if (!c.Equals(Grid3D.invalid))
                if ((_data.samples[c] - point).sqrMagnitude < sqrDist)
                {
                    return true;
                }
        }
        return false;
    }

    Grid3D cubeAroundPoint(Grid3D grid, Vector3 p, int neighbourhoodSize)
    {
        return grid.getSubgrid(p, neighbourhoodSize);
    }


    void generate_poisson(float width, float height, float depth)
    {

        //Create the grid
        float cellSize = _minDist / Mathf.Sqrt(3.0f);      // sqrt of dimension, in this case 3D

          _data = new Grid3D(new Triple(
          Mathf.CeilToInt(width / cellSize),                   // grid width
          Mathf.CeilToInt(height / cellSize),                  // grid height
          Mathf.CeilToInt(depth / cellSize)), cellSize);       // grid depth


        //RandomQueue works like a queue, except that it
        //pops a random element from the queue instead of
        //the element at the head of the queue
        RandomQueue<Vector3> processList = new RandomQueue<Vector3>();
        Vector3[] samples = new Vector3[_maxPointCount];


        // generate the first point randomly
        // and update

        Vector3 firstPoint = new Vector3(UnityEngine.Random.value * width, UnityEngine.Random.value * height, UnityEngine.Random.value * depth);

        //update containers
        processList.Add(firstPoint);
        int nSamples = 0;
        samples[nSamples] = firstPoint;
        _data[firstPoint] = nSamples;
        nSamples++;

        //generate other points from points in queue.
        while (processList.Count > 0 && nSamples < _maxPointCount)
        {
            // Select random point
            Vector3 point = processList.pop();
            // if we want less points than k (which is usually fixed to 30), don't try more.
            int k = Mathf.Min(_k, _maxPointCount);
            bool pointInserted = false;
            for (int i = 0; i < k; i++)
            {
                Vector3 newPoint = generateRandomPointAround(point);
                //check that the point is in the image region
                //and no points exists in the point's neighbourhood
                if (inCube(newPoint, width, height, depth) && !inNeighbourhood(_data, newPoint))
                {
                    //update containers
                    processList.Add(newPoint);
                    samples[nSamples] = newPoint;
                    _data[newPoint] = nSamples;
                    pointInserted = true;
                    nSamples++;
                    break;
                }
            }
            // reinsert the sample, if a new point was generated
            if (pointInserted)
            {
                processList.Add(point);
            }

        }

        // Resize to actual number of samples generated
        var obj = samples;
        Array.Resize(ref obj, nSamples);
        _data.samples = obj;
    }

    bool inCube(Vector3 sample, float width, float height, float depth)
    {
        return sample.x >= 0 && sample.x < width && sample.y >= 0 && sample.y < height && sample.z >= 0 && sample.z < depth;
    }

    public override Vector3[] sample()
    {
        generate_poisson(base._size.x, base._size.y, base._size.z);
        UnityEngine.Debug.Log("Generated " + _data.samples.Length + " samples.");
        return _data.samples;
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

    public override void visualize()
    {
        foreach (Vector3 sample in _data.samples)
        {
            // draw a sphere at the sample location
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.position = sample;
            sphere.name = "sample";
            Color col = ColorAssistant.getDivergingColor(0);
            setColor(sphere, col);
        }

        _data.visualize();
    }

}
