using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;

[CustomEditor(typeof(LowPolyTerrainObjects))]
public class LowPolyTerrainObjectsEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		LowPolyTerrainObjects objects = (LowPolyTerrainObjects)target;
		EditorGUI.BeginDisabledGroup(objects.ObjectPlacementMap == null);
		{
			if (GUILayout.Button("Generate Objects"))
			{
				var generator = new LowPolyTerrainObjectsGenerator(objects);
				generator.GenerateObjects();
				EditorSceneManager.MarkSceneDirty(objects.gameObject.scene);
			}
			if (GUILayout.Button("Clear Objects"))
			{
				objects.ClearObjects();
				EditorSceneManager.MarkSceneDirty(objects.gameObject.scene);
			}
		}
		EditorGUI.EndDisabledGroup();
	}
}
