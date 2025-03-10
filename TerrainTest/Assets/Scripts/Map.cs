using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Map : MonoBehaviour
{
    public int layers = 3;
    public int length = 6;
    private const float size_adjust = 1.0f;
    private const float half_sqr3 = 0.8660254f;
    private float hexSize = size_adjust * half_sqr3;
    public int tilesPerBiome = 6;
    public List<BiomeConfig> biomes;

    private List<List<GameObject>> m_hexTiles;
    private int m_currBiome;
    private Vector2 m_headPos;
    private int m_tileCount = 0;

    private bool m_skyChanged = false;

    private void Start()
    {
        m_currBiome = 0;
        InitializeTilePools();
        InitializeObjPools();
        GenerateGrid();
        SkyboxController.Instance.UpdateSkyColor(biomes[m_currBiome].skyConfig);
        m_skyChanged = true;
    }

    private void Update()
    {
        if (!ObjectManager.Instance.initializationFinished) return;

        UpdateGrid();

        if(m_tileCount >= tilesPerBiome)
        {
            UpdateCurrBiome();
        }

        if(!m_skyChanged && m_tileCount == tilesPerBiome - 1)
        {
            SkyboxController.Instance.UpdateSkyColor(biomes[m_currBiome].skyConfig);
            m_skyChanged = true;
        }
    }

    private void UpdateCurrBiome()
    {
        m_tileCount = 0;
        m_currBiome++;
        if (m_currBiome >= biomes.Count)
        {
            m_currBiome = 0;
        }
        m_skyChanged = false;
    }

    public void InitializeObjPools()
    {
        for (int i = 0; i < biomes.Count; i++)
        {
            ObjectManager.Instance.InitializeObjectPoolOneLayer(biomes[i].far, tilesPerBiome);
            ObjectManager.Instance.InitializeObjectPoolOneLayer(biomes[i].mid, tilesPerBiome);
            ObjectManager.Instance.InitializeObjectPoolOneLayer(biomes[i].near, tilesPerBiome);
        }
        ObjectManager.Instance.initializationFinished = true;
    }

    public void InitializeTilePools()
    {
        for (int i = 0; i < biomes.Count; i++)
        {
            ObjectManager.Instance.InitializeTilePoolOneLayer(biomes[i].near, tilesPerBiome, size_adjust);
            ObjectManager.Instance.InitializeTilePoolOneLayer(biomes[i].mid, tilesPerBiome, size_adjust);
            ObjectManager.Instance.InitializeTilePoolOneLayer(biomes[i].far, tilesPerBiome, size_adjust);
        }
    }

    public void GenerateGrid()
    {
        m_hexTiles = new List<List<GameObject>>();
        m_headPos.x = -0.5f * (length - 1) * hexSize;
        m_headPos.y = 0.5f * (layers - 1) * hexSize * half_sqr3;

        int currX = 0;
        while(currX < length)
        {
            if(currX + tilesPerBiome < length)
            {
                for (int z = 0; z < layers; z++)
                {
                    m_hexTiles.Add(new List<GameObject>());
                    for (int x = currX; x < currX + tilesPerBiome; x++)
                    {
                        SpawnHexTile(z, x);
                    }
                }
                UpdateCurrBiome();
                currX += tilesPerBiome;
            }
            else
            {
                for (int z = 0; z < layers; z++)
                {
                    m_hexTiles.Add(new List<GameObject>());
                    for (int x = currX; x < length; x++)
                    {
                        SpawnHexTile(z, x);
                    }
                }
                m_tileCount += length - currX;
                currX = length;
            }       
        }     
    }

    public void SpawnHexTile(int z, int x)
    {
        float pos_z = m_headPos.y - z * hexSize * half_sqr3;
        float pos_x; 
        if (z % 2 == 0) pos_x = m_headPos.x + x * hexSize;
        else pos_x = m_headPos.x - 0.5f * hexSize + x * hexSize;

        Layer layer = GetBiomeLayer(z);
        GameObject spawnedTile = ObjectManager.Instance.GetTileFromPool(layer.tilePoolID);
        spawnedTile.SetActive(true);
        spawnedTile.transform.position = new Vector3(pos_x, 0, pos_z);
        spawnedTile.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0, 6) * 60);
        spawnedTile.transform.SetParent(this.transform, true);

        m_hexTiles[z].Add(spawnedTile);

        StartCoroutine(Wait(layer, spawnedTile.transform));
    }

    IEnumerator Wait(Layer layer, Transform tran)
    {
        yield return new WaitForSeconds(0.1f);
        SpawnObjectsForLayer(layer, tran);
    }

    public Layer GetBiomeLayer(int z)
    {
        // Far & far mid boundary
        if (z < biomes[m_currBiome].far.width - 1)
        {
            return biomes[m_currBiome].far;
        }
        if (z < biomes[m_currBiome].far.width)
        {
            if(biomes[m_currBiome].blendMidFar 
                && UnityEngine.Random.Range(0.0f, 1.0f) > biomes[m_currBiome].farMidRatio)
            {
                return biomes[m_currBiome].mid;
            }
            return biomes[m_currBiome].far;
        }

        // Mid & mid near boundary
        if (z < biomes[m_currBiome].far.width + biomes[m_currBiome].mid.width - 1)
        {
            return biomes[m_currBiome].mid;
        }
        if (z < biomes[m_currBiome].far.width + biomes[m_currBiome].mid.width)
        {
            if (biomes[m_currBiome].blendNearMid
                && UnityEngine.Random.Range(0.0f, 1.0f) > biomes[m_currBiome].midNearRatio)
            {
                return biomes[m_currBiome].near;
            }
            return biomes[m_currBiome].mid;
        }
 
        // Near
        return biomes[m_currBiome].near;         
    }

    public void UpdateGrid()
    {
        if(m_hexTiles[0][0].transform.position.x <= Train.Instance.GetLeftBound())
        {
            m_headPos.x = m_hexTiles[0][1].transform.position.x;
            GameObject tile;
            for (int z = 0; z < layers; z++)
            {
                tile = m_hexTiles[z][0];
                m_hexTiles[z].RemoveAt(0);

                //Disable objects on tile
                int childCnt = tile.transform.childCount;
                for (int i = childCnt - 1; i >= 0; i--)
                { 
                    Transform c = tile.transform.GetChild(i);
                    ObjectManager.Instance.BackToObjectPool(c);
                }

                ObjectManager.Instance.BackToTilePool(tile);
                SpawnHexTile(z, length - 1);
            }
            m_tileCount++;
        }
    }

    void SpawnObjectsForLayer(Layer layer, Transform parent)
    {
        if(layer.hasTree)
        {
            SpawnObjectsOnGrid(layer.treePoolID, layer.treeNumberPerGrid, parent, layer.randomPosY, layer.randomPosYRange);
        }
        if (layer.hasStone)
        {
            SpawnObjectsOnGrid(layer.stonePoolID, layer.stoneNumberPerGrid, parent, false, new Vector2(0, 0));
        }
    }

    void SpawnObjectsOnGrid(int poolID, int numberPerGrid, Transform parent, bool randomPosY, Vector2 randomPosYRange)
    {
        for (int i = 0; i < numberPerGrid; i++)
        {
            float randomAngle = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
            float spawnRadius = hexSize * half_sqr3 * 0.5f * UnityEngine.Random.Range(0f, 1f);
            float randomX = parent.position.x + spawnRadius * Mathf.Cos(randomAngle);
            float randomZ = parent.position.z + spawnRadius * Mathf.Sin(randomAngle);

            float terrainHeight = GetMeshTerrainHeight(new Vector3(randomX, 0f, randomZ));
            if (terrainHeight > 0.2) terrainHeight = 0;
            Vector3 spawnPosition = new Vector3(randomX, terrainHeight, randomZ);
            if (randomPosY)
            {
                spawnPosition.y += UnityEngine.Random.Range(randomPosYRange.x, randomPosYRange.y);
            }

            GameObject newObj = ObjectManager.Instance.GetObjectFromPool(poolID);
            if (!newObj) continue;
            newObj.SetActive(true);
            newObj.transform.position = spawnPosition;
            newObj.transform.Rotate(Vector3.up, UnityEngine.Random.Range(0, 360));
            newObj.transform.SetParent(parent, true);
        }
    }

    float GetMeshTerrainHeight(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out hit, Mathf.Infinity))
        {
            return hit.point.y;
        }
        else
        {
            return 0f;
        }
    }

}
