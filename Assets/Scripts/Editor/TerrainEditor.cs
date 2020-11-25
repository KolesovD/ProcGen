using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainEditor : Editor
{
    private TerrainGenerator generator;

    public void OnEnable()
    {
        generator = (TerrainGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open external music file"))
        {
            generator.OpenMusicFile();
        }
        base.OnInspectorGUI();
        //EditorGUILayout.PropertyField(buildingsArray);
        if (GUILayout.Button("Build terrain"))
        {
            generator.ProcessMusicFile();
        }
        if (GUILayout.Button("Clear terrain"))
        {
            generator.ClearMap();
        }
    }
}
