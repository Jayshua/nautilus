using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the original heightmap that generated the LowPoly Terrain
/// Can be used at runtime to query the terrain height or color
/// </summary>
public class LowPolyHeightmap
	: ScriptableObject
	, ISerializationCallbackReceiver
{
	[System.Serializable]
	public class ChunkSizeInfo
	{
		public int originalVertOffsetX;
		public int originalVertOffsetY;
	}

	[System.Serializable]
	public class LODSizeInfo
	{
		public int quadPerSide;
		public int quadPerChunkSide;
		public int quadCount;
		public int triCount;
		public int vertPerSide;
		public int vertCount;
		public int stride;
	}

	public LODSizeInfo[] Sizes;
	public ChunkSizeInfo[,] Chunks;
	public float[,] Heights;
	public Vector3[,] Offsets;
	public Color[,] Colors;

	// Sample the heightmap
	public int originalQuadPerSide
	{
		get;
		private set;
	}

	public int originalVertPerSide
	{
		get;
		private set;
	}

	public int TerrainSize
	{
		get;
		private set;
	}

	public int TerrainHeight
	{
		get;
		private set;
	}

	public int ChunkSize
	{
		get;
		private set;
	}

	public int ChunkPerSide
	{
		get
		{
			if (ChunkSize > 0)
				return TerrainSize / ChunkSize;
			else
				return 0;
		}
	}

	public float SampleNormalizedHeight(Vector2 pos)
	{
		return LowPolyUtils.SampleBilinear(Heights, pos.y, pos.x);
	}

	public float SampleNormalizedHeight(float u, float v)
	{
		return LowPolyUtils.SampleBilinear(Heights, v, u);
	}

	public Color SampleNormalizedColor(float u, float v)
	{
		return LowPolyUtils.SampleBilinear(Colors, v, u);
	}

	public float SampleHeight(float x, float z)
	{
		float u = x / TerrainSize;
		float v = z / TerrainSize;
		float baseHeight = LowPolyUtils.SampleBilinear(Heights, v, u) * TerrainHeight;
		float offset = LowPolyUtils.SampleBilinear(Offsets, v, u).y;
		return baseHeight + offset;
	}

	public Vector3 GetVertex(int x, int y)
	{
		Vector3 offset = Offsets[x, y];

		float u = (float)x / (originalVertPerSide - 1);
		float v = (float)y / (originalVertPerSide - 1);
		float xCoord = u * TerrainSize;
		float zCoord = v * TerrainSize;

		return new Vector3(xCoord + offset.x, Heights[y, x] * TerrainHeight + offset.y, zCoord + offset.z);
	}

	/// <summary>
	/// Initialize the heightmap dimension, prepares the chunk information
	/// </summary>
	/// <param name="aTerrainSize">width/length of the terrain in meters</param>
	/// <param name="aTerrainHeight">height of terrain in meters</param>
	/// <param name="aMaxResolution">the size of the smallest quad in meters</param>
	/// <param name="aChunkSize">The number of chunks per side of the terrain, there will be a total of aChunkSize*aChunkSize separate meshes</param>
	/// <param name="aLODLevels">The number of lod levels to generate</param>
	public void Initialize(int aTerrainSize, int aTerrainHeight, int aMaxResolution, int aChunkSize, int aLODLevels)
	{
		TerrainSize = aTerrainSize;
		TerrainHeight = aTerrainHeight;
		ChunkSize = aChunkSize;
		originalQuadPerSide = aTerrainSize / aMaxResolution;
		originalVertPerSide = originalQuadPerSide + 1;

		int quadPerChunk = aChunkSize / aMaxResolution;

		Chunks = new ChunkSizeInfo[ChunkPerSide, ChunkPerSide];
		for (int chunkX = 0; chunkX < ChunkPerSide; ++chunkX)
		{
			int currentChunkOriginalVertOffsetX = chunkX * aChunkSize / aMaxResolution;
			for (int chunkY = 0; chunkY < ChunkPerSide; ++chunkY)
			{
				int currentChunkOriginalVertOffsetY = chunkY * aChunkSize / aMaxResolution;

				Chunks[chunkX, chunkY] = new ChunkSizeInfo()
				{
					originalVertOffsetX = currentChunkOriginalVertOffsetX,
					originalVertOffsetY = currentChunkOriginalVertOffsetY
				};
			}
		}

		Sizes = new LODSizeInfo[aLODLevels];
		int currentQuadPerSide = quadPerChunk;
		int lodCurrentQuadPerSide = originalQuadPerSide;
		int currentStride = 1;
		for (int i = 0; i < aLODLevels; ++i)
		{
			Sizes[i] = new LODSizeInfo()
			{
				quadPerSide = lodCurrentQuadPerSide,
				quadPerChunkSide = currentQuadPerSide,
				quadCount = currentQuadPerSide * currentQuadPerSide,
				triCount = currentQuadPerSide * currentQuadPerSide * 2,
				vertPerSide = currentQuadPerSide + 1,
				vertCount = currentQuadPerSide * currentQuadPerSide * 6,
				stride = currentStride,
			};

			lodCurrentQuadPerSide /= 2;
			currentQuadPerSide /= 2;
			currentStride *= 2;
		}

		Heights = new float[originalVertPerSide, originalVertPerSide];
		Offsets = new Vector3[originalVertPerSide, originalVertPerSide];
		Colors = new Color[originalVertPerSide, originalVertPerSide];
	}

	#region Serialization Workarounds
	[SerializeField]
	[HideInInspector]
	public ChunkSizeInfo[] _ChunksSerialized;

	[SerializeField]
	[HideInInspector]
	public float[] _HeightsSerialized;

	[SerializeField]
	[HideInInspector]
	public Vector3[] _OffsetsSerialized;

	[SerializeField]
	[HideInInspector]
	public Color32[] _ColorsSerialized;

	/// <summary>
	/// Unity doesn't like 2-dimensional arrays, so we handle them manually
	/// </summary>
	void ISerializationCallbackReceiver.OnBeforeSerialize()
	{
		_ChunksSerialized = new ChunkSizeInfo[ChunkPerSide * ChunkPerSide];
		int counter = 0;
		for (int j = 0; j < ChunkPerSide; ++j)
		{
			for (int i = 0; i < ChunkPerSide; ++i)
			{
				_ChunksSerialized[counter] = Chunks[i, j];
				counter++;
			}
		}

		_HeightsSerialized = new float[originalVertPerSide * originalVertPerSide];
		_OffsetsSerialized = new Vector3[originalVertPerSide * originalVertPerSide];
		_ColorsSerialized = new Color32[originalVertPerSide * originalVertPerSide];
		counter = 0;
		for (int j = 0; j < originalVertPerSide; ++j)
		{
			for (int i = 0; i < originalVertPerSide; ++i)
			{
				_HeightsSerialized[counter] = Heights[i, j];
				_OffsetsSerialized[counter] = Offsets[i, j];
				_ColorsSerialized[counter] = Colors[i, j];
				counter++;
			}
		}
	}

	/// <summary>
	/// Unity doesn't like 2-dimensional arrays, so we handle them manually
	/// </summary>
	void ISerializationCallbackReceiver.OnAfterDeserialize()
	{
		Chunks = new ChunkSizeInfo[ChunkPerSide, ChunkPerSide];
		int counter = 0;
		for (int j = 0; j < ChunkPerSide; ++j)
		{
			for (int i = 0; i < ChunkPerSide; ++i)
			{
				Chunks[i, j] = _ChunksSerialized[counter];
				counter++;
			}
		}

		Heights = new float[originalVertPerSide, originalVertPerSide];
		Offsets = new Vector3[originalVertPerSide, originalVertPerSide];
		Colors = new Color[originalVertPerSide, originalVertPerSide];
		counter = 0;
		for (int j = 0; j < originalVertPerSide; ++j)
		{
			for (int i = 0; i < originalVertPerSide; ++i)
			{
				Heights[i, j] = _HeightsSerialized[counter];
				Offsets[i, j] = _OffsetsSerialized[counter];
				Colors[i, j] = _ColorsSerialized[counter];
				counter++;
			}
		}
		_ChunksSerialized = null;
		_HeightsSerialized = null;
		_OffsetsSerialized = null;
		_ColorsSerialized = null;
	}
	#endregion
}
