using UnityEngine;
using System.Collections;

public class GridJitterSampling
{
    public Grid3D data;
    private float minDist;
    private float cellSize;

    public GridJitterSampling(Vector3 pos, Vector3 size, float p_minDist, int nSamples)
    {
        minDist = p_minDist;
        int sampleBase = Mathf.FloorToInt(Mathf.Pow(nSamples, 1f / 3f));
        if (sampleBase % 2 == 0) sampleBase--;
        nSamples = Mathf.RoundToInt(Mathf.Pow(sampleBase, 3.0f));
        generateGrid(size.x, size.y, size.z, nSamples);
        populateGrid(pos); // center grid parallel to ground plane towards the stem
        jitterGrid(minDist);

        UnityEngine.Debug.Log("Generated " + nSamples + " samples.");
    }

    private void generateGrid(float width, float height, float depth, int nSamples)
    {
        cellSize = -1;
        Triple size = calculateGridSize(width, height, depth, nSamples, ref cellSize);

        data = new Grid3D(size, cellSize);
    }

    private Triple calculateGridSize(float width, float height, float depth, int nSamples, ref float cellSize)
    {

        cellSize = Mathf.Pow((width * height * depth) / nSamples, 1.0f / 3.0f);
        //cellSize = Mathf.Pow((width * height) / nSamples, 1.0f / 2.0f);  // 2D Sampling
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
        offset -= new Vector3((data.GetLength(0)-1) * data.cellSize/2, 0, (data.GetLength(2)-1) * data.cellSize / 2);

        int sampleIdx = 0;
        for (int i = 0; i < data.GetLength(0); i++)
        {
            for (int j = 0; j < data.GetLength(1); j++)
            {
                for (int k = 0; k < data.GetLength(2); k++)
                {
                    // populate with grid vertices
                    data.samples[sampleIdx] = new Vector3(offset.x+i*data.cellSize, offset.y + j * data.cellSize, offset.z + k * data.cellSize);
                    data[i, j, k] = sampleIdx;
                    sampleIdx++;
                }
            }
        }
    }

    /**
     * Jitter grid vertices by an amount so that the minDist condition is not violated
     **/
    private void jitterGrid(float minDist)
    {
        float jitterMagnitude = data.cellSize * ((1-minDist)/2);
        Debug.Log("Jittering cellSize " + data.cellSize + " by " + jitterMagnitude + " conserving distance of " + minDist);
        for(int i=0; i < data.samples.Length; i++)
        {
            data.samples[i] += new Vector3(Random.Range(-jitterMagnitude, jitterMagnitude), Random.Range(-jitterMagnitude, jitterMagnitude), Random.Range(-jitterMagnitude, jitterMagnitude));
        }
    }


    public void visualize()
    {
        foreach (Vector3 sample in data.samples)
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
            float hullSize = ((1 - minDist)/2) * cellSize;
            sphere2.transform.localScale = new Vector3(hullSize, hullSize, hullSize);
            sphere2.transform.position = sample;
            sphere2.name = "hull";
            Color col2 = new Color(0 / 255.0f, 109 / 255.0f, 44 / 255.0f, 0.3f);
            setColor(sphere2, col2);
        }
    }

    private void setColor(GameObject o, Color c)
    {
        Material materialColored;
        if (c.a < 1)
        {
            materialColored = new Material(Shader.Find("Legacy Shaders/Transparent/VertexLit"));
        }
        else { 
            materialColored = new Material(Shader.Find("Unlit/Color"));
        }
        materialColored.color = c;
        o.GetComponent<Renderer>().material = materialColored;
    }



}
