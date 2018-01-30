using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LowPolyTerrainGenerator
{
	LowPolyTerrain _Terrain;

	/// <summary>
	/// Constructor, pass a terrain object that will be modified/generated
	/// </summary>
	public LowPolyTerrainGenerator(LowPolyTerrain aTerrain)
	{
		_Terrain = aTerrain;
	}

	/// <summary>
	/// Helper method used to display the asset associated with the terrain
	/// </summary>
	public Object GetFirstMesh()
	{
		Object ret = null;
		if (_Terrain.Chunks != null && _Terrain.Chunks.Length > 0)
		{
			if (_Terrain.Chunks[0].Renderers != null && _Terrain.Chunks[0].Renderers.Length > 0)
			{
				var filter = _Terrain.Chunks[0].Renderers[0].GetComponent<MeshFilter>();
				if (filter != null)
				{
					if (filter.sharedMesh != null)
					{
						var path = AssetDatabase.GetAssetPath(filter.sharedMesh);
						ret = AssetDatabase.LoadMainAssetAtPath(path);
					}
				}
			}
		}
		return ret;
	}

	/// <summary>
	/// When changing the material, update all the renderers
	/// </summary>
	public void UpdateRenderers()
	{
		foreach (var chunk in _Terrain.Chunks)
		{
			for (int i = 0; i < chunk.Renderers.Length; ++i)
			{
				chunk.Renderers[i].sharedMaterial = _Terrain.TerrainMaterial;
			}
		}
	}

	/// <summary>
	/// When flipping the shadow casting flag, update all the renderers
	/// </summary>
	public void UpdateShadowCasting()
	{
		foreach (var chunk in _Terrain.Chunks)
		{
			chunk.Renderers[0].shadowCastingMode = _Terrain.CastShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
		}
	}

	/// <summary>
	/// Main method: generate a terrain from all the terrain settings
	/// </summary>
	public void GenerateTerrain()
	{
		// Make sure we can generate something
		if (!CheckFilesAndReferences())
		{
			return;
		}

		// Destroy placed objects, if necessary
		foreach (var objects in _Terrain.GetComponents<LowPolyTerrainObjects>())
		{
			objects.ClearObjects();
		}

		// Destroy terrain objects
		DestroyExistingTerrainObjects();

		// Make sure our textures are readable
		PrepareTextureMaps();

		// Generate the heightmap
		GenerateHeightmap();

		// Generate the chunks
		GenerateChunks();

		// Create the terrain collider
		var terrainData = CreateTerrainDataFromHeightmap();

		var terrainCollider = _Terrain.GetComponent<TerrainCollider>();
		if (terrainCollider == null)
		{
			terrainCollider = _Terrain.gameObject.AddComponent<TerrainCollider>();
		}
		terrainCollider.terrainData = terrainData;

		// Bake the meshes
		BakeMeshes();

		// Clean up!
		EditorUtility.ClearProgressBar();
		EditorUtility.SetDirty(_Terrain.gameObject);
		AssetDatabase.Refresh();
	}


	/// <summary>
	/// Helper method to fetch the path to save mesh assets to
	/// </summary>
	string GetBasePath()
	{
		string basePath = _Terrain.gameObject.scene.path;
		if (basePath == "")
		{
			EditorUtility.DisplayDialog("Save Scene First", "You need to Save the Scene before you can generate Terrain", "Ok");
			return "";
		}

		string extension = System.IO.Path.GetExtension(basePath);
		return basePath.Substring(0, basePath.Length - extension.Length);
	}

	/// <summary>
	/// Makes sure that we have valid files or references set to generate a terrain
	/// </summary>
	bool CheckFilesAndReferences()
	{
		if (_Terrain.gameObject.scene.path == "")
		{
			EditorUtility.DisplayDialog("Save Scene First", "You need to Save the Scene before you can generate Terrain", "Ok");
			return false;
		}

		// Make sure we HAVE a source height map
		switch (_Terrain.SourceHeightMapType)
		{
			case LowPolyTerrain.HeightmapType.Bitmap:
				if (_Terrain.SourceHeightMap == null)
				{
					EditorUtility.DisplayDialog("No height map", "Can't generate mesh with no height map", "Ok");
					return false;
				}
				break;
			case LowPolyTerrain.HeightmapType.Raw16:
				goto case LowPolyTerrain.HeightmapType.Raw32;
			case LowPolyTerrain.HeightmapType.Raw32:
				if (!System.IO.File.Exists(_Terrain.SourceRawHeightMapFile))
				{
					EditorUtility.DisplayDialog("No height map", "Can't generate mesh with no height data", "Ok");
					return false;
				}
				break;
			case LowPolyTerrain.HeightmapType.Terrain:
				if (_Terrain.SourceTerrain == null)
				{
					EditorUtility.DisplayDialog("No Unity terrain", "Can't generate mesh without a Unity Terrain", "Ok");
					return false;
				}
				break;
		}

		return true;
	}

	/// <summary>
	/// Cleans up the terrain for a new generation
	/// </summary>
	void DestroyExistingTerrainObjects()
	{
		// Destroy any child terrain mesh of this object
		EditorUtility.DisplayProgressBar("Generating Terrain", "Deleting previous chunk assets", 0.0f);
		if (_Terrain.Chunks != null)
		{
			foreach (var chunk in _Terrain.Chunks)
			{
				if (chunk != null)
				{
					for (int i = 0; i < chunk.Renderers.Length; ++i)
					{
						var meshFilter = chunk.Renderers[i].GetComponent<MeshFilter>();
						if (meshFilter != null)
						{
							var mesh = meshFilter.sharedMesh;
							if (mesh != null)
							{
								string assetPath = AssetDatabase.GetAssetPath(mesh);
								if (assetPath != null && assetPath != "")
								{
									// Delete the previous asset
									AssetDatabase.DeleteAsset(assetPath);
								}

								// Destroy the mesh asset itself
								meshFilter.sharedMesh = null;
								GameObject.DestroyImmediate(mesh);
							}
						}
					}
				}
			}
			_Terrain.Chunks = null;
		}

		// Destroy actual objects
		for (int i = _Terrain.transform.childCount - 1; i >= 0; --i)
		{
			Object.DestroyImmediate(_Terrain.transform.GetChild(i).gameObject);
		}
	}

	/// <summary>
	/// Make sure all the texture maps are readable for generation
	/// </summary>
	void PrepareTextureMaps()
	{
		string basePath = _Terrain.gameObject.scene.path;
		string extension = System.IO.Path.GetExtension(basePath);
		string chunksPath = basePath.Substring(0, basePath.Length - extension.Length);
		string chunksFullPath = UnityEditorUtilities.GetFullPath(chunksPath);
		System.IO.Directory.CreateDirectory(chunksFullPath);
		AssetDatabase.Refresh();

		// Prepare texture maps
		if (_Terrain.SourceHeightMapType == LowPolyTerrain.HeightmapType.Bitmap)
		{
			// Make sure the sampling is "clamped" to the edges
			EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Height Texture", 0.5f);
			UnityEditorUtilities.EnableTextureReadWrite(_Terrain.SourceHeightMap);
			EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Height Texture", 1.0f);
			UnityEditorUtilities.SetTextureImporterOptions(_Terrain.SourceHeightMap, (importer) => importer.wrapMode = TextureWrapMode.Clamp);
		}

		// Same for the color maps
		if (_Terrain.GenerateVertColors)
		{
			if (_Terrain.SourceHeightMapType == LowPolyTerrain.HeightmapType.Terrain)
			{
				// In the case of a unity terrain, we want to be able to read the splat textures directly!
				var data = _Terrain.SourceTerrain.terrainData;
				for (int i = 0; i < data.alphamapLayers; ++i)
				{
					EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Splat Texture", (float)i / data.alphamapLayers);

					// Make sure the sampling is "clamped" to the edges
					UnityEditorUtilities.EnableTextureReadWrite(data.splatPrototypes[i].texture);
				}
			}
			else
			{
				// We want to be able to read the colormap
				EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Color Texture", 0.5f);
				UnityEditorUtilities.EnableTextureReadWrite(_Terrain.SourceColorMap);
				EditorUtility.DisplayProgressBar("Generating Terrain", "Preparing Color Texture", 1.0f);
				UnityEditorUtilities.SetTextureImporterOptions(_Terrain.SourceColorMap, (importer) => importer.wrapMode = TextureWrapMode.Clamp);
			}
		}
	}

	/// <summary>
	/// Reads the input data (textures, terrain, raw file) and generate the initial heightmap data!
	/// This heightmap will be stored in an asset, and can even be access at runtime, but mostly, it
	/// is used to generate the terrain meshes and LOD levels.
	/// </summary>
	void GenerateHeightmap()
	{
		_Terrain.Heightmap = ScriptableObject.CreateInstance<LowPolyHeightmap>();
		_Terrain.Heightmap.Initialize(_Terrain.TerrainSize, _Terrain.TerrainHeight, _Terrain.BaseResolution, _Terrain.ChunkSize, _Terrain.LODLevels);
		_Terrain.Heightmap.name = "Heightmap";

		// Import the heights
		switch (_Terrain.SourceHeightMapType)
		{
			case LowPolyTerrain.HeightmapType.Bitmap:
				ReadHeightsFromTexture();
				break;
			case LowPolyTerrain.HeightmapType.Raw16:
				ReadHeightsFromRaw16();
				break;
			case LowPolyTerrain.HeightmapType.Raw32:
				ReadHeightsFromRaw32();
				break;
			case LowPolyTerrain.HeightmapType.Terrain:
				ReadHeightsFromUnityTerrain();
				break;
			default:
				throw new System.ArgumentOutOfRangeException();
		}

		// Import the colors!
		if (_Terrain.GenerateVertColors)
		{
			switch (_Terrain.SourceHeightMapType)
			{
				case LowPolyTerrain.HeightmapType.Bitmap:
				case LowPolyTerrain.HeightmapType.Raw16:
				case LowPolyTerrain.HeightmapType.Raw32:
					ReadColorsFromColorMap();
					break;
				case LowPolyTerrain.HeightmapType.Terrain:
					ReadColorsFromUnityTerrain();
					break;
				default:
					throw new System.ArgumentOutOfRangeException();
			}
		}
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Generates the terrain chunks from the heightmap data!
	/// </summary>
	void GenerateChunks()
	{
		EditorUtility.DisplayProgressBar("Generating Terrain", "Generating chunks", 0.0f);
		int chunkPerSide = _Terrain.ChunkPerSide;
		_Terrain.Chunks = new LowPolyTerrain.ChunkRuntimeData[chunkPerSide * chunkPerSide];
		for (int x = 0; x < chunkPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Generating chunks", (float)x / chunkPerSide);
			for (int y = 0; y < chunkPerSide; ++y)
			{
				var chunkRoot = GenerateGameObjectForChunk(x, y);

				// Attach the group to this object
				chunkRoot.transform.SetParent(_Terrain.transform);
				chunkRoot.transform.localPosition = Vector3.zero;
				chunkRoot.transform.localRotation = Quaternion.identity;
			}
		}
	}

	/// <summary>
	/// Shove all the meshes to an actual asset file, so as not to bloat the scene file
	/// </summary>
	void BakeMeshes()
	{
		if (_Terrain.gameObject.scene.path == "")
		{
			EditorUtility.DisplayDialog("Save Scene First", "You need to Save the Scene before you can generate Terrain", "Ok");
			return;
		}

		string meshPath = GetBasePath() + "/" + _Terrain.name + "_FacetedChunks.asset";
		bool created = false;

		EditorUtility.DisplayProgressBar("Generating Terrain", "Baking chunks asset", 0.0f);
		int chunkCount = _Terrain.Chunks.Length;
		int chunkIndex = 0;
		foreach (var chunk in _Terrain.Chunks)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Baking chunks asset", (float)chunkIndex / chunkCount);
			Transform chunkTransform = chunk.ChunkRoot.transform;
			List<Mesh> meshes = new List<Mesh>(chunkTransform.GetComponentsInChildren<MeshFilter>().Select(mf => mf.sharedMesh));
			for (int j = 0; j < meshes.Count; ++j)
			{
				if (created)
				{
					AssetDatabase.AddObjectToAsset(meshes[j], meshPath);
				}
				else
				{
					AssetDatabase.CreateAsset(meshes[j], meshPath);
					created = true;
				}
			}
			chunkIndex++;
		}

		// Pack in the collider heightmap data as well
		var terrainCollider = _Terrain.GetComponent<TerrainCollider>();
		if (terrainCollider != null)
		{
			var heightData = terrainCollider.terrainData;
			AssetDatabase.AddObjectToAsset(heightData, meshPath);
		}

		AssetDatabase.AddObjectToAsset(_Terrain.Heightmap, meshPath);

		EditorUtility.DisplayProgressBar("Generating Terrain", "Saving Chunks Asset (this may take a while)", 0.5f);
		AssetDatabase.SaveAssets();
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Generate all the LOD levels for a given Terrain Chunk
	/// Pass in source data, and heightmap generation information, and return a root game object that has LOD meshes as children
	/// </summary>
	GameObject GenerateGameObjectForChunk(int aChunkX, int aChunkY)
	{
		// Create a root for the LODGroup
		var groupRoot = new GameObject("Chunk " + aChunkX + "," + aChunkY);

		// Create the children objects and attach them to the parent,
		// and create LOD data in the process as well
		MeshRenderer[] renderers = new MeshRenderer[_Terrain.LODLevels];
		LOD[] lods = new LOD[_Terrain.LODLevels];

		float currentHeight = 0.25f;
		for (int i = 0; i < _Terrain.LODLevels; ++i)
		{
			var lodChunk = GenerateGameObjectForChunkAndLOD(aChunkX, aChunkY, i);

			lodChunk.transform.SetParent(groupRoot.transform);
			lodChunk.transform.localPosition = Vector3.zero;
			lodChunk.transform.localRotation = Quaternion.identity;

			renderers[i] = lodChunk.GetComponent<MeshRenderer>();
			lods[i] = new LOD(currentHeight, new Renderer[] { lodChunk.GetComponent<MeshRenderer>() });
			if (i != 0 || !_Terrain.CastShadows)
			{
				renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			}

			currentHeight /= 2.0f;
		}

		// Add a LODGroup to the group root object, and make it point to the renderers
		// This is really only for the scene view, the component gets destroyed at the start of the game!
		var unityLODGroup = groupRoot.AddComponent<LODGroup>();
		unityLODGroup.hideFlags = HideFlags.NotEditable | HideFlags.HideInInspector;
		lods[lods.Length - 1].screenRelativeTransitionHeight = 0.0f;
		unityLODGroup.SetLODs(lods);

		// Create the chunk lod data
		_Terrain.Chunks[aChunkX * _Terrain.ChunkPerSide + aChunkY] = new LowPolyTerrain.ChunkRuntimeData(groupRoot, renderers);

		// Hide chunk if desired
		if (_Terrain.HideChunksInHierarchy)
		{
			groupRoot.hideFlags = HideFlags.HideInHierarchy;
		}

		// Done
		return groupRoot;
	}

	/// <summary>
	/// Generate a given LOD level of a given chunk of the terrain
	/// </summary>
	GameObject GenerateGameObjectForChunkAndLOD(int aChunkX, int aChunkY, int aLODIndex)
	{
		Texture2D retTexture = null;
		//bool drawTexture = aChunkX == 0 && aChunkY == 0;
		bool drawTexture = false;
		if (drawTexture)
		{
			retTexture = new Texture2D(_Terrain.UV2MapSize, _Terrain.UV2MapSize, TextureFormat.ARGB32, false);
		}

		Mesh mesh = GenerateMeshForChunkAndLOD(aChunkX, aChunkY, aLODIndex);

		// Now create a gameobject, with renderer and all and assign it the mesh
		var lodObject = new GameObject(mesh.name);

		var meshFilter = lodObject.AddComponent<MeshFilter>();
		meshFilter.sharedMesh = mesh;

		var meshRenderer = lodObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = _Terrain.TerrainMaterial;

		// This is a terrain mesh object, s oflag it as static
		GameObjectUtility.SetStaticEditorFlags(lodObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

		if (drawTexture)
		{
			for (int i = 0; i < mesh.uv2.Length; i += 3)
			{
				TextureDraw.DrawLine(retTexture,
					Mathf.RoundToInt(mesh.uv2[i + 0].x * _Terrain.UV2MapSize), Mathf.RoundToInt(mesh.uv2[i + 0].y * _Terrain.UV2MapSize),
					Mathf.RoundToInt(mesh.uv2[i + 1].x * _Terrain.UV2MapSize), Mathf.RoundToInt(mesh.uv2[i + 1].y * _Terrain.UV2MapSize),
					Color.red);

				TextureDraw.DrawLine(retTexture,
					Mathf.RoundToInt(mesh.uv2[i + 1].x * _Terrain.UV2MapSize), Mathf.RoundToInt(mesh.uv2[i + 1].y * _Terrain.UV2MapSize),
					Mathf.RoundToInt(mesh.uv2[i + 2].x * _Terrain.UV2MapSize), Mathf.RoundToInt(mesh.uv2[i + 2].y * _Terrain.UV2MapSize),
					Color.red);

				TextureDraw.DrawLine(retTexture,
					Mathf.RoundToInt(mesh.uv2[i + 2].x * _Terrain.UV2MapSize), Mathf.RoundToInt(mesh.uv2[i + 2].y * _Terrain.UV2MapSize),
					Mathf.RoundToInt(mesh.uv2[i + 0].x * _Terrain.UV2MapSize), Mathf.RoundToInt(mesh.uv2[i + 0].y * _Terrain.UV2MapSize),
					Color.red);
			}


			string basePath = _Terrain.gameObject.scene.path;
			string extension = System.IO.Path.GetExtension(basePath);
			string imagePath = basePath.Substring(0, basePath.Length - extension.Length) + "/" + _Terrain.name + "_Chunk_" + aChunkX + "_" + aChunkY + "_" + aLODIndex + ".png";
			string imageFilePath = UnityEditorUtilities.GetFullPath(imagePath);

			byte[] bytes = retTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(imageFilePath, bytes);
		}
		return lodObject;

	}

	/// <summary>
	/// Helper struct used during mesh generation!
	/// </summary>
	struct AddTriangleParams
	{
		public Vector3[] Verts;
		public int[] Triangle;
		public bool FlippedTri;
		public bool UpperTri;
	}

	/// <summary>
	/// Generate a given LOD level of a given chunk of the terrain
	/// </summary>
	Mesh GenerateMeshForChunkAndLOD(int aChunkX, int aChunkY, int aLODIndex)
	{
		// Grab the size info for this LOD level
		var chunkInfo = _Terrain.Heightmap.Chunks[aChunkX, aChunkY];
		var sizeInfo = _Terrain.Heightmap.Sizes[aLODIndex];

		// Create the first mesh
		Mesh mesh = new Mesh();
		List<Vector3> verts = new List<Vector3>(sizeInfo.vertCount);
		List<Color> colors = new List<Color>(sizeInfo.vertCount);
		List<Vector2> uvs = new List<Vector2>(sizeInfo.vertCount);
		List<Vector2> uv2s = new List<Vector2>(sizeInfo.vertCount);
		List<Vector3> normals = new List<Vector3>(sizeInfo.vertCount);
		List<int> tris = new List<int>(sizeInfo.triCount * 3);

		Vector3[] edgeNormalXM = new Vector3[sizeInfo.quadPerChunkSide];
		Vector3[] edgeNormalXP = new Vector3[sizeInfo.quadPerChunkSide];
		Vector3[] edgeNormalYM = new Vector3[sizeInfo.quadPerChunkSide];
		Vector3[] edgeNormalYP = new Vector3[sizeInfo.quadPerChunkSide];

		Vector2[][] edgeUV2XM = null;
		Vector2[][] edgeUV2XP = null;
		Vector2[][] edgeUV2YM = null;
		Vector2[][] edgeUV2YP = null;

		if (_Terrain.GenerateUV2)
		{
			edgeUV2XM = new Vector2[sizeInfo.quadPerChunkSide][];
			edgeUV2XP = new Vector2[sizeInfo.quadPerChunkSide][];
			edgeUV2YM = new Vector2[sizeInfo.quadPerChunkSide][];
			edgeUV2YP = new Vector2[sizeInfo.quadPerChunkSide][];
		}

		int uv2padding = _Terrain.UV2Padding;
		int skirtQuad = 0;
		int uvQuadPerSide = (sizeInfo.quadPerChunkSide + skirtQuad);// / uvMapCount;
		int uv2paddingTotal = _Terrain.UV2Padding * 2 + uv2padding * (2 * uvQuadPerSide - 1);
		float uv2perQuad = (float)(_Terrain.UV2MapSize - uv2paddingTotal) / uvQuadPerSide;
		float uv2QuadStride = uv2perQuad + uv2padding * 2;

		int vertIndex = 0;
		System.Action<int, int, AddTriangleParams> AddTriangle = (x, y, triparams) =>
		{
			// Verts
			var corner0 = triparams.Verts[triparams.Triangle[0]];
			var corner1 = triparams.Verts[triparams.Triangle[1]];
			var corner2 = triparams.Verts[triparams.Triangle[2]];
			verts.Add(corner0);
			verts.Add(corner1);
			verts.Add(corner2);

			// Compute the u,v barycenter
			float u = (corner0.x + corner1.x + corner2.x) / (_Terrain.TerrainSize * 3.0f);
			float v = (corner0.z + corner1.z + corner2.z) / (_Terrain.TerrainSize * 3.0f);

			Vector2 triUVs = new Vector2(u, v);
			uvs.Add(triUVs);
			uvs.Add(triUVs);
			uvs.Add(triUVs);

			if (_Terrain.GenerateVertColors)
			{
				// Sample the colormap at that location
				Color triColor = LowPolyUtils.SampleBilinear(_Terrain.Heightmap.Colors, u, v);
				colors.Add(triColor);
				colors.Add(triColor);
				colors.Add(triColor);
			}

			// lightmap uvs
			Vector2[] cornerUV2s = null;
			if (_Terrain.GenerateUV2)
			{
				float uv2x = 0.0f;
				float uv2y = 0.0f;
				float uv2xp1 = 0.0f;
				float uv2yp1 = 0.0f;

				int padX = _Terrain.UV2Padding;
				int padY = _Terrain.UV2Padding;
				if (x >= 0 && x < sizeInfo.quadPerChunkSide && y >= 0 && y < sizeInfo.quadPerChunkSide)
				{
					padX += ((triparams.UpperTri && !triparams.FlippedTri || !triparams.UpperTri && triparams.FlippedTri) ? uv2padding : 0);
					padY += ((triparams.UpperTri && !triparams.FlippedTri || triparams.UpperTri && triparams.FlippedTri) ? uv2padding : 0);
				}
				int chunkX = aChunkX * sizeInfo.quadPerChunkSide;
				int chunkY = aChunkY * sizeInfo.quadPerChunkSide;

				uv2x = padX + ((x + chunkX) % uvQuadPerSide) * uv2QuadStride;
				uv2xp1 = uv2x + uv2perQuad;
				uv2y = padY + ((y + chunkY) % uvQuadPerSide) * uv2QuadStride;
				uv2yp1 = uv2y + uv2perQuad;

				cornerUV2s = new Vector2[4]
				{
					new Vector2(uv2x / _Terrain.UV2MapSize, uv2y / _Terrain.UV2MapSize),
					new Vector2(uv2x / _Terrain.UV2MapSize, uv2yp1 / _Terrain.UV2MapSize),
					new Vector2(uv2xp1 / _Terrain.UV2MapSize, uv2y / _Terrain.UV2MapSize),
					new Vector2(uv2xp1 / _Terrain.UV2MapSize, uv2yp1 / _Terrain.UV2MapSize),
				};

				var uv20 = cornerUV2s[triparams.Triangle[0]];
				var uv21 = cornerUV2s[triparams.Triangle[1]];
				var uv22 = cornerUV2s[triparams.Triangle[2]];
				uv2s.Add(uv20);
				uv2s.Add(uv21);
				uv2s.Add(uv22);
			}

			// Normal
			Vector3 triNormal = Vector3.Cross(corner1 - corner0, corner2 - corner0).normalized;
			normals.Add(triNormal);
			normals.Add(triNormal);
			normals.Add(triNormal);

			if (x == 0 && y >= 0 && y < sizeInfo.quadPerChunkSide && triparams.FlippedTri == triparams.UpperTri)
			{
				edgeNormalXM[y] = triNormal;
			}

			if (x == sizeInfo.quadPerChunkSide - 1 && y >= 0 && y < sizeInfo.quadPerChunkSide && triparams.FlippedTri != triparams.UpperTri)
			{
				edgeNormalXP[y] = triNormal;
			}

			if (y == 0 && x >= 0 && x < sizeInfo.quadPerChunkSide && !triparams.UpperTri)
			{
				edgeNormalYM[x] = triNormal;
			}

			if (y == sizeInfo.quadPerChunkSide - 1 && x >= 0 && x < sizeInfo.quadPerChunkSide && triparams.UpperTri)
			{
				edgeNormalYP[x] = triNormal;
			}

			if (_Terrain.GenerateUV2)
			{
				if (x == 0 && y >= 0 && y < sizeInfo.quadPerChunkSide && triparams.FlippedTri == triparams.UpperTri)
				{
					cornerUV2s[2] = cornerUV2s[0];
					cornerUV2s[3] = cornerUV2s[1];
					edgeUV2XM[y] = cornerUV2s;
				}

				if (x == sizeInfo.quadPerChunkSide - 1 && y >= 0 && y < sizeInfo.quadPerChunkSide && triparams.FlippedTri != triparams.UpperTri)
				{
					cornerUV2s[0] = cornerUV2s[2];
					cornerUV2s[1] = cornerUV2s[3];
					edgeUV2XP[y] = cornerUV2s;
				}

				if (y == 0 && x >= 0 && x < sizeInfo.quadPerChunkSide && !triparams.UpperTri)
				{
					cornerUV2s[1] = cornerUV2s[0];
					cornerUV2s[3] = cornerUV2s[2];
					edgeUV2YM[x] = cornerUV2s;
				}

				if (y == sizeInfo.quadPerChunkSide - 1 && x >= 0 && x < sizeInfo.quadPerChunkSide && triparams.UpperTri)
				{
					cornerUV2s[0] = cornerUV2s[1];
					cornerUV2s[2] = cornerUV2s[3];
					edgeUV2YP[x] = cornerUV2s;
				}
			}

			// And make the triangle
			tris.Add(vertIndex + 0);
			tris.Add(vertIndex + 1);
			tris.Add(vertIndex + 2);

			vertIndex += 3;
		};

		Vector3[] currentCorners = new Vector3[4];
		int[] topLeftBottomRightLower = new int[3] { 0, 1, 2 };
		int[] topLeftBottomRightUpper = new int[3] { 2, 1, 3 };
		int[] topRightBottomLeftLower = new int[3] { 0, 3, 2 };
		int[] topRightBottomLeftUpper = new int[3] { 0, 1, 3 };

		float skirtHeight = _Terrain.BaseResolution * Mathf.Pow(2.0f, aLODIndex);
		for (int x = -1; x < sizeInfo.quadPerChunkSide + 1; ++x)
		{
			for (int y = -1; y < sizeInfo.quadPerChunkSide + 1; ++y)
			{
				int lowerX = (x == -1) ? chunkInfo.originalVertOffsetX : x * sizeInfo.stride + chunkInfo.originalVertOffsetX;
				int lowerY = (y == -1) ? chunkInfo.originalVertOffsetY : y * sizeInfo.stride + chunkInfo.originalVertOffsetY;
				int upperX = (x == sizeInfo.quadPerChunkSide) ? sizeInfo.quadPerChunkSide * sizeInfo.stride + chunkInfo.originalVertOffsetX : (x + 1) * sizeInfo.stride + chunkInfo.originalVertOffsetX;
				int upperY = (y == sizeInfo.quadPerChunkSide) ? sizeInfo.quadPerChunkSide * sizeInfo.stride + chunkInfo.originalVertOffsetY : (y + 1) * sizeInfo.stride + chunkInfo.originalVertOffsetY;

				// Fixup coordinates for skirts
				Vector3 corner0 = _Terrain.Heightmap.GetVertex(lowerX, lowerY);
				if (x == -1 || y == -1)
				{
					corner0.y -= skirtHeight;
				}
				currentCorners[0] = corner0;

				Vector3 corner1 = _Terrain.Heightmap.GetVertex(lowerX, upperY);
				if (x == -1 || y == sizeInfo.quadPerChunkSide)
				{
					corner1.y -= skirtHeight;
				}
				currentCorners[1] = corner1;

				Vector3 corner2 = _Terrain.Heightmap.GetVertex(upperX, lowerY);
				if (x == sizeInfo.quadPerChunkSide || y == -1)
				{
					corner2.y -= skirtHeight;
				}
				currentCorners[2] = corner2;

				Vector3 corner3 = _Terrain.Heightmap.GetVertex(upperX, upperY);
				if (x == sizeInfo.quadPerChunkSide || y == sizeInfo.quadPerChunkSide)
				{
					corner3.y -= skirtHeight;
				}
				currentCorners[3] = corner3;

				bool topLeftToBottomRight = false;
				if (!_Terrain.UniformTriangles && x >= 0 && x < sizeInfo.quadPerChunkSide && y >= 0 && y < sizeInfo.quadPerChunkSide)
				{
					// Figure out which edge is best to use as mid-edge
					Vector2 center = new Vector2((corner1.x + corner2.x) * 0.5f, (corner1.z + corner2.z) * 0.5f);
					float centerHeight = _Terrain.Heightmap.SampleNormalizedHeight(center);

					Vector2 toCorner0 = new Vector2(corner0.x - center.x, corner0.z - center.y);
					Vector2 toCorner1 = new Vector2(corner1.x - center.x, corner1.z - center.y);
					float pastCorner0Height = _Terrain.Heightmap.SampleNormalizedHeight(center + toCorner0 * 4.0f) - centerHeight;
					float pastCorner1Height = _Terrain.Heightmap.SampleNormalizedHeight(center + toCorner1 * 4.0f) - centerHeight;
					float pastCorner2Height = _Terrain.Heightmap.SampleNormalizedHeight(center - toCorner1 * 4.0f) - centerHeight;
					float pastCorner3Height = _Terrain.Heightmap.SampleNormalizedHeight(center - toCorner0 * 4.0f) - centerHeight;

					float heightDelta0 = Mathf.Abs(pastCorner0Height - pastCorner3Height);
					float heightDelta1 = Mathf.Abs(pastCorner1Height - pastCorner2Height);

					topLeftToBottomRight = heightDelta0 < heightDelta1;
				}

				if (topLeftToBottomRight)
				{
					// Mid edge goes from top-left to bottom-right
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topLeftBottomRightLower,
						FlippedTri = false,
						UpperTri = false
					});
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topLeftBottomRightUpper,
						FlippedTri = false,
						UpperTri = true
					});
				}
				else
				{
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topRightBottomLeftLower,
						FlippedTri = true,
						UpperTri = false
					});
					AddTriangle(x, y, new AddTriangleParams()
					{
						Verts = currentCorners,
						Triangle = topRightBottomLeftUpper,
						FlippedTri = true,
						UpperTri = true
					});
				}
			}
		}

		System.Action<int, int, Vector3> SetNormal = (x, y, normal) =>
		{
			for (int i = 0; i < 6; ++i)
			{
				normals[((x + 1) * (sizeInfo.quadPerChunkSide + 2) + (y + 1)) * 6 + i] = normal;
			}
		};

		System.Action<int, int, Vector2[]> SetUV2 = (x, y, corners) =>
		{
			int quadIndex = ((x + 1) * (sizeInfo.quadPerChunkSide + 2) + (y + 1)) * 6;

			uv2s[quadIndex + 0] = corners[0];
			uv2s[quadIndex + 1] = corners[1];
			uv2s[quadIndex + 2] = corners[2];

			uv2s[quadIndex + 3] = corners[2];
			uv2s[quadIndex + 4] = corners[1];
			uv2s[quadIndex + 5] = corners[3];
		};

		// Fixup skirt normals
		for (int i = 0; i < sizeInfo.quadPerChunkSide; ++i)
		{
			SetNormal(-1, i, edgeNormalXM[i]);
			SetNormal(sizeInfo.quadPerChunkSide, i, edgeNormalXP[i]);
			SetNormal(i, -1, edgeNormalYM[i]);
			SetNormal(i, sizeInfo.quadPerChunkSide, edgeNormalYP[i]);
		}

		mesh.vertices = verts.ToArray();
		mesh.normals = normals.ToArray();
		if (_Terrain.GenerateVertColors)
		{
			mesh.colors = colors.ToArray();
		}
		else
		{
			mesh.uv = uvs.ToArray();
		}
		if (_Terrain.GenerateUV2)
		{
			for (int i = -1; i < sizeInfo.quadPerChunkSide + 1; ++i)
			{
				int uvIndex = Mathf.Clamp(i, 0, sizeInfo.quadPerChunkSide - 1);
				SetUV2(-1, i, edgeUV2XM[uvIndex]);
				SetUV2(sizeInfo.quadPerChunkSide, i, edgeUV2XP[uvIndex]);
				SetUV2(i, -1, edgeUV2YM[uvIndex]);
				SetUV2(i, sizeInfo.quadPerChunkSide, edgeUV2YP[uvIndex]);
			}
			mesh.uv2 = uv2s.ToArray();
		}
		mesh.triangles = tris.ToArray();
		mesh.name = "LOD" + aLODIndex;
		mesh.RecalculateBounds();

		return mesh;
	}

	/// <summary>
	/// Grab the height map information from a texture's greyscale
	/// </summary>
	void ReadHeightsFromTexture()
	{
		System.Func<float, float, float> readFromTexture = (u,v) =>
			{
				return _Terrain.SourceHeightMap.GetPixelBilinear(u, v).grayscale;
			};

		ReadHeightmap(readFromTexture);
	}

	/// <summary>
	/// Grab the height map from a raw floating point file
	/// </summary>
	void ReadHeightsFromRaw32()
	{
		var rawBytes = System.IO.File.ReadAllBytes(_Terrain.SourceRawHeightMapFile);
		bool reverseBytes = System.BitConverter.IsLittleEndian == (_Terrain.RawHeightMapOrder == LowPolyTerrain.ByteOrder.Mac);
		float[,] rawFloats = new float[_Terrain.RawHeightMapSize, _Terrain.RawHeightMapSize];
		int index = 0;

		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", 0.0f);
		for (int y = 0; y < _Terrain.RawHeightMapSize; ++y)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", (float)y / _Terrain.RawHeightMapSize);
			for (int x = 0; x < _Terrain.RawHeightMapSize; ++x)
			{
				if (reverseBytes)
				{
					System.Array.Reverse(rawBytes, index, sizeof(System.Single));
				}
				rawFloats[x, _Terrain.RawHeightMapSize - y - 1] = System.BitConverter.ToSingle(rawBytes, index);
				index += sizeof(float);
			}
		}
		EditorUtility.ClearProgressBar();

		ReadHeightmap((u, v) => LowPolyUtils.SampleBilinear(rawFloats, u, v));
	}

	/// <summary>
	/// Grab the height map from a raw16 (uint16) file
	/// </summary>
	void ReadHeightsFromRaw16()
	{
		var rawBytes = System.IO.File.ReadAllBytes(_Terrain.SourceRawHeightMapFile);
		bool reverseBytes = System.BitConverter.IsLittleEndian == (_Terrain.RawHeightMapOrder == LowPolyTerrain.ByteOrder.Mac);
		float[,] rawFloats = new float[_Terrain.RawHeightMapSize, _Terrain.RawHeightMapSize];
		int index = 0;

		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", 0.0f);
		for (int y = 0; y < _Terrain.RawHeightMapSize; ++y)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Heightmap", (float)y / _Terrain.RawHeightMapSize);
			for (int x = 0; x < _Terrain.RawHeightMapSize; ++x)
			{
				if (reverseBytes)
				{
					System.Array.Reverse(rawBytes, index, sizeof(System.UInt16));
				}
				rawFloats[x, _Terrain.RawHeightMapSize - y - 1] = (float)System.BitConverter.ToUInt16(rawBytes, index) / System.UInt16.MaxValue;

				index += sizeof(System.UInt16);
			}
		}
		EditorUtility.ClearProgressBar();

		ReadHeightmap((u, v) => LowPolyUtils.SampleBilinear(rawFloats, u, v));
	}

	/// <summary>
	/// Grabs the heightmap from a Unity Terrain!
	/// </summary>
	void ReadHeightsFromUnityTerrain()
	{
		var heightmap = _Terrain.Heightmap;
		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Terrain Heightmap", 0.0f);
		for (int x = 0; x < heightmap.originalVertPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Terrain Heightmap", (float)x / heightmap.originalVertPerSide);
			for (int y = 0; y < heightmap.originalVertPerSide; ++y)
			{
				float dataHeight = _Terrain.SourceTerrain.terrainData.GetHeight(x, y);
				heightmap.Heights[y, x] = dataHeight / _Terrain.TerrainHeight;

				Vector2 xyOffset = Vector2.zero;
				if (!_Terrain.UniformTriangles)
				{
					xyOffset = Random.insideUnitCircle * _Terrain.RandomXZOffset;
				}
				heightmap.Offsets[x, y] = new Vector3(xyOffset.x, Random.Range(-_Terrain.RandomYOffset, _Terrain.RandomYOffset), xyOffset.y);
			}
		}

		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Read the colors from a bitmap!
	/// </summary>
	void ReadColorsFromColorMap()
	{
		var heightmap = _Terrain.Heightmap;
		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Colormap", 0.0f);
		for (int x = 0; x < heightmap.originalVertPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Colormap", (float)x / heightmap.originalVertPerSide);
			for (int y = 0; y < heightmap.originalVertPerSide; ++y)
			{
				float u = (float)x / (heightmap.originalVertPerSide - 1);
				float v = (float)y / (heightmap.originalVertPerSide - 1);
				heightmap.Colors[x, y] = _Terrain.SourceColorMap.GetPixelBilinear(u, v);
			}
		}

		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Read the colors from a Unity Terrain Splat map!
	/// </summary>
	void ReadColorsFromUnityTerrain()
	{
		var heightmap = _Terrain.Heightmap;
		var data = _Terrain.SourceTerrain.terrainData;
		int size = data.alphamapResolution;
		var mapWeights = data.GetAlphamaps(0, 0, size, size);
		var mapTextures = data.alphamapTextures;

		float[] vertWeights = new float[data.alphamapLayers];
		Color[] averageColors = new Color[data.alphamapLayers];

		for (int i = 0; i < data.alphamapLayers; ++i)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Extracting Splat Colors", (float)i / data.alphamapLayers);

			Color[] pixels = data.splatPrototypes[i].texture.GetPixels();
			float r = 0.0f;
			float g = 0.0f;
			float b = 0.0f;
			for (int j = 0; j < pixels.Length; ++j)
			{
				Color pixel = pixels[j];
				r += pixel.r;
				g += pixel.g;
				b += pixel.b;
			}
			averageColors[i] = new Color(r / pixels.Length, g / pixels.Length, b / pixels.Length, 1.0f);
		}

		EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Colormap", 0.0f);
		for (int x = 0; x < heightmap.originalVertPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Reading Colormap", (float)x / heightmap.originalVertPerSide);
			for (int y = 0; y < heightmap.originalVertPerSide; ++y)
			{
				// Fetch the sampled weights of all the splatmaps!
				float u = (float)x / (heightmap.originalVertPerSide - 1);
				float v = (float)y / (heightmap.originalVertPerSide - 1);
				LowPolyUtils.SampleBilinear(mapWeights, u, v, vertWeights);

				// Blend the colors of the original maps accordingly
				float r = 0.0f;
				float g = 0.0f;
				float b = 0.0f;
				for (int i = 0; i < data.alphamapLayers; ++i)
				{
					Color mapColor = averageColors[i];
					r += mapColor.r * vertWeights[i];
					g += mapColor.g * vertWeights[i];
					b += mapColor.b * vertWeights[i];
				}
				heightmap.Colors[y, x] = new Color(r, g, b, 1.0f);
			}
		}

		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Method to read the height map into the initial array of vertices used to generate the terrain meshes
	/// </summary>
	void ReadHeightmap(System.Func<float, float, float> sampleFunc)
	{
		var heightmap = _Terrain.Heightmap;
		EditorUtility.DisplayProgressBar("Generating Terrain", "Generating Base Heightmap", 0.0f);
		for (int x = 0; x < heightmap.originalVertPerSide; ++x)
		{
			EditorUtility.DisplayProgressBar("Generating Terrain", "Generating Base Heightmap", (float)x / heightmap.originalVertPerSide);
			for (int y = 0; y < heightmap.originalVertPerSide; ++y)
			{
				float u = (float)x / (heightmap.originalVertPerSide - 1);
				float v = (float)y / (heightmap.originalVertPerSide - 1);
				float greyscale = sampleFunc(u, v);
				heightmap.Heights[y, x] = greyscale;
				Vector2 xyOffset = Vector2.zero;
				if (!_Terrain.UniformTriangles)
				{
					xyOffset = Random.insideUnitCircle * _Terrain.RandomXZOffset;
				}
				heightmap.Offsets[x, y] = new Vector3(xyOffset.x, Random.Range(-_Terrain.RandomYOffset, _Terrain.RandomYOffset), xyOffset.y);
			}
		}
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Create a Terrain Data object that matches the low poly terrain!
	/// </summary>
	TerrainData CreateTerrainDataFromHeightmap()
	{
		var terrainData = new TerrainData();
		terrainData.alphamapResolution = _Terrain.Heightmap.originalVertPerSide;
		terrainData.heightmapResolution = _Terrain.Heightmap.originalVertPerSide;
		terrainData.size = new Vector3(_Terrain.TerrainSize, _Terrain.TerrainHeight, _Terrain.TerrainSize);
		terrainData.SetHeights(0, 0, _Terrain.Heightmap.Heights);
		terrainData.name = _Terrain.name + " Heightmap Data";
		return terrainData;
	}
}
