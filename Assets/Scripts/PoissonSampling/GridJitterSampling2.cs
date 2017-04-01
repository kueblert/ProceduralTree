using UnityEngine;
using System.Collections;
using System;

public class GridJitterSampling2 : PoissonSampling
{
    public Grid3D _data;
    private float _minDist;
    private int _nSamples;


    /**
     * 
     **/
    public GridJitterSampling2(Vector3 pos, Vector3 size, float minDist, int nSamples):
        base(pos, size)
    {
        _minDist = minDist;
        _nSamples = nSamples;
    }

    private int generateGrid(float width, float height, float depth, int nSamples)
    {
        float cellSize = getCellSize(width, height, depth);
        Triple size = calculateGridSize(width, height, depth, cellSize);
        _data = new Grid3D(size, cellSize);

        return nSamples;
    }

    private float getCellSize(float width, float height, float depth)
    {
        // This is where we differ from the instructions in the original blog post by taking the cubic root.
        // For the 2D case, use the square root.
        float cellSize = Mathf.Pow((width * height * depth) / _nSamples, 1.0f / 3.0f);

        // check whether the resulting cell size allows for the desired minimum distance.
        if (cellSize < _minDist)
        {
            // if not, reduce the number of samples
            _nSamples = Mathf.FloorToInt((width * height * depth) / Mathf.Pow(_minDist, 3.0f));
            Debug.LogWarning("Desired minimal distance cannot be achieved for the requested amount of samples. Reducing samples to " + _nSamples + ", resulting in only minimal jitter!");
            cellSize = Mathf.Pow((width * height * depth) / _nSamples, 1.0f / 3.0f);
        }

        return cellSize;
    }

    private Triple calculateGridSize(float width, float height, float depth, float cellSize)
    {
        
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

    private void sparseGrid()
    {
        // if we want more samplesthan were generated, we have a problem earlier on
        if (_nSamples > _data.samples.Length) throw new Exception("Not enough samples were generated!");

        // only do something, if required to do so
        if(_nSamples != _data.samples.Length)
        {
            // a correctly sized samples array
            Vector3[] samples = new Vector3[_nSamples];
            // find out how many and which samples to delete by choosing them randomly
            int nSamplesToDelete = _data.samples.Length - _nSamples;
            int[] samplesToDelete = getUniqueRandomsInRange(nSamplesToDelete, 0, _data.samples.Length);

            // make a copy of those samples that we want to keep
            int offset = 0;
            for(int i=0; i < _data.samples.Length; i++)
            {
                // is this an index that we want to delete?
                bool containedInDelete = false;
                for (int j = 0; j < samplesToDelete.Length; j++) if (samplesToDelete[j] == i) containedInDelete = true;
                if (containedInDelete)
                {
                    offset--;
                }
                else { 
                    // or a keeper?
                    samples[i + offset] = _data.samples[i];
                }
            }

            // override the original data with the selected samples only
            _data.samples = samples;
        }
    }

    /**
     * Get n unique integers within [min, max[ with min inclusive, max exclusive.
    **/
    private int[] getUniqueRandomsInRange(int n, int min, int max)
    {
        if (max - min <= n) throw new Exception("Not sufficient intergers in [" + min + "," + max + "[ to draw " + n + " samples.");
        int[] drawn = new int[n];
        int insertionIndex = 0;
        while (insertionIndex < n)
        {
            // draw a random index
            int currentDraw = -1;
            bool isUnique = false;
            while(currentDraw==-1 || !isUnique) {
                currentDraw = UnityEngine.Random.Range(min, max);
                isUnique = true;
                for(int i=0; i < insertionIndex; i++)
                {
                    if (drawn[i] == currentDraw) isUnique = false;
                }
            }
            
            drawn[insertionIndex] = currentDraw;
            insertionIndex++;
        }

            return drawn;
    }

    /**
     * Jitter grid vertices by an amount so that the minDist condition is never violated
     **/
    private void jitterGrid()
    {
        float jitterMagnitude = getJitterMagnitude(); 
        Debug.Log("Jittering cellSize " + _data.cellSize + " by " + jitterMagnitude + " conserving a distance of " + _minDist);
        for (int i = 0; i < _data.samples.Length; i++)
        {
            _data.samples[i] += new Vector3(UnityEngine.Random.Range(-jitterMagnitude, jitterMagnitude), UnityEngine.Random.Range(-jitterMagnitude, jitterMagnitude), UnityEngine.Random.Range(-jitterMagnitude, jitterMagnitude));
        }
    }


    private float getJitterMagnitude()
    {
        // jitter both sizes by a maximum of cellsize-(minDist/2). If they both jitter towards opposite directions, they still cannot violate the minDist criterium.
        return _data.cellSize * ((1 - _minDist) / 2);
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

        visualizeSamples();
        //visualizeBoundingVolume();
    }

    private void visualizeSamples()
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

            // draw Neighborhood hull
            // 186,228,179
            GameObject sphere2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            float hullSize = _minDist; // diameter of the sphere = 2 * radius. Therefore, no two such hull spheres should intersect
            sphere2.transform.localScale = new Vector3(hullSize, hullSize, hullSize);
            sphere2.transform.position = sample;
            sphere2.name = "hull";
            Color col2 = ColorAssistant.getDivergingColor(1);
            col2.a = 70.0f / 255.0f;
            setColor(sphere2, col2);
        }
    }

    /**
     * Shows a cube in which all samples should be contained
     **/
    private void visualizeBoundingVolume()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = base._size;
        cube.transform.position = base._center;
        cube.name = "BoundingVolume";
        Color col = ColorAssistant.getQualitativeColor(1);
        col.a = 100.0f / 255.0f;
        setColor(cube, col);
    }

    /**
     * Generate the samples
     **/
    public override Vector3[] sample()
    {

        generateGrid(base._size.x, base._size.y, base._size.z, _nSamples);
        // generate full grid
        populateGrid(base._center);
        // delete samples randomly until only the requested amount is left.
        sparseGrid();
        jitterGrid();

        UnityEngine.Debug.Log("Generated " + _data.samples.Length + "/" + _nSamples + " samples.");
        return _data.samples;
    }
}
