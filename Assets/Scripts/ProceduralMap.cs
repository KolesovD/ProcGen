using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public class ProceduralMap {

    public Vector3[] terrainCoordinates { get; private set; }
    public float maxX2DCoord { get; private set; }
    public float maxY2DCoord { get; private set; }
    public int terrainDimension { get; private set; }
    public int rotationDegrees { get; private set; }
    public List<BuildingSpawnOptions>[] spawnCoordinates { get; private set; }

    private int[] _frequenciesPeaksCount;
    private csDelaunay.Voronoi _vorDiagram;
    private List<Vector2f> _dots;
    private List<Vector3Int> _regionsStartPositions;
    private Building[] _buildingsProperties;
    private System.Random _pointsRG;

    public static float MOUNTAIN_TRESHOLD = 0.88f;
    public static float WATERLINIE = -0.75f;
    public static float COAST_TRESHOLD = 0.05f;

    public ProceduralMap(float[] terrainLength, int musicSamples, int[] frequenciesPeaksCount, Building[] buildingsProperties)
    {
        _frequenciesPeaksCount = frequenciesPeaksCount;
        _buildingsProperties = buildingsProperties;
        terrainDimension = terrainLength.Length / 2;

        spawnCoordinates = new List<BuildingSpawnOptions>[_buildingsProperties.Length];
        for (int i = 0; i < _buildingsProperties.Length; i++)
            spawnCoordinates[i] = new List<BuildingSpawnOptions>();

        PerlinNoiseGenerator png = new PerlinNoiseGenerator(terrainDimension, musicSamples, _frequenciesPeaksCount[121], _frequenciesPeaksCount[122]);
        float[,] heightMap = png.GeneratePerlinNoiseMap();

        /*********/
        /*for (int i = 0; i < heightMap.GetLength(0); i++)
        {
            for (int j = 0; j < heightMap.GetLength(1); j++)
            {
                int num = (int)(heightMap[i, j] / 0.2f);
                heightMap[i, j] = num * 0.2f;
            }
        }
        */
        /*********/

        terrainCoordinates = new Vector3[terrainDimension * terrainDimension];

        float yLength = 0;
        for (int y = 0; y < terrainDimension; y++)
        {
            float xLength = 0;
            for (int x = 0; x < terrainDimension; x++)
            {
                terrainCoordinates[y * terrainDimension + x] = new Vector3(xLength, heightMap[y, x], yLength);
                if (y == terrainDimension - 1 && x == terrainDimension - 1)
                {
                    maxX2DCoord = xLength;
                    maxY2DCoord = yLength;
                }
                xLength += terrainLength[x];
            }
            yLength += terrainLength[terrainDimension + y];
        }
    }

    public void CreateMap()
    {
        CreateAreasFromVoronoiDiagrams();
        CreatePointsForSpawning();
    }

    private void CreateAreasFromVoronoiDiagrams()
    {
        _dots = new List<Vector2f>();
        System.Random rg = new System.Random(_frequenciesPeaksCount[103]);
        int numberOfZones = (int)(Mathf.Sqrt(maxX2DCoord * maxY2DCoord) / 15f);
        for (int i = 0; i < numberOfZones; i++)
            _dots.Add(new Vector2f(rg.Next(1, (int)maxX2DCoord), rg.Next(1, (int)maxY2DCoord)));

        _vorDiagram = new csDelaunay.Voronoi(_dots, new Rectf(0, 0, maxX2DCoord, maxY2DCoord), 20);
        _dots = _vorDiagram.SiteCoords();
    }

    private void CreatePointsForSpawning()
    {
        Dictionary<Vector2f, RegionType> regionTypes = new Dictionary<Vector2f, RegionType>(_dots.Count);
        foreach (Vector2f dot in _dots)
            regionTypes.Add(dot, RegionType.flat);
        List<Vector3>[] sortedDotsInRegions = new List<Vector3>[_dots.Count];
        for (int i = 0; i < sortedDotsInRegions.Length; i++)
            sortedDotsInRegions[i] = new List<Vector3>();

        foreach (Vector3 terrainCoord in terrainCoordinates)
        {
            int currentArea = FindRegion(terrainCoord);
            sortedDotsInRegions[currentArea].Add(terrainCoord);
        }

        for (int i = 0; i < sortedDotsInRegions.Length; i++)
            sortedDotsInRegions[i].Sort(new VectorYComparer());

        for (int i = 0; i < sortedDotsInRegions.Length; i++)
        {
            if (sortedDotsInRegions[i][0].y <= WATERLINIE)
            {
                if (regionTypes[_dots[i]] != RegionType.water)
                    regionTypes[_dots[i]] = RegionType.water;
            }
            else if (sortedDotsInRegions[i][sortedDotsInRegions[i].Count - 1].y >= MOUNTAIN_TRESHOLD)
            {
                if (regionTypes[_dots[i]] != RegionType.mountain)
                    regionTypes[_dots[i]] = RegionType.mountain;
            }
        }

        int numberOfRegions = Enum.GetNames(typeof(RegionType)).Length;
        List<Vector2f>[] regions = new List<Vector2f>[numberOfRegions];
        for (int i = 0; i < numberOfRegions; i++)
            regions[i] = new List<Vector2f>();
        foreach (Vector2f dot in _dots)
        {
            regions[(int)regionTypes[dot]].Add(dot);
        }

        Vector2f startPosition = new Vector2f(maxX2DCoord / 2f, maxY2DCoord / 2f);
        if (regions[(int)RegionType.flat].Count != 0)
        {
            startPosition = FindNearest(startPosition, regions[(int)RegionType.flat]);
        }
        else if (regions[(int)RegionType.mountain].Count != 0)
        {
            startPosition = FindNearest(startPosition, regions[(int)RegionType.mountain]);
        }
        else startPosition = FindNearest(startPosition, regions[(int)RegionType.water]);

        System.Random rotationRandom = new System.Random(_frequenciesPeaksCount[102]);
        rotationDegrees = rotationRandom.Next(0, 360);
        float spawnCos = Mathf.Cos(Mathf.Deg2Rad * rotationDegrees);
        float spawnSin = Mathf.Sin(Mathf.Deg2Rad * rotationDegrees);

        int numberOfSteps = (int)(1.5f * Mathf.Max(maxX2DCoord, maxY2DCoord));

        SpawnPoint[,] spawnPoints = new SpawnPoint[2 * numberOfSteps + 1, 2 * numberOfSteps + 1];
        for (int i = 0; i < 4; i++)
        {
            int currentYNumberOfSteps = numberOfSteps;
            float startXPos = startPosition.x;
            float startYPos = startPosition.y;
            float currentYPos = startYPos;
            int currentYStep = numberOfSteps;
            for (int y = 0; y < currentYNumberOfSteps; y++)
            {
                switch (i)
                {
                    case 0:
                    case 1:
                        if (y == 0)
                        {
                            currentYNumberOfSteps++;
                        }
                        else
                        {
                            currentYPos += 1f;
                            currentYStep++;
                        }
                        break;
                    default:
                        currentYPos -= 1f;
                        currentYStep--;
                        break;
                }

                float currentXPos = startXPos;
                int currentXNumberOfSteps = numberOfSteps;
                int currentXStep = numberOfSteps;
                for (int x = 0; x < currentXNumberOfSteps; x++)
                {
                    switch (i)
                    {
                        case 0:
                        case 2:
                            if (x == 0)
                            {
                                currentXNumberOfSteps++;
                            }
                            else
                            {
                                currentXPos += 1f;
                                currentXStep++;
                            }
                            break;
                        default:
                            currentXPos -= 1f;
                            currentXStep--;
                            break;
                    }

                    float yRotatePosition = startYPos + (currentYPos - startYPos) * spawnCos + (currentXPos - startXPos) * spawnSin;
                    float xRotatePosition = startXPos + (currentXPos - startXPos) * spawnCos - (currentYPos - startYPos) * spawnSin;

                    if (xRotatePosition > 5f && xRotatePosition < (maxX2DCoord - 5f) && yRotatePosition > 5f && yRotatePosition < (maxY2DCoord - 5f))
                    {
                        Vector2f currentDot = new Vector2f(xRotatePosition, yRotatePosition);
                        float height = FindDotHeight(currentDot);
                        SpawnPointType currentType;
                        if (height >= MOUNTAIN_TRESHOLD)
                            currentType = SpawnPointType.mountain;
                        else if (height <= (WATERLINIE - COAST_TRESHOLD))
                            currentType = SpawnPointType.water;
                        else if (height <= (WATERLINIE + COAST_TRESHOLD))
                            currentType = SpawnPointType.waterBorderWall;
                        else currentType = SpawnPointType.vacant;

                        spawnPoints[currentYStep, currentXStep] = new SpawnPoint { pointPosition = new Vector3(xRotatePosition, height, yRotatePosition), type = currentType, region = FindRegion(new Vector2f(xRotatePosition, yRotatePosition)) };
                    }
                    else spawnPoints[currentYStep, currentXStep] = new SpawnPoint { pointPosition = Vector3.zero, type = SpawnPointType.none, region = -1 };
                }
            }
        }

        _regionsStartPositions = FormRegionsStartPositions(spawnPoints, numberOfSteps);

        _pointsRG = new System.Random(_frequenciesPeaksCount[101]);

        //Main Building
        for (int id = 0; id < _buildingsProperties.Length; id++)
        {
            if (_buildingsProperties[id].buildingType != BuildingTypes.mainBuilding)
                continue;
            else
            {
                SpawnPointType neededSPType = FindSpawnPointType(BuildingTypes.mainBuilding, _buildingsProperties[id].buildingRegionType);
                int xPos;
                int yPos;
                bool horisontal;
                if (FindStartingSpawnDot(spawnPoints, numberOfSteps, _buildingsProperties[id].length, _buildingsProperties[id].width, neededSPType, out xPos, out yPos, out horisontal))
                    PlaceSpawnPoint(spawnPoints, xPos, yPos, _buildingsProperties[id].length, _buildingsProperties[id].width, neededSPType, horisontal, id);
                break;
            }
        }

        //Building
        {
            List<ObjectToSpawn> unitObjectsToSpawn = new List<ObjectToSpawn>();
            List<ObjectToSpawn> littleObjectsToSpawn = new List<ObjectToSpawn>();
            List<ObjectToSpawn> mediumObjectsToSpawn = new List<ObjectToSpawn>();
            List<ObjectToSpawn> bigObjectsToSpawn = new List<ObjectToSpawn>();
            for (int id = 0; id < _buildingsProperties.Length; id++)
            {
                if (_buildingsProperties[id].buildingType != BuildingTypes.building)
                    continue;
                else
                {
                    if (_buildingsProperties[id].length * _buildingsProperties[id].width <= 1)
                        unitObjectsToSpawn.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));
                    else if (_buildingsProperties[id].length * _buildingsProperties[id].width <= 4)
                        littleObjectsToSpawn.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));
                    else if (_buildingsProperties[id].length * _buildingsProperties[id].width <= 12)
                        mediumObjectsToSpawn.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));
                    else bigObjectsToSpawn.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));
                }
            }

            int listType = 0;
            while (unitObjectsToSpawn.Count > 0 || littleObjectsToSpawn.Count > 0 || mediumObjectsToSpawn.Count > 0 || bigObjectsToSpawn.Count > 0)
            {
                List<ObjectToSpawn> nextList;
                int numberToSpawn = 0;
                switch (listType)
                {
                    case 0:
                        nextList = bigObjectsToSpawn;
                        numberToSpawn = 18;
                        break;
                    case 1:
                        nextList = mediumObjectsToSpawn;
                        numberToSpawn = 14;
                        break;
                    case 2:
                        nextList = littleObjectsToSpawn;
                        numberToSpawn = 10;
                        break;
                    default:
                        nextList = unitObjectsToSpawn;
                        numberToSpawn = 6;
                        break;
                }

                for (int step = 0; step < Mathf.Min(numberToSpawn, nextList.Count); step++)
                {
                    int i = _pointsRG.Next(0, nextList.Count);
                    SpawnPointType neededSPType = FindSpawnPointType(BuildingTypes.building, nextList[i].buildingToSpawn.buildingRegionType);

                    int xPos;
                    int yPos;
                    bool horisontal;
                    if (FindStartingSpawnDot(spawnPoints, numberOfSteps, nextList[i].buildingToSpawn.length, nextList[i].buildingToSpawn.width, neededSPType, out xPos, out yPos, out horisontal))
                    {
                        PlaceSpawnPoint(spawnPoints, xPos, yPos, nextList[i].buildingToSpawn.length, nextList[i].buildingToSpawn.width, neededSPType, horisontal, nextList[i].buildingID);

                        ObjectToSpawn obj2s = nextList[i];
                        obj2s.numberToSpawn--;
                        nextList[i] = obj2s;
                        if (nextList[i].numberToSpawn <= 0)
                            nextList.RemoveAt(i);
                    }
                    else
                    {
                        nextList.RemoveAt(i);
                    }
                }
                listType = (listType + 1) % 4;
            }
        }

        List<Vector2Int>[] spVacant = new List<Vector2Int>[numberOfRegions];
        List<Vector2Int>[] spWalls = new List<Vector2Int>[numberOfRegions];
        List<Vector2Int>[] spDecor = new List<Vector2Int>[numberOfRegions];
        for (int i = 0; i < numberOfRegions; i++)
        {
            spVacant[i] = new List<Vector2Int>();
            spWalls[i] = new List<Vector2Int>();
            spDecor[i] = new List<Vector2Int>();
        } 

        for (int y = 2; y < spawnPoints.GetLength(0) - 2; y++)
        {
            for (int x = 2; x < spawnPoints.GetLength(1) - 2; x++)
            {
                for (int region = 0; region < numberOfRegions; region++)
                {
                    if (spawnPoints[y, x].type == FindSpawnPointType(BuildingTypes.naturalDecoration, (RegionType)region))
                        spVacant[region].Add(new Vector2Int(x, y));
                    else if (spawnPoints[y, x].type == FindSpawnPointType(BuildingTypes.wall, (RegionType)region))
                    {
                        if (IsSpawnPointTypeVacant(spawnPoints[y + 1, x].type) && IsSpawnPointTypeVacant(spawnPoints[y + 2, x].type) ||
                            IsSpawnPointTypeVacant(spawnPoints[y - 1, x].type) && IsSpawnPointTypeVacant(spawnPoints[y - 2, x].type) ||
                            IsSpawnPointTypeVacant(spawnPoints[y, x + 1].type) && IsSpawnPointTypeVacant(spawnPoints[y, x + 2].type) ||
                            IsSpawnPointTypeVacant(spawnPoints[y, x - 1].type) && IsSpawnPointTypeVacant(spawnPoints[y, x - 2].type))
                            spWalls[region].Add(new Vector2Int(x, y));
                        else spDecor[region].Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        //Wall
        {
            List<ObjectToSpawn> wallList = new List<ObjectToSpawn>();
            for (int id = 0; id < _buildingsProperties.Length; id++)
                if (_buildingsProperties[id].buildingType != BuildingTypes.wall)
                    continue;
                else wallList.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));

            while (wallList.Count > 0)
            {
                int i = _pointsRG.Next(0, wallList.Count);
                SpawnPointType neededSPType = FindSpawnPointType(BuildingTypes.wall, wallList[i].buildingToSpawn.buildingRegionType);
                
                if (spWalls[(int)wallList[i].buildingToSpawn.buildingRegionType].Count > 0)
                {
                    PlaceSpawnPoint(spawnPoints, spWalls[(int)wallList[i].buildingToSpawn.buildingRegionType][0].x, spWalls[(int)wallList[i].buildingToSpawn.buildingRegionType][0].y, 1, 1, neededSPType, false, wallList[i].buildingID);
                    spWalls[(int)wallList[i].buildingToSpawn.buildingRegionType].RemoveAt(0);
                
                ObjectToSpawn obj2s = wallList[i];
                obj2s.numberToSpawn--;
                wallList[i] = obj2s;
                if (wallList[i].numberToSpawn <= 0)
                    wallList.RemoveAt(i);
                }
                else
                {
                    wallList.RemoveAt(i);
                }
            }
        }

        //Natural Decoration
        {
            List<ObjectToSpawn> decorList = new List<ObjectToSpawn>();
            for (int id = 0; id < _buildingsProperties.Length; id++)
                if (_buildingsProperties[id].buildingType != BuildingTypes.naturalDecoration)
                    continue;
                else decorList.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));

            while (decorList.Count > 0)
            {
                int i = _pointsRG.Next(0, decorList.Count);
                if (spVacant[(int)decorList[i].buildingToSpawn.buildingRegionType].Count > 0)
                {
                    int pointToSpawn = _pointsRG.Next(0, spVacant[(int)decorList[i].buildingToSpawn.buildingRegionType].Count);
                    PlaceSpawnPoint(spawnPoints, spVacant[(int)decorList[i].buildingToSpawn.buildingRegionType][pointToSpawn].x, spVacant[(int)decorList[i].buildingToSpawn.buildingRegionType][pointToSpawn].y, 1, 1, SpawnPointType.none, false, decorList[i].buildingID);
                    spVacant[(int)decorList[i].buildingToSpawn.buildingRegionType].RemoveAt(pointToSpawn);

                    ObjectToSpawn obj2s = decorList[i];
                    obj2s.numberToSpawn--;
                    decorList[i] = obj2s;
                    if (decorList[i].numberToSpawn <= 0)
                        decorList.RemoveAt(i);
                }
                else
                {
                    decorList.RemoveAt(i);
                }
            }
        }

        //Industrial Decoration
        {
            List<ObjectToSpawn> decorList = new List<ObjectToSpawn>();
            for (int id = 0; id < _buildingsProperties.Length; id++)
                if (_buildingsProperties[id].buildingType != BuildingTypes.industrialDecoration)
                    continue;
                else decorList.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));

            while (decorList.Count > 0)
            {
                int i = _pointsRG.Next(0, decorList.Count);
                if (spDecor[(int)decorList[i].buildingToSpawn.buildingRegionType].Count > 0)
                {
                    int pointToSpawn = _pointsRG.Next(0, spDecor[(int)decorList[i].buildingToSpawn.buildingRegionType].Count);
                    PlaceSpawnPoint(spawnPoints, spDecor[(int)decorList[i].buildingToSpawn.buildingRegionType][pointToSpawn].x, spDecor[(int)decorList[i].buildingToSpawn.buildingRegionType][pointToSpawn].y, 1, 1, SpawnPointType.none, false, decorList[i].buildingID);
                    spDecor[(int)decorList[i].buildingToSpawn.buildingRegionType].RemoveAt(pointToSpawn);

                    ObjectToSpawn obj2s = decorList[i];
                    obj2s.numberToSpawn--;
                    decorList[i] = obj2s;
                    if (decorList[i].numberToSpawn <= 0)
                        decorList.RemoveAt(i);
                }
                else
                {
                    decorList.RemoveAt(i);
                }
            }
        }

        //Road
        {
            List<ObjectToSpawn> roadList = new List<ObjectToSpawn>();
            for (int id = 0; id < _buildingsProperties.Length; id++)
                if (_buildingsProperties[id].buildingType != BuildingTypes.road)
                    continue;
                else roadList.Add(new ObjectToSpawn(_buildingsProperties[id], _buildingsProperties[id].numberBuildingsToSpawn, id));

            while (roadList.Count > 0)
            {
                int i = _pointsRG.Next(0, roadList.Count);
                SpawnPointType neededSPType = FindSpawnPointType(BuildingTypes.wall, roadList[i].buildingToSpawn.buildingRegionType);
                SpawnPointType placeSPType = FindSpawnPointType(BuildingTypes.road, roadList[i].buildingToSpawn.buildingRegionType);
                int xPos;
                int yPos;
                if (FindRoadSpawnDot(spawnPoints, numberOfSteps, neededSPType, placeSPType, out xPos, out yPos))
                {
                    PlaceSpawnPoint(spawnPoints, xPos, yPos, 1, 1, placeSPType, false, roadList[i].buildingID);

                    ObjectToSpawn obj2s = roadList[i];
                    obj2s.numberToSpawn--;
                    roadList[i] = obj2s;
                    if (roadList[i].numberToSpawn <= 0)
                        roadList.RemoveAt(i);
                }
                else
                {
                    roadList.RemoveAt(i);
                }
            }
        }


        /**************************************************/
        /*foreach (Vector3Int clod in _regionsStartPositions)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = new Vector3(spawnPoints[clod.y, clod.x].pointPosition.x, 1, spawnPoints[clod.y, clod.x].pointPosition.z);
            go.transform.localScale = new Vector3(1f, 1f, 1f);
            go.GetComponent<Renderer>().material.color = new Color(0.9f, 0.2f, 0.4f);
        }*/
        
        /*foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.type != SpawnPointType.none && sp.type != SpawnPointType.vacant)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = new Vector3(sp.pointPosition.x, sp.pointPosition.y, sp.pointPosition.z);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                if (sp.type == SpawnPointType.mountain)
                    go.GetComponent<Renderer>().material.color = new Color(0.9f, 0f, 0f);
                else if (sp.type == SpawnPointType.water)
                    go.GetComponent<Renderer>().material.color = new Color(0f, 0f, 0.9f);
                else if (sp.type == SpawnPointType.occupied)
                    go.GetComponent<Renderer>().material.color = new Color(0.9f, 0.8f, 0.9f);
                else if (sp.type == SpawnPointType.border)
                    go.GetComponent<Renderer>().material.color = new Color(0.6f, 0f, 0.1f);
                else if (sp.type == SpawnPointType.waterOccupied)
                    go.GetComponent<Renderer>().material.color = new Color(0f, 0.4f, 0.9f);
                else if (sp.type == SpawnPointType.waterBorder)
                    go.GetComponent<Renderer>().material.color = new Color(0.2f, 0.1f, 0.9f);
                else if (sp.type == SpawnPointType.borderWall)
                    go.GetComponent<Renderer>().material.color = new Color(1f, 0.2f, 0.4f);
                //else go.GetComponent<Renderer>().material.color = new Color(0.9f, 0.2f, 0.4f);
            }
        }*/

        /*for (int i = 0; i < spawnCoordinates.Length; i++)
        {
            foreach(Vector3 pnt in spawnCoordinates[i])
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.position = new Vector3(pnt.x, pnt.y, pnt.z);
                go.transform.localScale = new Vector3(1f, 1f, 1f);
                go.GetComponent<Renderer>().material.color = new Color(i / 30f, i / 30f, i / 30f);
            }
        }*/

        /*for (int i = 0; i < _dots.Count; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = new Vector3(_dots[i].x, 0, _dots[i].y);
            go.transform.localScale = new Vector3(10f, 10f, 10f);
            if (regionTypes[_dots[i]] == RegionType.water)
                go.GetComponent<Renderer>().material.color = new Color(0f, 0f, 1f);
            else if (regionTypes[_dots[i]] == RegionType.mountain)
                go.GetComponent<Renderer>().material.color = new Color(1f, 0f, 0f);
            else go.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.5f);
        }*/

        //List<csDelaunay.LineSegment> ls = _vorDiagram.VoronoiDiagram();
        //List<csDelaunay.LineSegment> ls2 = vorDiagram.VoronoiBoundarayForSite();
        //Debug.Log("LS2 length = " + ls2.Count);


        /*foreach (csDelaunay.LineSegment line in ls)
        {
            Debug.DrawLine(new Vector3(line.p0.x, 0, line.p0.y), new Vector3(line.p1.x, 0, line.p1.y), Color.red, 10000);
        }*/
    }

    private int FindRegion(Vector2f currentDot)
    {
        int currentArea = 0;
        float currentDistance = currentDot.DistanceSquare(_dots[0]);
        for (int i = 1; i < _dots.Count; i++)
        {
            float distance = currentDot.DistanceSquare(_dots[i]);
            if (distance < currentDistance)
            {
                currentDistance = distance;
                currentArea = i;
            }
        }
        return currentArea;
    }

    private int FindRegion(Vector3 currentDot)
    {
        Vector2f currentFixedDot = new Vector2f(currentDot.x, currentDot.z);
        return FindRegion(currentFixedDot);
    }

    private Vector2f FindNearest(Vector2f dot, List<Vector2f> listOfDots)
    {
        if (listOfDots.Count == 0)
            return Vector2f.zero;

        Vector2f nearestDot = listOfDots[0];
        float currentDistance = dot.DistanceSquare(listOfDots[0]);
        for (int i = 1; i < listOfDots.Count; i++)
        {
            float distance = dot.DistanceSquare(listOfDots[i]);
            if (distance < currentDistance)
            {
                currentDistance = distance;
                nearestDot = listOfDots[i];
            }
        }
        return nearestDot;
    }

    private Vector2f FindNearest(Vector3 dot, List<Vector2f> listOfDots)
    {
        Vector2f currentFixedDot = new Vector2f(dot.x, dot.z);
        return FindNearest(currentFixedDot, listOfDots);
    }

    private float FindDotHeight(Vector2f dot)
    {
        float currentDistance = (dot.x - terrainCoordinates[0].x) * (dot.x - terrainCoordinates[0].x) + (dot.y - terrainCoordinates[0].z) * (dot.y - terrainCoordinates[0].z);
        int xPoint = 0;
        int yPoint = 0;
        float currentDimensionDistance = currentDistance;
        for (int i = 1; i < terrainDimension; i++)
        {
            float distance = (dot.x - terrainCoordinates[i].x) * (dot.x - terrainCoordinates[i].x) + (dot.y - terrainCoordinates[i].z) * (dot.y - terrainCoordinates[i].z);
            if (distance < currentDimensionDistance)
                currentDimensionDistance = distance;
            else
            {
                xPoint = i - 1;
                break;
            }
        }
        currentDimensionDistance = currentDistance;
        for (int i = 1; i < terrainDimension; i++)
        {
            float distance = (dot.x - terrainCoordinates[i * terrainDimension].x) * (dot.x - terrainCoordinates[i * terrainDimension].x) + (dot.y - terrainCoordinates[i * terrainDimension].z) * (dot.y - terrainCoordinates[i * terrainDimension].z);
            if (distance < currentDimensionDistance)
                currentDimensionDistance = distance;
            else
            {
                yPoint = i - 1;
                break;
            }
        }
        int nearestDotIndex = yPoint * terrainDimension + xPoint;

        int nearVerticalDotIndex;
        int nearHorisontalDotIndex;

        if (dot.y - terrainCoordinates[nearestDotIndex].z >= 0)
            nearVerticalDotIndex = nearestDotIndex + terrainDimension;
        else nearVerticalDotIndex = nearestDotIndex - terrainDimension;

        if (dot.x - terrainCoordinates[nearestDotIndex].x >= 0)
            nearHorisontalDotIndex = nearestDotIndex + 1;
        else nearHorisontalDotIndex = nearestDotIndex - 1;

        float dhAB = terrainCoordinates[nearVerticalDotIndex].y - terrainCoordinates[nearestDotIndex].y;
        float dhBC = terrainCoordinates[nearHorisontalDotIndex].y - terrainCoordinates[nearestDotIndex].y;

        float dotHeight = dhAB * ((dot.x - terrainCoordinates[nearestDotIndex].x) / (terrainCoordinates[nearHorisontalDotIndex].x - terrainCoordinates[nearestDotIndex].x)) +
            dhBC * ((dot.y - terrainCoordinates[nearestDotIndex].z) / (terrainCoordinates[nearVerticalDotIndex].z - terrainCoordinates[nearestDotIndex].z)) + terrainCoordinates[nearestDotIndex].y;

        return dotHeight;
    }

    private List<Vector3Int> FormRegionsStartPositions(SpawnPoint[,] spawnPoints, int startPosition)
    {
        List<Vector3Int> listOfRegionsStartPositions = new List<Vector3Int>();
        Vector2Int[] regionsStartPositions = new Vector2Int[_dots.Count];
        int startRegion = FindRegion(spawnPoints[startPosition, startPosition].pointPosition);
        regionsStartPositions[startRegion] = new Vector2Int(startPosition, startPosition);
        listOfRegionsStartPositions.Add(new Vector3Int(regionsStartPositions[startRegion].x, regionsStartPositions[startRegion].y, startRegion));
        for (int cubeDimension = 0; cubeDimension < startPosition; cubeDimension++)
        {
            for (int i = -cubeDimension; i < cubeDimension + 1; i++)
            {
                for (int j = -cubeDimension; j < cubeDimension + 1; j++)
                {
                    if (i != cubeDimension && i != -cubeDimension && j != cubeDimension && j != -cubeDimension)
                        j = cubeDimension - 1;
                    else
                    {
                        int currentRegion = FindRegion(spawnPoints[startPosition + i, startPosition + j].pointPosition);
                        if (regionsStartPositions[currentRegion] == Vector2Int.zero && spawnPoints[startPosition + i, startPosition + j].pointPosition != Vector3.zero)
                        {
                            regionsStartPositions[currentRegion] = new Vector2Int(startPosition + j, startPosition + i);
                            listOfRegionsStartPositions.Add(new Vector3Int(regionsStartPositions[currentRegion].x, regionsStartPositions[currentRegion].y, currentRegion));
                            
                        }
                    }
                }
            }
        }
        return listOfRegionsStartPositions;
    }

    private int FindNextRegion(int currentRegion)
    {
        int nextRegion = -1;
        for (int i = 0; i < _regionsStartPositions.Count - 1; i++)
        {
            if (_regionsStartPositions[i].z == currentRegion)
            {
                nextRegion = i + 1;
                break;
            }
        }
        return nextRegion;
    }

    private bool FindStartingSpawnDot(SpawnPoint[,] spawnPoints, int startPosition, int length, int width, SpawnPointType requiredPointType, out int xPoint, out int yPoint, out bool horisontal)
    {
        bool isHorisontal = true;
        bool findPoint = false;
        int findedX = 0;
        int findedY = 0;
        int startXPos = startPosition;
        int startYPos = startPosition;
        int maxCubeDimensions = startPosition;
        int currentRegion = spawnPoints[startYPos, startXPos].region;
        bool inCurrentRegion = false;
        //Cube
        for (int cubeDimension = 0; cubeDimension < maxCubeDimensions; cubeDimension++)
        {
            for (int i = -cubeDimension; i < cubeDimension + 1; i++)
            {
                for (int j = -cubeDimension; j < cubeDimension + 1; j++)
                {
                    if (i != cubeDimension && i != -cubeDimension && j != cubeDimension && j != -cubeDimension)
                        j = cubeDimension - 1;
                    else
                    {
                        if (inCurrentRegion == false && spawnPoints[startYPos + i, startXPos + j].region == currentRegion)
                            inCurrentRegion = true;
                        if (spawnPoints[startYPos + i, startXPos + j].region == currentRegion)
                        {
                            bool isHorisintalPointCorrect = true;
                            for (int widthCheck = 0; widthCheck < width; widthCheck++)
                            {
                                for (int lengthCheck = 0; lengthCheck < length; lengthCheck++)
                                {
                                    if (spawnPoints[startYPos + i + widthCheck, startXPos + j + lengthCheck].type != requiredPointType)
                                    {
                                        isHorisintalPointCorrect = false;
                                        break;
                                    }
                                }
                                if (!isHorisintalPointCorrect) break;
                            }
                            if (isHorisintalPointCorrect)
                            {
                                findPoint = true;
                                findedX = startXPos + j;
                                break;
                            }

                            bool isVerticalPointCorrect = true;
                            for (int widthCheck = 0; widthCheck < width; widthCheck++)
                            {
                                for (int lengthCheck = 0; lengthCheck < length; lengthCheck++)
                                {
                                    if (spawnPoints[startYPos + i + lengthCheck, startXPos + j + widthCheck].type != requiredPointType)
                                    {
                                        isVerticalPointCorrect = false;
                                        break;
                                    }
                                }
                                if (!isVerticalPointCorrect) break;
                            }
                            if (isVerticalPointCorrect)
                            {
                                isHorisontal = false;
                                findPoint = true;
                                findedX = startXPos + j;
                                break;
                            }
                        }
                    }
                }
                if (findPoint)
                {
                    findedY = startYPos + i;
                    break;
                }
            }
            if (findPoint) break;
            if (inCurrentRegion)
            {
                inCurrentRegion = !inCurrentRegion;
            }
            else
            {
                int nextRegion = FindNextRegion(currentRegion);
                //Debug.Log(nextRegion + " " + currentRegion);
                if (nextRegion == (-1))
                    break;
                currentRegion = _regionsStartPositions[nextRegion].z;
                startXPos = _regionsStartPositions[nextRegion].x;
                startYPos = _regionsStartPositions[nextRegion].y;
                cubeDimension = -1;
            }
        }
        if (!findPoint)
        {
            xPoint = 0;
            yPoint = 0;
            horisontal = false;
            return false;
        }

        xPoint = findedX;
        yPoint = findedY;
        horisontal = isHorisontal;
        return true;
    }

    private bool FindRoadSpawnDot(SpawnPoint[,] spawnPoints, int startPosition, SpawnPointType requiredPointType, SpawnPointType changedPointType, out int xPoint, out int yPoint)
    {
        bool findPoint = false;
        int findedX = 0;
        int findedY = 0;
        for (int cubeDimension = 0; cubeDimension < startPosition; cubeDimension++)
        {
            for (int i = -cubeDimension; i < cubeDimension + 1; i++)
            {
                for (int j = -cubeDimension; j < cubeDimension + 1; j++)
                {
                    if (i != cubeDimension && i != -cubeDimension && j != cubeDimension && j != -cubeDimension)
                        j = cubeDimension - 1;
                    else if (spawnPoints[startPosition + i, startPosition + j].type == requiredPointType &&
                        (ComparePointType(spawnPoints[startPosition + i, startPosition + j - 1].type, requiredPointType, changedPointType) || ComparePointType(spawnPoints[startPosition + i, startPosition + j + 1].type, requiredPointType, changedPointType)) &&
                        (ComparePointType(spawnPoints[startPosition + i - 1, startPosition + j].type, requiredPointType, changedPointType) || ComparePointType(spawnPoints[startPosition + i + 1, startPosition + j].type, requiredPointType, changedPointType)))
                    {
                        findPoint = true;
                        findedX = startPosition + j;
                        break;
                    }
                }
                if (findPoint)
                {
                    findedY = startPosition + i;
                    break;
                }
            }
            if (findPoint) break;
        }

        if (!findPoint)
        {
            xPoint = 0;
            yPoint = 0;
            return false;
        }

        xPoint = findedX;
        yPoint = findedY;
        return true;
    }

    private SpawnPointType FindSpawnPointType(BuildingTypes buildingType, RegionType regionType)
    {
        if (buildingType == BuildingTypes.building || buildingType == BuildingTypes.mainBuilding || buildingType == BuildingTypes.naturalDecoration)
        {
            if (regionType == RegionType.flat)
                return SpawnPointType.vacant;
            else if (regionType == RegionType.mountain)
                return SpawnPointType.mountain;
            else return SpawnPointType.water;
        }
        else if (buildingType == BuildingTypes.wall)
        {
            if (regionType == RegionType.flat)
                return SpawnPointType.border;
            else if (regionType == RegionType.mountain)
                return SpawnPointType.mountainBorder;
            else return SpawnPointType.waterBorder;
        }
        else
        {
            if (regionType == RegionType.flat)
                return SpawnPointType.borderRoad;
            else if (regionType == RegionType.mountain)
                return SpawnPointType.mountainBorderRoad;
            else return SpawnPointType.waterBorderRoad;
        }
    }

    private void PlaceSpawnPoint(SpawnPoint[,] spawnPoints, int startXPosition, int startYPosition, int length, int width, SpawnPointType requiredPointType, bool horisontal, int buildingID)
    {
        if (!horisontal)
        {
            int temp = width;
            width = length;
            length = temp;
        }
        for (int y = 0; y < width; y++)
        {
            for (int x = 0; x < length; x++)
            {
                if (requiredPointType == SpawnPointType.vacant || requiredPointType == SpawnPointType.mountain || requiredPointType == SpawnPointType.water)
                {
                    SpawnPointType buildingSPType;
                    SpawnPointType borderSPType;
                    if (requiredPointType == SpawnPointType.vacant)
                    {
                        buildingSPType = SpawnPointType.occupied;
                        borderSPType = SpawnPointType.border;
                    }
                    else if (requiredPointType == SpawnPointType.mountain)
                    {
                        buildingSPType = SpawnPointType.mountainOccupied;
                        borderSPType = SpawnPointType.mountainBorder;
                    }
                    else
                    {
                        buildingSPType = SpawnPointType.waterOccupied;
                        borderSPType = SpawnPointType.waterBorder;
                    }
                    spawnPoints[startYPosition + y, startXPosition + x].type = buildingSPType;
                    if (x == 0)
                        if (spawnPoints[startYPosition + y, startXPosition + x - 1].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y, startXPosition + x - 1].type = borderSPType;
                    if (x == length - 1)
                        if (spawnPoints[startYPosition + y, startXPosition + x + 1].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y, startXPosition + x + 1].type = borderSPType;
                    if (y == 0)
                        if (spawnPoints[startYPosition + y - 1, startXPosition + x].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y - 1, startXPosition + x].type = borderSPType;
                    if (y == width - 1)
                        if (spawnPoints[startYPosition + y + 1, startXPosition + x].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y + 1, startXPosition + x].type = borderSPType;
                    if (x == 0 && y == 0)
                        if (spawnPoints[startYPosition + y - 1, startXPosition + x - 1].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y - 1, startXPosition + x - 1].type = borderSPType;
                    if (x == 0 && y == width - 1)
                        if (spawnPoints[startYPosition + y + 1, startXPosition + x - 1].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y + 1, startXPosition + x - 1].type = borderSPType;
                    if (x == length - 1 && y == 0)
                        if (spawnPoints[startYPosition + y - 1, startXPosition + x + 1].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y - 1, startXPosition + x + 1].type = borderSPType;
                    if (x == length - 1 && y == width - 1)
                        if (spawnPoints[startYPosition + y + 1, startXPosition + x + 1].type != SpawnPointType.none)
                            spawnPoints[startYPosition + y + 1, startXPosition + x + 1].type = borderSPType;
                }
                else if (requiredPointType == SpawnPointType.border)
                {
                    spawnPoints[startYPosition + y, startXPosition + x].type = SpawnPointType.borderWall;
                }
                else if (requiredPointType == SpawnPointType.mountainBorder)
                {
                    spawnPoints[startYPosition + y, startXPosition + x].type = SpawnPointType.mountainBorderWall;
                }
                else if (requiredPointType == SpawnPointType.waterBorder)
                {
                    spawnPoints[startYPosition + y, startXPosition + x].type = SpawnPointType.waterBorderWall;
                }
                else if (requiredPointType == SpawnPointType.borderRoad)
                {
                    spawnPoints[startYPosition + y, startXPosition + x].type = SpawnPointType.borderRoad;
                }
                else if (requiredPointType == SpawnPointType.mountainBorderRoad)
                {
                    spawnPoints[startYPosition + y, startXPosition + x].type = SpawnPointType.mountainBorderRoad;
                }
                else if (requiredPointType == SpawnPointType.waterBorderRoad)
                {
                    spawnPoints[startYPosition + y, startXPosition + x].type = SpawnPointType.waterBorderRoad;
                }
            }
        }
        if (length == 1 && width == 1)
        {
            Vector3 newPointPos = spawnPoints[startYPosition, startXPosition].pointPosition;
            if (newPointPos.y < WATERLINIE) newPointPos.y = WATERLINIE;
            if (_buildingsProperties[buildingID].buildingType == BuildingTypes.industrialDecoration || _buildingsProperties[buildingID].buildingType == BuildingTypes.naturalDecoration)
            {
                float offsetX = _pointsRG.Next(-300, 301) / 1000f;
                float offsetZ = _pointsRG.Next(-300, 301) / 1000f;
                newPointPos.x += offsetX;
                newPointPos.z += offsetZ;
            }
            if (_buildingsProperties[buildingID].buildingType == BuildingTypes.industrialDecoration && spawnPoints[startYPosition, startXPosition].pointPosition == Vector3.zero)
                Debug.Log(startYPosition + " " + startXPosition + " " + spawnPoints[startYPosition, startXPosition].region.ToString() + " " + spawnPoints[startYPosition, startXPosition].type.ToString());
            spawnCoordinates[buildingID].Add(new BuildingSpawnOptions { buildingCoordinates = newPointPos, horisontal = horisontal });
        }
        else
        {
            float vectorX, vectorY, vectorZ;
            vectorX = ((spawnPoints[startYPosition, startXPosition].pointPosition.x + spawnPoints[startYPosition + width - 1, startXPosition].pointPosition.x) / 2 +
                (spawnPoints[startYPosition, startXPosition + length - 1].pointPosition.x + spawnPoints[startYPosition + width - 1, startXPosition + length - 1].pointPosition.x) / 2) / 2;
            vectorY = ((spawnPoints[startYPosition, startXPosition].pointPosition.y + spawnPoints[startYPosition + width - 1, startXPosition].pointPosition.y) / 2 +
                (spawnPoints[startYPosition, startXPosition + length - 1].pointPosition.y + spawnPoints[startYPosition + width - 1, startXPosition + length - 1].pointPosition.y) / 2) / 2;
            vectorZ = ((spawnPoints[startYPosition, startXPosition].pointPosition.z + spawnPoints[startYPosition + width - 1, startXPosition].pointPosition.z) / 2 +
                (spawnPoints[startYPosition, startXPosition + length - 1].pointPosition.z + spawnPoints[startYPosition + width - 1, startXPosition + length - 1].pointPosition.z) / 2) / 2;
            spawnCoordinates[buildingID].Add(new BuildingSpawnOptions { buildingCoordinates = new Vector3(vectorX, vectorY < WATERLINIE ? WATERLINIE : vectorY, vectorZ), horisontal = horisontal });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSpawnPointTypeVacant(SpawnPointType spType)
    {
        if (spType == SpawnPointType.vacant || spType == SpawnPointType.mountain || spType == SpawnPointType.water || spType == SpawnPointType.waterBorderWall)
            return true;
        else return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ComparePointType(SpawnPointType spType, params SpawnPointType[] requiresTypes)
    {
        foreach (SpawnPointType neededSPType in requiresTypes)
            if (spType == neededSPType)
                return true;
        return false;
    }
}

public class VectorYComparer : IComparer<Vector3>
{
    public int Compare(Vector3 left, Vector3 right)
    {
        return left.y.CompareTo(right.y);
    }
}

internal struct SpawnPoint
{
    public Vector3 pointPosition;
    public SpawnPointType type;
    public int region;
}

internal enum SpawnPointType
{
    none, vacant, occupied, border, borderRoad, borderWall, water, waterOccupied, waterBorder, waterBorderRoad, waterBorderWall, mountain, mountainOccupied, mountainBorder, mountainBorderRoad, mountainBorderWall
}

internal struct ObjectToSpawn
{
    public Building buildingToSpawn;
    public int numberToSpawn;
    public int buildingID;

    public ObjectToSpawn(Building newBuildingToSpawn, int newNumberToSpawn, int newBuildingID)
    {
        buildingToSpawn = newBuildingToSpawn;
        numberToSpawn = newNumberToSpawn;
        buildingID = newBuildingID;
    }
}