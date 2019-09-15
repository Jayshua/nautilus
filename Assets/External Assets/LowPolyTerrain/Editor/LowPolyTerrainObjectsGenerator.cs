using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LowPolyTerrainObjectsGenerator
{
	LowPolyTerrainObjects _Objects;

	public LowPolyTerrainObjectsGenerator(LowPolyTerrainObjects aObjects)
	{
		_Objects = aObjects;
	}

	public void GenerateObjects()
	{
		if (_Objects.ObjectPlacementMap == null || _Objects.ObjectPrefabs == null || _Objects.ObjectPrefabs.Length == 0)
		{
			return;
		}

		var terrain = _Objects.GetComponent<LowPolyTerrain>();

		UnityEditorUtilities.EnableTextureReadWrite(_Objects.ObjectPlacementMap);
		UnityEditorUtilities.EnableTextureReadWrite(terrain.SourceHeightMap);

		_Objects.ClearObjects();

		// These guys will keep track of matching chunks to terrain chunks
		_Objects.StoredChunks = new List<LowPolyTerrainObjects.ChunkRuntimeData>();

		// Create object roots
		for (int i = 0; i < terrain.Chunks.Length; ++i)
		{
			var chunk = terrain.Chunks[i];

			// Delete previous object root, if any
			var objectsRoot = chunk.ChunkRoot.transform.Find("Objects Root");
			if (objectsRoot == null)
			{
				// Create a root for the objects
				var objectRootObj = new GameObject("Objects Root");
				objectsRoot = objectRootObj.transform;
				objectsRoot.SetParent(chunk.ChunkRoot.transform);
				objectsRoot.localPosition = Vector3.zero;
				objectsRoot.localRotation = Quaternion.identity;
				GameObjectUtility.SetStaticEditorFlags(objectsRoot.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
			}
			_Objects.StoredChunks.Add(new LowPolyTerrainObjects.ChunkRuntimeData(objectsRoot.gameObject));
		}

		for (int x = 0; x < _Objects.ObjectPlacementMap.width; ++x)
		{
			for (int y = 0; y < _Objects.ObjectPlacementMap.height; ++y)
			{
				if (Random.Range(0.0f, 1.0f) < _Objects.ObjectPlacementMap.GetPixel(x, y).grayscale * _Objects.ObjectProbabilityScale)
				{
					// Instantiate a object
					Vector2 uv = new Vector2((float)x / _Objects.ObjectPlacementMap.width, (float)y / _Objects.ObjectPlacementMap.height);
					Vector2 xy = new Vector2(uv.x * terrain.TerrainSize, uv.y * terrain.TerrainSize);

					// Figure out the parent of this object
					int terrainChunkIndex = terrain.GetChunkIndex(uv.x, uv.y);
					var objectChunk = _Objects.StoredChunks[terrainChunkIndex];
					var parent = objectChunk.ObjectRoot;

					// Grab the height
					float height = terrain.SampleHeight(xy);
					Vector3 objectPosition = new Vector3(xy.x, height - _Objects.ObjectHeightOffset, xy.y);
					Quaternion objectRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
					var obj = Object.Instantiate(_Objects.ObjectPrefabs[Random.Range(0, _Objects.ObjectPrefabs.Length)]) as GameObject;

					//obj.transform.SetParent(objectsRoot.transform);
					obj.transform.SetParent(parent.transform);
					obj.transform.localPosition = objectPosition;
					obj.transform.localRotation = objectRotation;
					obj.transform.localScale = Vector3.one * Random.Range(_Objects.ObjectScaleMin, _Objects.ObjectScaleMax);

					// Set static flags
					GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
					foreach (var childTr in obj.GetComponentsInChildren<Transform>())
					{
						GameObjectUtility.SetStaticEditorFlags(childTr.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
					}

					// Keep track of the object
					objectChunk.Objects.Add(obj);

					// Flag object as static, so it gets batched
					obj.isStatic = true;
				}
			}
		}

		EditorUtility.SetDirty(_Objects.gameObject);
	}

}
