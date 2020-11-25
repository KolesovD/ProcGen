using UnityEngine;

public class PerlinNoiseGenerator {

    private int dimension;
    private float noiseScale = 80;
    private int seed = 32;
    private int octaves = 20;
    private float persistance = 0.5f;
    private float lacunarity = 2f;
    private float offsetX = 0;
    private float offsetY = 0;

    private Vector2[] octaveOffset;
    private float[,] noiseMap;

    public PerlinNoiseGenerator() { }

    public PerlinNoiseGenerator(int newDimension, int newSeed, float newOffsetX, float newOffsetY)
    {
        SetParameters(newDimension, newSeed, newOffsetX, newOffsetY);
    }

    public float[,] GeneratePerlinNoiseMap(int newDimension, int newSeed, float newOffsetX, float newOffsetY)
    {
        SetParameters(newDimension, newSeed, newOffsetX, newOffsetY);
        return GeneratePerlinNoiseMap();
    }

    public float[,] GeneratePerlinNoiseMap()
    {
        BuildNoiseMap();
        return noiseMap;
    }

    public void SetParameters(int newDimension, int newSeed, float newOffsetX, float newOffsetY)
    {
        dimension = newDimension;
        seed = newSeed;
        offsetX = newOffsetX;
        offsetY = newOffsetY;
    }

    public float[,] GetLastNoiseMap()
    {
        return noiseMap;
    }

    public void BuildNoiseMap()
    {
        noiseMap = new float[dimension, dimension];

        octaveOffset = new Vector2[octaves];
        System.Random randomGenerator = new System.Random(seed);

        for (int i = 0; i < octaves; i++)
        {
            octaveOffset[i] = new Vector2
            {
                x = randomGenerator.Next(-10000, 10000) + offsetX,
                y = randomGenerator.Next(-10000, 10000) + offsetY
            };
        }

        if (noiseScale < 0.001f) noiseScale = 0.01f;

        float halfDimension = dimension / 2f;

        for (int y = 0; y < dimension; y++)
        {
            for (int x = 0; x < dimension; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float perlinValue = 0f;

                for (int oct = 0; oct < octaves; oct++)
                {
                    float xCoord = (x - halfDimension) / noiseScale * frequency + octaveOffset[oct].x;
                    float yCoord = (y - halfDimension) / noiseScale * frequency + octaveOffset[oct].y;

                    float currentPerlin = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;
                    perlinValue += currentPerlin * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseMap[y, x] = Mathf.Clamp(perlinValue, -1f, 1f);
            }
        }
    }
}
