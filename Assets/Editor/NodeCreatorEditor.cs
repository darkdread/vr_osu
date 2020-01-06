﻿using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(NodeCreator))]
public class NodeCreatorEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		NodeCreator myScript = (NodeCreator)target;
		if (GUILayout.Button("Generate Nodes")) {
			myScript.Generate();
		}

		if (GUILayout.Button("Generate Nodes Compute Shader Single Thread")) {
			myScript.GenerateWithComputeShaderSingleThreaded();
		}

		if (GUILayout.Button("Generate Nodes Compute Shader Multi Thread")) {
			myScript.GenerateWithComputeShaderMultiThreaded();
		}

        if (!myScript.debugOutline){
			if (GUILayout.Button("Show Debug Outline")) {
				myScript.DebugOutline();
			}
        } else {
			if (GUILayout.Button("Hide Debug Outline")) {
				myScript.DebugOutline();
			}
        }

		
	}

}