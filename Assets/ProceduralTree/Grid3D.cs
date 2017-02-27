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

    public IEnumerator<int> GetEnumerator()
    {
        for (int i = 0; i < data.GetLength(0); i++)
            for (int j = 0; j < data.GetLength(0); j++)
                for (int k = 0; k < data.GetLength(0); k++)
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
