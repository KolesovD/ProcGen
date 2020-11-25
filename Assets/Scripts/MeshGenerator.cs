using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator {
    
    private int _height;
    private int _width;
    private readonly int NUMBER_OF_DOTS_IN_ONE_MESH = 240;

    public Mesh[,] meshArray;

    public MeshGenerator(int width, int height)
    {
        _height = height;
        _width = width;
    }

    public MeshGenerator(int dimension) : this(dimension, dimension) { }

    public void GenerateMesh(Vector3[] vertices)
    {
        Debug.Log(_height + " " + _width);
        int numberOfXMeshes = _width % NUMBER_OF_DOTS_IN_ONE_MESH == 0 ? _width / NUMBER_OF_DOTS_IN_ONE_MESH : _width / NUMBER_OF_DOTS_IN_ONE_MESH + 1;
        int numberOfYMeshes = _height % NUMBER_OF_DOTS_IN_ONE_MESH == 0 ? _height / NUMBER_OF_DOTS_IN_ONE_MESH : _height / NUMBER_OF_DOTS_IN_ONE_MESH + 1;
        meshArray = new Mesh[numberOfYMeshes, numberOfXMeshes];

        for (int yMesh = 0; yMesh < numberOfYMeshes; yMesh++)
        {
            for (int xMesh = 0; xMesh < numberOfXMeshes; xMesh++)
            {
                Mesh mesh = new Mesh();
                int triangleIndex = 0;

                int numberOfPointsX = Mathf.Min(_width - NUMBER_OF_DOTS_IN_ONE_MESH * xMesh + xMesh, NUMBER_OF_DOTS_IN_ONE_MESH);
                int numberOfPointsY = Mathf.Min(_height - NUMBER_OF_DOTS_IN_ONE_MESH * yMesh + yMesh, NUMBER_OF_DOTS_IN_ONE_MESH);
                Debug.Log(numberOfPointsY + " " + numberOfPointsX);

                int[] triangles = new int[(numberOfPointsY - 1) * (numberOfPointsX - 1) * 6];
                Vector3[] currentVertices = new Vector3[numberOfPointsY * numberOfPointsX];
                Vector2[] currentUVs = new Vector2[numberOfPointsY * numberOfPointsX];

                for (int y = 0; y < numberOfPointsY; y++)
                {
                    for (int x = 0; x < numberOfPointsX; x++)
                    {
                        int currentPoint = x + y * numberOfPointsX;
                        currentVertices[currentPoint] = vertices[(x + y * _width - xMesh) + NUMBER_OF_DOTS_IN_ONE_MESH * (xMesh + yMesh * _width) - yMesh * _width];
                        currentUVs[currentPoint] = new Vector2(currentVertices[currentPoint].x / 50f, currentVertices[currentPoint].z / 50f);

                        if (x != numberOfPointsX - 1 && y != numberOfPointsY - 1)
                        {
                            triangles[triangleIndex] = currentPoint;
                            triangles[triangleIndex + 1] = currentPoint + numberOfPointsX;
                            triangles[triangleIndex + 2] = currentPoint + numberOfPointsX + 1;

                            triangles[triangleIndex + 3] = currentPoint;
                            triangles[triangleIndex + 4] = currentPoint + numberOfPointsX + 1;
                            triangles[triangleIndex + 5] = currentPoint + 1;

                            triangleIndex += 6;
                        }
                    }
                }

                mesh.vertices = currentVertices;
                mesh.triangles = triangles;
                mesh.uv = currentUVs;
                mesh.RecalculateNormals();

                meshArray[yMesh, xMesh] = mesh;
            }
        }

        Debug.Log(meshArray.Length);
    }
}
