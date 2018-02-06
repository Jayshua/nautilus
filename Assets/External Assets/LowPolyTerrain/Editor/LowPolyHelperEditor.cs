using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LowPolyHelper))]
public class LowPolyHelperEditor : Editor
{
	public override void OnInspectorGUI()
	{
		LowPolyHelper helper = (LowPolyHelper)target;
		EditorGUILayout.HelpBox("Click on ApplyChanges to Update the Low Poly Terrain", MessageType.Info);
		if (GUILayout.Button("Apply Changes"))
		{
			// Add a terrain helper component, so the user can click on "finished"
			var sourceTerrain = helper.Terrain;

			var data = helper.GetComponent<Terrain>().terrainData;
			Vector3 sizes = data.size;
			sourceTerrain.TerrainSize = Mathf.RoundToInt(sizes.x);
			sourceTerrain.TerrainHeight = Mathf.RoundToInt(sizes.y);
			sourceTerrain.BaseResolution = Mathf.RoundToInt(sizes.x / data.heightmapResolution);

			var helperGO = helper.gameObject;
			helperGO.SetActive(false);
			
			sourceTerrain.gameObject.SetActive(true);
			Selection.activeObject = sourceTerrain.gameObject;
			GameObject.DestroyImmediate(helper);

			// Disable the low poly terrain and enable unity terrain
			LowPolyTerrainGenerator generator = new LowPolyTerrainGenerator(sourceTerrain);
			generator.GenerateTerrain();

			EditorGUIUtility.ExitGUI();
		}
	}
}
