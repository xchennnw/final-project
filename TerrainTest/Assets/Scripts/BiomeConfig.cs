using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Layer
{
    public int width;
    public List<GameObject> meshes;
    public Material material;

    public bool hasTree;   
    public GameObject tree;
    public int treeNumberPerGrid;
    public bool randomPosY;
    public Vector2 randomPosYRange;

    public bool hasStone;
    public int stoneNumberPerGrid;
    public GameObject stone;

    [HideInInspector] public int tilePoolID;
    [HideInInspector] public int treePoolID;
    [HideInInspector] public int stonePoolID;
}

[CreateAssetMenu(fileName = "New Biome Config", menuName = "Biome Config")]
public class BiomeConfig : ScriptableObject
{
    public bool blendNearMid;
    public float midNearRatio;
    public bool blendMidFar;
    public float farMidRatio;
    public Layer near;
    public Layer mid;
    public Layer far;
    public SkyColorScriptableObject skyConfig;
}


