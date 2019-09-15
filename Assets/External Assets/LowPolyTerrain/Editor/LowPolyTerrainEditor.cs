using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;

[CustomEditor(typeof(LowPolyTerrain))]
public class LowPolyTerrainEditor : Editor
{
	static GUIStyle _Line;

	[MenuItem("GameObject/Low Poly Terrain", false, 10)]
	static void CreateLowPolyTerrain()
	{
		var terrainObj = new GameObject();
		terrainObj.name = "Terrain";
		terrainObj.transform.position = Vector3.zero;
		terrainObj.transform.rotation = Quaternion.identity;
		terrainObj.transform.localScale = Vector3.one;
		terrainObj.AddComponent<LowPolyTerrain>();
	}

	public override void OnInspectorGUI()
	{
		if (_Line == null)
		{
			_Line = new GUIStyle("box");
			_Line.border.top = _Line.border.bottom = 1;
			_Line.margin.top = 1;
			_Line.margin.bottom = 5;
			_Line.padding.top = _Line.padding.bottom = 1;
			_Line.padding.top = _Line.padding.bottom = 1;
		}

		LowPolyTerrain terrain = (LowPolyTerrain)target;
		var terrainObj = new SerializedObject(terrain);
		var SourceHeightMapType = terrainObj.FindProperty("SourceHeightMapType");
		var SourceHeightMap = terrainObj.FindProperty("SourceHeightMap");
		var SourceRawHeightMapFile = terrainObj.FindProperty("SourceRawHeightMapFile");
		var RawHeightMapSize = terrainObj.FindProperty("RawHeightMapSize");
		var RawHeightMapOrder = terrainObj.FindProperty("RawHeightMapOrder");
		var SourceTerrain = terrainObj.FindProperty("SourceTerrain");
		var SourceColorMap = terrainObj.FindProperty("SourceColorMap");
		var TerrainMaterial = terrainObj.FindProperty("TerrainMaterial");
		var TerrainAlphaMaterial = terrainObj.FindProperty("TerrainAlphaMaterial");
		var TerrainSize = terrainObj.FindProperty("TerrainSize");
		var TerrainHeight = terrainObj.FindProperty("TerrainHeight");
		var ChunkSize = terrainObj.FindProperty("ChunkSize");
		var BaseResolution = terrainObj.FindProperty("BaseResolution");
		var RandomYOffset = terrainObj.FindProperty("RandomYOffset");
		var RandomXZOffset = terrainObj.FindProperty("RandomXZOffset");
		var UniformTriangles = terrainObj.FindProperty("UniformTriangles");
		var LODLevels = terrainObj.FindProperty("LODLevels");
		var LODDistances = terrainObj.FindProperty("_Distances");
		var LODTransitionTime = terrainObj.FindProperty("LODTransitionTime");
		var FlipFlopPercent = terrainObj.FindProperty("FlipFlopPercent");
		var HideChunksInHierarchy = terrainObj.FindProperty("HideChunksInHierarchy");
		var GenerateVertColors = terrainObj.FindProperty("GenerateVertColors");
		var CastShadows = terrainObj.FindProperty("CastShadows");

        EditorGUILayout.Separator();
		GUILayout.Label("Generation Settings");
		GUILayout.Box(GUIContent.none, _Line, GUILayout.ExpandWidth(true), GUILayout.Height(1f));

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(SourceHeightMapType);
		if (EditorGUI.EndChangeCheck())
		{
			// Clear the file path
			SourceRawHeightMapFile.stringValue = "";
		}

		bool canGenerate = true;
		if (SourceHeightMapType.enumValueIndex == (int)LowPolyTerrain.HeightmapType.Bitmap)
		{
			EditorGUILayout.PropertyField(SourceHeightMap);
			canGenerate = SourceHeightMap.objectReferenceValue != null;
		}
		else if (SourceHeightMapType.enumValueIndex == (int)LowPolyTerrain.HeightmapType.Raw16 || SourceHeightMapType.enumValueIndex == (int)LowPolyTerrain.HeightmapType.Raw32)
		{
			EditorGUILayout.BeginHorizontal();
			if (SourceHeightMapType.enumValueIndex == (int)LowPolyTerrain.HeightmapType.Raw16)
			{
				EditorGUILayout.TextField("Raw-16 Heightmap", System.IO.Path.GetFileName(SourceRawHeightMapFile.stringValue));
				if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(45.0f)))
				{
					SourceRawHeightMapFile.stringValue = EditorUtility.OpenFilePanelWithFilters("Select Raw file", SourceRawHeightMapFile.stringValue, new string[] { "16-bit Raw", "r16,raw", "Allfiles", "*" });

					// Try to determine the map size
					canGenerate = System.IO.File.Exists(SourceRawHeightMapFile.stringValue);
					if (canGenerate)
					{
						var info = new System.IO.FileInfo(SourceRawHeightMapFile.stringValue);
						RawHeightMapSize.intValue = Mathf.RoundToInt(Mathf.Sqrt(info.Length / sizeof(System.UInt16)));
					}
				}
				canGenerate = System.IO.File.Exists(SourceRawHeightMapFile.stringValue);
			}
			else
			{
				EditorGUILayout.TextField("Raw-32 Heightmap", System.IO.Path.GetFileName(SourceRawHeightMapFile.stringValue));
				if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(45.0f)))
				{
					SourceRawHeightMapFile.stringValue = EditorUtility.OpenFilePanelWithFilters("Select Raw file", SourceRawHeightMapFile.stringValue, new string[] { "32-bit Raw", "r32", "Allfiles", "*" });

					// Try to determine the map size
					canGenerate = System.IO.File.Exists(SourceRawHeightMapFile.stringValue);
					if (canGenerate)
					{
						var info = new System.IO.FileInfo(SourceRawHeightMapFile.stringValue);
						RawHeightMapSize.intValue = Mathf.RoundToInt(Mathf.Sqrt(info.Length / sizeof(System.Single)));
					}
				}
				canGenerate = System.IO.File.Exists(SourceRawHeightMapFile.stringValue);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Raw Map Size");
			RawHeightMapSize.intValue = EditorGUILayout.IntField(RawHeightMapSize.intValue, GUILayout.MinWidth(50.0f));
			GUILayout.Label("Byte Order");
			RawHeightMapOrder.enumValueIndex = (int)(LowPolyTerrain.ByteOrder)EditorGUILayout.EnumPopup((LowPolyTerrain.ByteOrder)RawHeightMapOrder.enumValueIndex, GUILayout.Width(80.0f));
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(SourceTerrain);
			if (EditorGUI.EndChangeCheck())
			{
				if (SourceTerrain.objectReferenceValue != null)
				{
					var sourceTerrain = SourceTerrain.objectReferenceValue as Terrain;
					Vector3 sizes = sourceTerrain.terrainData.size;
					TerrainSize.intValue = Mathf.RoundToInt(sizes.x);
					TerrainHeight.intValue = Mathf.RoundToInt(sizes.y);
					BaseResolution.intValue = Mathf.RoundToInt(sizes.x / sourceTerrain.terrainData.heightmapResolution);
				}
			}
			EditorGUI.BeginDisabledGroup(SourceTerrain.objectReferenceValue == null);
			if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(45.0f)))
			{
				// Add a terrain helper component, so the user can click on "finished"
				var sourceTerrain = SourceTerrain.objectReferenceValue as Terrain;
				var helper = sourceTerrain.GetComponent<LowPolyHelper>();
				if (helper == null)
				{
					helper = sourceTerrain.gameObject.AddComponent<LowPolyHelper>();
					helper.Terrain = terrain;
				}

				// Disable the low poly terrain and enable unity terrain
				Selection.activeObject = sourceTerrain.gameObject;
				terrain.gameObject.SetActive(false);
				sourceTerrain.gameObject.SetActive(true);
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.HelpBox("Terrain Size, Height and Base Resolution will be derived from the Unity Terrain Settings", MessageType.Info);

			canGenerate = SourceTerrain.objectReferenceValue != null;
		}

		if (SourceHeightMapType.enumValueIndex != (int)LowPolyTerrain.HeightmapType.Terrain)
		{
			EditorGUILayout.PropertyField(GenerateVertColors);
			if (GenerateVertColors.boolValue)
			{
				EditorGUILayout.PropertyField(SourceColorMap);
			}

			EditorGUILayout.PropertyField(TerrainSize);
			EditorGUILayout.PropertyField(TerrainHeight);
		}
		else
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(TerrainSize);
			EditorGUILayout.PropertyField(TerrainHeight);
			EditorGUI.EndDisabledGroup();
		}

		int chunkCount = -1;
		EditorGUILayout.PropertyField(ChunkSize);
		if (ChunkSize.intValue == 0)
		{
			EditorGUILayout.HelpBox("Chunk Size of 0 is invalid", MessageType.Warning);
			canGenerate = false;
		}
		else if (ChunkSize.intValue > TerrainSize.intValue)
		{
			EditorGUILayout.HelpBox("Chunk Size can not be larger than terrain size", MessageType.Warning);
			canGenerate = false;
		}
		else if (ChunkSize.intValue != 0)
		{
			if (TerrainSize.intValue % ChunkSize.intValue != 0)
			{
				EditorGUILayout.HelpBox("Chunk Size isn't a divisor of Terrain Size", MessageType.Warning);
				canGenerate = false;
			}
			else
			{
				chunkCount = TerrainSize.intValue / ChunkSize.intValue;
				chunkCount *= chunkCount;

				EditorGUI.BeginDisabledGroup(true);
				{
					EditorGUILayout.IntField("Chunk Count", chunkCount);
				}
				EditorGUI.EndDisabledGroup();

				if (chunkCount > 4096)
				{
					EditorGUILayout.HelpBox("Chunk Size of " + ChunkSize.intValue + " with terrain size of  " + TerrainSize.intValue + " would generate too many chunk objects (>" + chunkCount + ") and impact performance. Try to stay under a total of 4096 chunks.", MessageType.Warning);
					canGenerate = false;
				}
			}
		}

		int meshVertCount = -1;
		int colliderResolution = -1;
		if (SourceHeightMapType.enumValueIndex != (int)LowPolyTerrain.HeightmapType.Terrain)
		{
			EditorGUILayout.PropertyField(BaseResolution);
		}
		else
		{
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.PropertyField(BaseResolution);
			EditorGUI.EndDisabledGroup();
		}
		if (BaseResolution.intValue == 0)
		{
			EditorGUILayout.HelpBox("Base Resolution of 0 is invalid", MessageType.Warning);
			canGenerate = false;
		}
		else if (BaseResolution.intValue > ChunkSize.intValue)
		{
			EditorGUILayout.HelpBox("Base Resolution can not be greater than the Chunk Size", MessageType.Warning);
			canGenerate = false;
		}
		else if (BaseResolution.intValue != 0)
		{
			if (ChunkSize.intValue % BaseResolution.intValue != 0)
			{
				EditorGUILayout.HelpBox("Base Resolution (size of smallest quad) isn't a divisor of Chunk Size", MessageType.Warning);
				canGenerate = false;
			}
			else
			{
				meshVertCount = (ChunkSize.intValue + 2) / BaseResolution.intValue;
				meshVertCount *= meshVertCount * 6;

				colliderResolution = TerrainSize.intValue / BaseResolution.intValue + 1;

				EditorGUI.BeginDisabledGroup(true);
				{
					EditorGUILayout.IntField("Collider Resolution", colliderResolution);
					EditorGUILayout.IntField("Highest Mesh Vert Count", meshVertCount);
				}
				EditorGUI.EndDisabledGroup();

				if (meshVertCount > 65000)
				{
					EditorGUILayout.HelpBox("Chunk Size of " + ChunkSize.intValue + " with base resolution of  " + BaseResolution.intValue + " would generate too many verts (>" + meshVertCount + "). The limit is 65000 verts per mesh.", MessageType.Warning);
					canGenerate = false;
				}

				//if (!IsPowerOfTwoPlusOne(colliderResolution))
				//{
				//	EditorGUILayout.HelpBox("Collider Resolution must be in the form (power-of-two + 1)\n(ColliderResolution = (TerrainSize/BaseResolution) + 1)", MessageType.Warning);
				//	canGenerate = false;
				//}
			}
		}

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(LODLevels);
		if (EditorGUI.EndChangeCheck())
		{
			// Update the distances on the terrain.
			terrainObj.ApplyModifiedProperties();
			terrain.UpdateDistances();
		}
		int resolution = BaseResolution.intValue;
		bool LODLevelsOk = true;
		for (int lod = 0; LODLevelsOk && lod < terrain.LODLevels; ++lod)
		{
			LODLevelsOk = resolution != 0 && ChunkSize.intValue % resolution == 0;
			if (LODLevelsOk)
			{
				resolution *= 2;
			}
		}
		if (!LODLevelsOk)
		{
			EditorGUILayout.HelpBox("Chunk Size of " + ChunkSize.intValue + " can't accomodate " + LODLevels.intValue + " LOD Levels with a base resolution of " + BaseResolution.intValue, MessageType.Warning);
			canGenerate = false;
		}

		EditorGUILayout.PropertyField(RandomYOffset);
		EditorGUILayout.PropertyField(UniformTriangles);
		if (!UniformTriangles.boolValue)
		{
			EditorGUILayout.PropertyField(RandomXZOffset);
			EditorGUILayout.HelpBox("Non uniform triangles and XZ Offset will make your terrain look better but cause the collision geometry to be slightly off from render geometry", MessageType.Info);
		}
		EditorGUILayout.PropertyField(HideChunksInHierarchy);

		int lodVertCount = chunkCount * meshVertCount;
		int totalVertCount = lodVertCount;
		for (int i = 1; i < LODLevels.intValue; ++i)
		{
			lodVertCount /= 4;
			totalVertCount += lodVertCount;
		}

		EditorGUI.BeginDisabledGroup(true);
		{
			EditorGUILayout.IntField("Total Vert Count", totalVertCount);
		}
		EditorGUI.EndDisabledGroup();
		if (totalVertCount > 10000000)
		{
			EditorGUILayout.HelpBox("Total Vert Count is High (>10,000,000), it may take a while to generate", MessageType.Info);
		}
		EditorGUI.BeginDisabledGroup(true);
		{
			var generator = new LowPolyTerrainGenerator(terrain);
			EditorGUILayout.ObjectField("Terrain Data Asset", generator.GetFirstMesh(), typeof(Object), false);
		}
		EditorGUI.EndDisabledGroup();

		EditorGUI.BeginDisabledGroup(!canGenerate);
		{
			if (GUILayout.Button("Generate Meshes"))
			{
				terrainObj.ApplyModifiedProperties();
				var generator = new LowPolyTerrainGenerator(terrain);
				generator.GenerateTerrain();
				EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
			}
		}
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.Separator();
		GUILayout.Label("Runtime Settings");
		GUILayout.Box(GUIContent.none, _Line, GUILayout.ExpandWidth(true), GUILayout.Height(1f));

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(TerrainMaterial);
		EditorGUILayout.PropertyField(TerrainAlphaMaterial);
		if (EditorGUI.EndChangeCheck())
		{
			// Assign material to all renderers
			terrainObj.ApplyModifiedProperties();
			var generator = new LowPolyTerrainGenerator(terrain);
			generator.UpdateRenderers();
		}
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(CastShadows);
		if (EditorGUI.EndChangeCheck())
		{
			// turn on/off shadow casting
			terrainObj.ApplyModifiedProperties();
			var generator = new LowPolyTerrainGenerator(terrain);
			generator.UpdateShadowCasting();
		}
		if (CastShadows.boolValue)
		{
			EditorGUILayout.HelpBox("Shadow casting is experimental and has a few issues. Make sure that your lights have a high shadow bias, otherwise, lod levels will cast shadows on each other.", MessageType.Info);
		}
		GUILayout.Label("LOD Distances");
		EditorGUI.indentLevel++;
		for (int i = 0; i < LODDistances.arraySize; ++i)
		{
			if (i < LODDistances.arraySize - 1)
			{
				EditorGUILayout.PropertyField(LODDistances.GetArrayElementAtIndex(i), new GUIContent("LOD" + i.ToString() + "->LOD" + (i + 1).ToString()));
			}
			else
			{
				EditorGUILayout.PropertyField(LODDistances.GetArrayElementAtIndex(i), new GUIContent("LOD" + i.ToString() + "->OFF"));
			}
			if ( i > 0)
			{
				if (LODDistances.GetArrayElementAtIndex(i).floatValue <= LODDistances.GetArrayElementAtIndex(i-1).floatValue)
				{
					EditorGUILayout.HelpBox("LOD switching distances should be increasing!", MessageType.Warning);
				}
			}
		}
		EditorGUI.indentLevel--;
		if (LODDistances.GetArrayElementAtIndex(0).floatValue < ChunkSize.intValue)
		{
			EditorGUILayout.HelpBox("Your first LOD Distance is less than the chunk size, you may never get to see the lowest LOD level!", MessageType.Warning);
		}
		EditorGUILayout.PropertyField(LODTransitionTime);
		EditorGUILayout.PropertyField(FlipFlopPercent);
		terrainObj.ApplyModifiedProperties();
	}

	bool IsPowerOfTwoPlusOne(int value)
	{
		for (int i = 0; i < 31; ++i)
		{
			if (value == ((1 << i) + 1))
				return true;
		}
		return false;
	}
}
