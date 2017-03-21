using UnityEngine;
using System.Collections;
using System;

public class GridJitterSampling2 : PoissonSampling
{
    public Grid3D _data;
    private float _minDist;
    private int _nSamples;
    private float _cellSize;

    /**
     * 
     **/
    public GridJitterSampling2(Vector3 pos, Vector3 size, float minDist, int nSamples):
        base(pos, size)
    {
        _minDist = minDist;
        _nSamples = nSamples;
    }

    private void generateGrid(float width, float height, float depth, int nSamples)
    {
        _cellSize = -1;
        Triple size = calculateGridSize(width, height, depth, nSamples, ref _cellSize);

        _data = new Grid3D(size, _cellSize);
    }

    private Triple calculateGridSize(float width, float height, float depth, int nSamples, ref float cellSize)
    {
        // This is where we differ from the instructions in the original blog post by taking the cubic root.
        // For the 2D case, use the square root.
        cellSize = Mathf.Pow((width * height * depth) / nSamples, 1.0f / 3.0f);
        int nX = Mathf.CeilToInt(width / cellSize);
        int nY = Mathf.CeilToInt(height / cellSize);
        int nZ = Mathf.CeilToInt(depth / cellSize);

        return new Triple(nX, nY, nZ);
    }

    /**
     * Generate samples at grid vertices
     **/
    private void populateGrid(Vector3 offset)
    {
        // To move above the stem
        //offset -= new Vector3((_data.GetLength(0) - 1) * _data.cellSize / 2, 0, (_data.GetLength(2) - 1) * _data.cellSize / 2);

        offset -= new Vector3((_data.GetLength(0) - 1) * _data.cellSize / 2, (_data.GetLength(1) - 1) * _data.cellSize / 2, (_data.GetLength(2) - 1) * _data.cellSize / 2);

        int sampleIdx = 0;
        for (int i = 0; i < _data.GetLength(0); i++)
        {
            for (int j = 0; j < _data.GetLength(1); j++)
            {
                for (int k = 0; k < _data.GetLength(2); k++)
                {
                    // populate with grid vertices
                    _data.samples[sampleIdx] = new Vector3(offset.x + i * _data.cellSize, offset.y + j * _data.cellSize, offset.z + k * _data.cellSize);
                    _data[i, j, k] = sampleIdx;
                    sampleIdx++;
                }
            }
        }
    }

    /**
     * Jitter grid vertices by an amount so that the minDist condition is never violated
     **/
    private void jitterGrid(float minDist)
    {
        float jitterMagnitude = _data.cellSize * ((1 - minDist) / 2); // jitter both sizes by a maximum of cellsize-(minDist/2). If they both jitter towards opposite directions, they still cannot violate the minDist criterium.
        Debug.Log("Jittering cellSize " + _data.cellSize + " by " + jitterMagnitude + " conserving a distance of " + minDist);
        for (int i = 0; i < _data.samples.Length; i++)
        {
            _data.samples[i] += new Vector3(UnityEngine.Random.Range(-jitterMagnitude, jitterMagnitude), UnityEngine.Random.Range(-jitterMagnitude, jitterMagnitude), UnityEngine.Random.Range(-jitterMagnitude, jitterMagnitude));
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
            Color col = new Color(0 / 255.0f, 109 / 255.0f, 44 / 255.0f);
            setColor(sphere, col);

            // draw Neighborhood hull
            // 186,228,179
            GameObject sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float hullSize = ((1 - _minDist) / 2) * _cellSize;
            sphere2.transform.localScale = new Vector3(hullSize, hullSize, hullSize);
            sphere2.transform.position = sample;
            sphere2.name = "hull";
            Color col2 = new Color(0 / 255.0f, 109 / 255.0f, 44 / 255.0f, 0.3f);
            setColor(sphere2, col2);
        }
    }

    public override Vector3[] sample()
    {
        generateGrid(base._size.x, base._size.y, base._size.z, _nSamples);
        populateGrid(base._center);
        jitterGrid(_minDist);

        UnityEngine.Debug.Log("Generated " + _nSamples + " samples.");
        return _data.samples;
    }
}
