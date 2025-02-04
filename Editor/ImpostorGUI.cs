using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ImpostorEntity))]
public class ImpostorGUI : Editor
{


    public override void OnInspectorGUI(){
        DrawDefaultInspector();
        ImpostorEntity impostor = (ImpostorEntity) target;
        if(GUILayout.Button("GENERATE HEMIOCTAHEDRON IMPOSTOR")){
            impostor.Initialize();
            impostor.createImpostorHemiOctahedron();
        }
    }

    
}
