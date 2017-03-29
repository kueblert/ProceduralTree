using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Grid3D : IEnumerable<int>
{

    private int[,,] data;
    public Vector3[] samples { get; set; }
    public float cellSize { get; set; }

    public static int invalid = -1;

    public Grid3D(Triple size, float p_cellSize)
    {
        data = new int[size.x, size.y, size.z];

        for (int i = 0; i < size.x; i++)
            for (int j = 0; j < size.y; j++)
                for (int k = 0; k < size.z; k++)
                    data[i, j, k] = -1;

        cellSize = p_cellSize;
        samples = new Vector3[size.x * size.y * size.z];
    }

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

    // relevant?
    public Grid3D getSubgrid(Vector3 p, int extend)
    {
        Triple p_grid = imageToGrid(p);
        Grid3D subgrid = new Grid3D(new Triple(extend, extend, extend), cellSize);
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
                        subgrid[i, j, k] = data[x, y, z];
                    }
                    else
                    {
                        subgrid[i, j, k] = invalid;
                    }

                }
            }
        }

        return subgrid;
    }

    private Triple imageToGrid(Vector3 point)
    {
        int gridX = (int)(point.x / cellSize);
        int gridY = (int)(point.y / cellSize);
        int gridZ = (int)(point.z / cellSize);
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
        for (int i = 0; i < GetLength(0); i++)
        {
            for (int j = 0; j < GetLength(1); j++)
            {
                for (int k = 0; k < GetLength(2); k++)
                {
                    Vector3 mid = new Vector3(i * cellSize, j * cellSize, k * cellSize);

                    // Corner vertices
                    Vector3 leftTopFront = mid + new Vector3(-cellSize / 2, -cellSize / 2, -cellSize / 2);
                    Vector3 leftBottomFront = mid + new Vector3(-cellSize / 2, cellSize / 2, -cellSize / 2);
                    Vector3 rightBottomFront = mid + new Vector3(cellSize / 2, cellSize / 2, -cellSize / 2);
                    Vector3 rightTopFront = mid + new Vector3(cellSize / 2, -cellSize / 2, -cellSize / 2);
                    Vector3 leftTopBack = mid + new Vector3(-cellSize / 2, -cellSize / 2, cellSize / 2);
                    Vector3 leftBottomBack = mid + new Vector3(-cellSize / 2, cellSize / 2, cellSize / 2);
                    Vector3 rightBottomBack = mid + new Vector3(cellSize / 2, cellSize / 2, cellSize / 2);
                    Vector3 rightTopBack = mid + new Vector3(cellSize / 2, -cellSize / 2, cellSize / 2);

                    // Front side
                    drawLine(leftTopFront, leftBottomFront, Color.gray);
                    drawLine(leftBottomFront, rightBottomFront, Color.gray);
                    drawLine(rightBottomFront, rightTopFront, Color.gray);
                    drawLine(rightTopFront, leftTopFront, Color.gray);

                    // Back side
                    drawLine(leftTopBack, leftBottomBack, Color.gray);
                    drawLine(leftBottomBack, rightBottomBack, Color.gray);
                    drawLine(rightBottomBack, rightTopBack, Color.gray);
                    drawLine(rightTopBack, leftTopBack, Color.gray);

                    // Top side
                    drawLine(leftTopBack, rightTopBack, Color.gray);
                    drawLine(rightTopBack, rightTopFront, Color.gray);
                    drawLine(rightTopFront, leftTopFront, Color.gray);
                    drawLine(leftTopFront, leftTopBack, Color.gray);

                    // Bottom side
                    drawLine(leftBottomBack, rightBottomBack, Color.gray);
                    drawLine(rightBottomBack, rightBottomFront, Color.gray);
                    drawLine(rightBottomFront, leftBottomFront, Color.gray);
                    drawLine(leftBottomFront, leftBottomBack, Color.gray);

                    // Left side
                    drawLine(leftBottomBack, leftBottomFront, Color.gray);
                    drawLine(leftBottomFront, leftTopFront, Color.gray);
                    drawLine(leftTopFront, leftTopBack, Color.gray);
                    drawLine(leftTopBack, leftBottomBack, Color.gray);

                    // Right side
                    drawLine(rightBottomBack, rightBottomFront, Color.gray);
                    drawLine(rightBottomFront, rightTopFront, Color.gray);
                    drawLine(rightTopFront, rightTopBack, Color.gray);
                    drawLine(rightTopBack, rightBottomBack, Color.gray);
                }
            }
        }
    }

    private void drawLine(Vector3 start, Vector3 end, Color c, float width = 0.03f, string name = "line")
    {
        GameObject myLine = new GameObject(name);
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();

        myLine.GetComponent<Renderer>().material.color = c;
        lr.material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
        lr.material.color = c;
        lr.SetWidth(width, width);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

}
