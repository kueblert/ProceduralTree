using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid3D2 : IEnumerable<int>
{
    // Reference array
    private int[,,] data;
    // Data array
    public Vector3[] samples { get; set; }

    public float _cellSize { get; set; }

    public static int invalid = -1;
    public int nSamples;

    private GameObject _visualization;

    public Grid3D2(Triple size, float cellSize)
    {

        _cellSize = cellSize;

        data = new int[size.x, size.y, size.z];
        for (int i = 0; i < size.x; i++)
            for (int j = 0; j < size.y; j++)
                for (int k = 0; k < size.z; k++)
                    data[i, j, k] = -1;

        samples = new Vector3[size.x * size.y * size.z];
        nSamples = 0;
    }

    public int addSample(Vector3 sample)
    {
        samples[nSamples] = sample;

        Triple i = imageToGrid(sample);
        data[i.x, i.y, i.z] = nSamples;

        nSamples++;

        return nSamples - 1;
    }

    public Vector3 this[int sampleReference]
    { 
        get
        {
            // indexer
            return samples[sampleReference];
        }
        set
        {
            samples[sampleReference] = value;
        }

    }

    /*
    public int this[int i, int j, int k]
    {
        get
        {
            // indexer
            return data[i, j, k];
        }
        set
        {
            data[i, j, k] = value;
        }

    }
    
    public int this[Vector3 idx]
    {
        get
        {
            // indexer
            Triple i = imageToGrid(idx);
            return data[i.x, i.y, i.z];
        }
        set
        {
            Triple i = imageToGrid(idx);
            data[i.x, i.y, i.z] = value;
        }

    }
    */

    /**
     * WARNING: This copies only the reference, not the data array!
     **/
    public Grid3D2 getSubgrid(Vector3 p, int extend)
    {
        Triple p_grid = imageToGrid(p);
        Grid3D2 subgrid = new Grid3D2(new Triple(extend, extend, extend), _cellSize);
        int centerIdx = (extend - 1) / 2 + 1;
        for (int i = 0; i < extend; i++)
        {
            for (int j = 0; j < extend; j++)
            {
                for (int k = 0; k < extend; k++)
                {
                    int x = (int)p_grid.x + i - (extend - 1) / 2;
                    int y = (int)p_grid.y + j - (extend - 1) / 2;
                    int z = (int)p_grid.z + k - (extend - 1) / 2;
                    if (x >= 0 && x < data.GetLength(0) && y >= 0 && y < data.GetLength(1) && z >= 0 && z < data.GetLength(2))
                    {
                        subgrid.data[i, j, k] = data[x, y, z];
                    }
                    else
                    {
                        subgrid.data[i, j, k] = invalid;
                    }

                }
            }
        }

        return subgrid;
    }

    private Triple imageToGrid(Vector3 point)
    {
        int gridX = (int)(point.x / _cellSize);
        int gridY = (int)(point.y / _cellSize);
        int gridZ = (int)(point.z / _cellSize);
        return new Triple(gridX, gridY, gridZ);
    }


    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < data.GetLength(0); i++)
            for (int j = 0; j < data.GetLength(1); j++)
                for (int k = 0; k < data.GetLength(2); k++)
                    yield return this.data[i, j, k];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int GetLength(int dimension)
    {
        return data.GetLength(dimension);
    }

    public void visualize()
    {
        GameObject.Destroy(_visualization);
        _visualization = new GameObject("Grid3D2");

        for (int i = 0; i < GetLength(0); i++)
        {
            for (int j = 0; j < GetLength(1); j++)
            {
                for (int k = 0; k < GetLength(2); k++)
                {
                    Vector3 mid = new Vector3(i * _cellSize, j * _cellSize, k * _cellSize);

                    // Corner vertices
                    Vector3 leftTopFront = mid + new Vector3(-_cellSize / 2, -_cellSize / 2, -_cellSize / 2);
                    Vector3 leftBottomFront = mid + new Vector3(-_cellSize / 2, _cellSize / 2, -_cellSize / 2);
                    Vector3 rightBottomFront = mid + new Vector3(_cellSize / 2, _cellSize / 2, -_cellSize / 2);
                    Vector3 rightTopFront = mid + new Vector3(_cellSize / 2, -_cellSize / 2, -_cellSize / 2);
                    Vector3 leftTopBack = mid + new Vector3(-_cellSize / 2, -_cellSize / 2, _cellSize / 2);
                    Vector3 leftBottomBack = mid + new Vector3(-_cellSize / 2, _cellSize / 2, _cellSize / 2);
                    Vector3 rightBottomBack = mid + new Vector3(_cellSize / 2, _cellSize / 2, _cellSize / 2);
                    Vector3 rightTopBack = mid + new Vector3(_cellSize / 2, -_cellSize / 2, _cellSize / 2);

                    Color col = new Color(0.0f, 0.0f, 0.0f, 0.3f);
                    // Front side
                    drawLine(leftTopFront, leftBottomFront, col);
                    drawLine(leftBottomFront, rightBottomFront, col);
                    drawLine(rightBottomFront, rightTopFront, col);
                    drawLine(rightTopFront, leftTopFront, col);

                    // Back side
                    drawLine(leftTopBack, leftBottomBack, col);
                    drawLine(leftBottomBack, rightBottomBack, col);
                    drawLine(rightBottomBack, rightTopBack, col);
                    drawLine(rightTopBack, leftTopBack, col);

                    // Top side
                    drawLine(leftTopBack, rightTopBack, col);
                    drawLine(rightTopBack, rightTopFront, col);
                    drawLine(rightTopFront, leftTopFront, col);
                    drawLine(leftTopFront, leftTopBack, col);

                    // Bottom side
                    drawLine(leftBottomBack, rightBottomBack, col);
                    drawLine(rightBottomBack, rightBottomFront, col);
                    drawLine(rightBottomFront, leftBottomFront, col);
                    drawLine(leftBottomFront, leftBottomBack, col);

                    // Left side
                    drawLine(leftBottomBack, leftBottomFront, col);
                    drawLine(leftBottomFront, leftTopFront, col);
                    drawLine(leftTopFront, leftTopBack, col);
                    drawLine(leftTopBack, leftBottomBack, col);

                    // Right side
                    drawLine(rightBottomBack, rightBottomFront, col);
                    drawLine(rightBottomFront, rightTopFront, col);
                    drawLine(rightTopFront, rightTopBack, col);
                    drawLine(rightTopBack, rightBottomBack, col);
                }
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
        myLine.transform.SetParent(_visualization.transform);
    }



}
