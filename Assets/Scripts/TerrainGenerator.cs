using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

public class TerrainGenerator : MonoBehaviour {

    public AudioClip musicClip;

    public Building[] buildings = new Building[50];
    public Material terrainMaterial;
    public bool randomTerrainColor = false;
    public Material waterMaterial;
    public bool randomWaterColor = false;

    public float heightScaling = 1f;

    private MusicProcessor _musicProcessor = new MusicProcessor();
    private GameObject[,] _meshObject;

    public void ProcessMusicFile()
    {
        if (_musicProcessor.isProceed)
        {
            ClearMap();

            int[] musicData = _musicProcessor.frequenciesPeaksCount;
            int buildingStep = 0;

            if (buildings.Length > 90)
            {
                Building[] tempB = new Building[90];
                System.Array.Copy(buildings, tempB, 90);
                buildings = tempB;
            }

            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i].buildingType == BuildingTypes.building)
                {
                    buildings[i].numberBuildingsToSpawn = musicData[buildingStep];
                    buildingStep++;
                }
                else if (buildings[i].buildingType == BuildingTypes.wall)
                {
                    buildings[i].numberBuildingsToSpawn = 10 * musicData[buildingStep];
                    buildingStep++;
                }
                else if (buildings[i].buildingType == BuildingTypes.road)
                {
                    buildings[i].numberBuildingsToSpawn = 10 * musicData[buildingStep];
                    buildingStep++;
                }
                else if (buildings[i].buildingType == BuildingTypes.industrialDecoration)
                {
                    buildings[i].numberBuildingsToSpawn = 10 * musicData[buildingStep];
                    buildingStep++;
                }
                else if (buildings[i].buildingType == BuildingTypes.naturalDecoration)
                {
                    buildings[i].numberBuildingsToSpawn = 10 * musicData[buildingStep];
                    buildingStep++;
                }
                else
                {
                    buildings[i].numberBuildingsToSpawn = musicData[125];
                }
            }

            GenerateTerrainFromMusic();
        }
        else if (musicClip != null)
        {

            if (musicClip.samples > 1000000)
            {
                CreateMusicData();
                ProcessMusicFile();
            }
            else
            {
                musicClip = null;
                Debug.Log("Загружен слишком маленький файл");
            }
        }
    }

    public void OpenMusicFile()
    {
        string clipPath = EditorUtility.OpenFilePanel("Выберите музыкальный файл", "", "OGG,MP3,WAV");
        
        if (clipPath.Length != 0)
        {
            WWW clipWWW = new WWW(clipPath);
            if (clipPath.Substring(clipPath.LastIndexOf('.') + 1).ToLower().Equals("mp3"))
            {
                musicClip = NAudioPlayer.FromMp3Data(clipWWW.bytes, clipPath.Substring(clipPath.LastIndexOf('/') + 1, clipPath.Length - clipPath.LastIndexOf('/') - 1));
                if (musicClip.samples > 1000000)
                    CreateMusicData();
                else
                {
                    musicClip = null;
                    Debug.Log("Загружен слишком маленький файл");
                }
            }
            else if (clipPath.Substring(clipPath.LastIndexOf('.') + 1).ToLower().Equals("ogg") || clipPath.Substring(clipPath.LastIndexOf('.') + 1).ToLower().Equals("wav"))
            {
                musicClip = clipWWW.GetAudioClip(false, false);
                if (musicClip.samples > 1000000)
                {
                    musicClip.name = clipPath.Substring(clipPath.LastIndexOf('/') + 1, clipPath.Length - clipPath.LastIndexOf('/') - 1);
                    CreateMusicData();
                }
                else
                {
                    musicClip = null;
                    Debug.Log("Загружен слишком маленький файл");
                }
            }
            else Debug.Log("Wrong type of file");
        }
        else
        {
            Debug.Log("Cant open file");
        }
    }

    public void ClearMap()
    {
        Transform[] childrens = GetComponentsInChildren<Transform>();
        for (int i = 1; i < childrens.Length; i++)
        {
            DestroyImmediate(childrens[i].gameObject);
        }
    }

    private void CreateMusicData()
    {
        _musicProcessor.musicClip = musicClip;
        _musicProcessor.CreateSpectrumData();
    }

    private void GenerateTerrainFromMusic()
    {
        ProceduralMap pMap = new ProceduralMap(_musicProcessor.GetLenghtFromBPM(), musicClip.samples, _musicProcessor.frequenciesPeaksCount, buildings);
        pMap.CreateMap();
        CreateMeshTerrain(pMap.terrainDimension, pMap.terrainCoordinates, pMap.maxX2DCoord, pMap.maxY2DCoord);

        SpawnBuildings(pMap.spawnCoordinates, pMap.rotationDegrees);
    }

    private void CreateMeshTerrain(int dimension, Vector3[] squareTerrainVertices, float maxX, float maxZ)
    {
        if (_meshObject != null)
            for (int i = 0; i < _meshObject.GetLength(0); i++)
            {
                for (int j = 0; j < _meshObject.GetLength(1); j++)
                {
                    DestroyImmediate(_meshObject[i, j].gameObject);
                }
            }

        MeshGenerator mg = new MeshGenerator(dimension);
        mg.GenerateMesh(squareTerrainVertices);

        _meshObject = new GameObject[mg.meshArray.GetLength(0), mg.meshArray.GetLength(1)];

        Color32 terrainColor = new Color32();
        if (randomTerrainColor)
        {
            System.Random terrainColorGenerator = new System.Random(_musicProcessor.frequenciesPeaksCount[106]);
            int fullChannel = terrainColorGenerator.Next(0, 3);
            int[] otherChannelsValue = new int[] { terrainColorGenerator.Next(0, 256), terrainColorGenerator.Next(0, 256) };
            byte[] randTerrainColor = new byte[3];
            randTerrainColor[fullChannel] = 255;
            int j = 0;
            for (int i = 0; i < 3; i++)
            {
                if (i == fullChannel)
                    continue;
                randTerrainColor[i] = (byte)otherChannelsValue[j];
                j++;
            }
            terrainColor = new Color32(randTerrainColor[0], randTerrainColor[1], randTerrainColor[2], 255);
        }

        for (int i = 0; i < _meshObject.GetLength(0); i++)
        {
            for (int j = 0; j < _meshObject.GetLength(1); j++)
            {
                _meshObject[i, j] = new GameObject("MeshTerrain_" + i + "x" + j);
                Vector3 newScale = _meshObject[i, j].transform.localScale;
                newScale.y = newScale.y * heightScaling;
                _meshObject[i, j].transform.localScale = newScale;
                _meshObject[i, j].transform.SetParent(transform);
                _meshObject[i, j].AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
                _meshObject[i, j].GetComponent<Renderer>().material = terrainMaterial;
                if (randomTerrainColor) _meshObject[i, j].GetComponent<Renderer>().material.color = terrainColor;
                MeshFilter meshFilter = _meshObject[i, j].AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mg.meshArray[i, j];
            }
        }

        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "Water";
        waterPlane.transform.SetParent(transform);
        waterPlane.transform.position = new Vector3(maxX / 2f, ProceduralMap.WATERLINIE * heightScaling, maxZ / 2f);
        waterPlane.transform.localScale = new Vector3(maxX / 8f, 1, maxZ / 8f);
        waterPlane.GetComponent<Renderer>().material = waterMaterial;
        if (randomWaterColor)
        {
            System.Random waterColorGenerator = new System.Random(_musicProcessor.frequenciesPeaksCount[107]);
            int fullChannel = waterColorGenerator.Next(0, 3);
            int zeroChannel = waterColorGenerator.Next(0, 10) < 5 ? (fullChannel - 1) % 3 : (fullChannel + 1) % 3;
            int thirdChannelvalue = waterColorGenerator.Next(0, 256);
            byte[] waterColor = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                if (i == fullChannel)
                    waterColor[i] = 255;
                else if (i == zeroChannel)
                    waterColor[i] = 0;
                else waterColor[i] = (byte)thirdChannelvalue;
            }
            waterPlane.GetComponent<Renderer>().material.color = new Color32(waterColor[0], waterColor[1], waterColor[2], 255);
        }
    }

    private void SpawnBuildings(List<BuildingSpawnOptions>[] spawnCoordinates, int rotationDegrees)
    {
        for (int i = 0; i < buildings.Length; i++)
        {
            while (spawnCoordinates[i].Count != 0)
            {
                GameObject nextBuilding = Instantiate(buildings[i].buildingPrefab) as GameObject;
                nextBuilding.name = buildings[i].name;
                nextBuilding.transform.SetParent(transform);
                Vector3 pointPosition = spawnCoordinates[i][0].buildingCoordinates;
                pointPosition.y = pointPosition.y * heightScaling;
                nextBuilding.transform.position = pointPosition;
                nextBuilding.transform.rotation = Quaternion.Euler(0, spawnCoordinates[i][0].horisontal ? -rotationDegrees :  -(rotationDegrees + 90), 0);
                spawnCoordinates[i].RemoveAt(0);
            }
        }
    }
}

public enum BuildingTypes
{
    building, industrialDecoration, naturalDecoration, road, wall, mainBuilding
}

public enum RegionType
{
    flat, water, mountain
}

[System.Serializable]
public struct Building
{
    public string name;
    public GameObject buildingPrefab;
    public int length;
    public int width;
    public BuildingTypes buildingType;
    public RegionType buildingRegionType;
    [HideInInspector]
    public int numberBuildingsToSpawn;
}

public struct BuildingSpawnOptions
{
    public Vector3 buildingCoordinates;
    public bool horisontal;
}