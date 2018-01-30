using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
#if UNITY_EDITOR
using UnityEditor;
#endif //UNITY_EDITOR

// TODO!!!
// Lightmap LOD surface distance
// Lightmap resolutions
// Use original mesh for lightmaps
// Verify resolution / lod / dimensions
// center-edge flip
// edge-split for vertical triangles
[SelectionBase]
public class LowPolyTerrain
	: MonoBehaviour
{
#pragma warning disable 0414 // Unused in non-editor mode, but we want to save them for convenience

	public enum HeightmapType
	{
		Bitmap,
		Raw16,
		Raw32,
		Terrain,
	}
	public HeightmapType SourceHeightMapType;
	public Texture2D SourceHeightMap;
	public string  SourceRawHeightMapFile;
	public int RawHeightMapSize;
	public enum ByteOrder
	{
		Windows,
		Mac,
	}
	public ByteOrder RawHeightMapOrder;
	public Terrain SourceTerrain;

	public Texture2D SourceColorMap;
	public Material TerrainMaterial;
	[FormerlySerializedAs("TerrainAlphaMaterialFadeIn")]
	public Material TerrainAlphaMaterial;
	public int TerrainSize = 4096;
	public int TerrainHeight = 1024;
	public int ChunkSize = 256;
	[FormerlySerializedAs("MaxResolution")]
	[FormerlySerializedAs("QuadSize")]
	public int BaseResolution = 8;
	public int UV2Padding = 4;
	[FormerlySerializedAs("RandomOffset")]
	[FormerlySerializedAs("RandomZOffset")]
	public float RandomYOffset = 0.6f;
	[FormerlySerializedAs("RandomXYOffset")]
	public float RandomXZOffset = 0.0f;
	public bool UniformTriangles = false;
	public bool HideChunksInHierarchy = true;
	public int UV2MapSize = 1024;
	public bool GenerateVertColors = true;
	public bool GenerateUV2 = false;
	public bool CastShadows = false;
#pragma warning restore 0414

    [SerializeField]
    [HideInInspector]
    public LowPolyHeightmap Heightmap;

    // Data to handle LOD for all chunks
    public int LODLevels = 5;

	// Pervious version only stored a single distance, but now you can set them by hand
	[SerializeField]
	[HideInInspector]
	[FormerlySerializedAs("LODDistance")]
	float DefaultLODDistance = 300.0f;

	public float LODTransitionTime = 1.0f;
	public float FlipFlopPercent = 0.03f;

	[System.Serializable]
	public class ChunkRuntimeData
	{
		public enum LODState
		{
			Idle = 0,
			Transitionning_In,
			Transitionning_Out,
		}

		public GameObject ChunkRoot;
		public MeshRenderer[] Renderers;

		// The following members are initialized in Start()
		[System.NonSerialized]
		public LODState State;
		[System.NonSerialized]
		public int CurrentLOD;
		[System.NonSerialized]
		public int TargetLOD;
		[System.NonSerialized]
		public float Timer;
		[System.NonSerialized]
		public Vector3 Center;

		public ChunkRuntimeData(GameObject aChunkRoot, MeshRenderer[] aRenderers)
		{
			ChunkRoot = aChunkRoot;
			Renderers = aRenderers;
		}
	}
	
	// LOD Switching distances
	[SerializeField]
	[HideInInspector]
	float[] _Distances;

	// LOD chunks
	[SerializeField]
	[HideInInspector]
	ChunkRuntimeData[] _Chunks;

    // Use this object's position to compute distances
    Transform _CachedCamera;
	Color _MaterialBaseColor;

	// This will store the list of chunks currently switching LOD level
	List<ChunkRuntimeData> _TransitionningChunks;

	public int ChunkPerSide
	{
		get
		{
			return TerrainSize / ChunkSize;
		}
	}

	public int GetChunkIndex(float u, float v)
	{
		int x = Mathf.FloorToInt(u * ChunkPerSide);
		if (x == ChunkPerSide)
			x = ChunkPerSide - 1;
		int y = Mathf.FloorToInt(v * ChunkPerSide);
		if (y == ChunkPerSide)
			y = ChunkPerSide - 1;
		return x * ChunkPerSide + y;
	}

	public ChunkRuntimeData GetChunkForUV(float u, float v)
	{
		return _Chunks[GetChunkIndex(u, v)];
	}

	public ChunkRuntimeData[] Chunks
	{
		get { return _Chunks; }
		set { _Chunks = value; }
	}

	public void SetDistances(float[] aDistances)
	{
		_Distances = aDistances;
	}

	public delegate void OnChunkLODChangeDelegate(ChunkRuntimeData aChunk, int aTargetLOD);

	public OnChunkLODChangeDelegate OnChunkLODChange;

    public float SampleHeight(Vector2 point)
    {
        return SampleHeight(point.x, point.y);
    }

    public float SampleHeight(Vector3 point)
    {
        return SampleHeight(point.x, point.z);
    }

    public float SampleHeight(float x, float z)
    {
        return Heightmap.SampleHeight(x, z);
    }

	void Reset()
	{
#if UNITY_EDITOR
		TerrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/LowPolyTerrain/Materials/VertexColor.mat");
		TerrainAlphaMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/LowPolyTerrain/Materials/VertexColorFade.mat");
#endif
		if (_Distances == null || _Distances.Length == 0)
		{
			GenerateTransitionDistances(DefaultLODDistance);
		}
	}

	// Use this for initialization
	void Awake()
	{
		// Initialize the transition array
		_TransitionningChunks = new List<ChunkRuntimeData>();
	}

	void Start()
	{
		if (_Chunks == null || _Chunks.Length == 0)
		{
			Debug.LogError("No proper chunks defined, you need to generate the terrain");
			return;
		}

		if (_Chunks[0].Renderers.Length != LODLevels)
		{
			Debug.LogError("LOD and renderer count mismatch, you need to regenerate the terrain");
			return;
		}

		_CachedCamera = Camera.main.transform;
		_MaterialBaseColor = TerrainMaterial.GetColor("_Color");

		// Initialize all the chunks
		foreach (var chunk in _Chunks)
		{
			// Remove LOD group
			Object.Destroy(chunk.ChunkRoot.GetComponent<LODGroup>());

			// Setup chunk data
			chunk.State = ChunkRuntimeData.LODState.Idle;
			chunk.Center = chunk.Renderers[0].bounds.center;

			// Compute this chunk's distance to the camera, so we can determine LOD level
			Vector3 deltaToChunk = _CachedCamera.transform.position - chunk.Center;
			deltaToChunk.y = 0.0f;
			float currentDistance = deltaToChunk.magnitude;

			// Enable proper renderer, we do this progressively here...
			chunk.Renderers[0].enabled = true;
			chunk.CurrentLOD = 0;
			for (int i = 1; i < chunk.Renderers.Length; ++i)
			{
				if (currentDistance > _Distances[chunk.CurrentLOD] * (1.0f + FlipFlopPercent))
				{
					chunk.Renderers[i - 1].enabled = false;
					chunk.Renderers[i].enabled = true;

					if (CastShadows)
					{
						chunk.Renderers[i - 1].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
						chunk.Renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
					}

					chunk.CurrentLOD = i;
				}
				else
				{
					chunk.Renderers[i].enabled = false;

					if (CastShadows)
					{
						chunk.Renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					}
				}
			}
			chunk.TargetLOD = chunk.CurrentLOD;

			if (OnChunkLODChange != null)
			{
				OnChunkLODChange(chunk, chunk.TargetLOD);
			}
		}

		// Watch for chunk changes
		StartCoroutine(SlowUpdate());
	}
	
	// Use this for initialization
	void OnEnable()
	{
	}

	// This coroutine checks the chunks and triggers them switching state
	// The actual state switching happens in the Update() method because we want it to happen
	// every frame.
	IEnumerator SlowUpdate()
	{
		System.Action<ChunkRuntimeData, int> StartTransitionTo = (aChunk, aTargetLOD) =>
		{
			// Start a transition
			aChunk.State = ChunkRuntimeData.LODState.Transitionning_In;
			aChunk.TargetLOD = aTargetLOD;
			aChunk.Timer = 0.0f;

			// Enable the target renderer, but make it fully transparent, do this through a property block

			// Build the block
			var color = _MaterialBaseColor;
			color.a = 0.0f;
			var fadeInBlock = new MaterialPropertyBlock();
			fadeInBlock.SetColor("_Color", color);

			// Enable the renderer and set the property block
			aChunk.Renderers[aTargetLOD].enabled = true;
			aChunk.Renderers[aTargetLOD].sharedMaterial = TerrainAlphaMaterial;
			aChunk.Renderers[aTargetLOD].SetPropertyBlock(fadeInBlock);

			//if (CastShadows)
			//{
			//	//chunk.Renderers[chunk.TargetLOD].receiveShadows = true;
			//	aChunk.Renderers[aTargetLOD].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
			//}


			// Update the chunk every frame while it's transitioning
			_TransitionningChunks.Add(aChunk);

			// Trigger delegates, if any
			if (OnChunkLODChange != null)
			{
				OnChunkLODChange(aChunk, aTargetLOD);
			}
		};

		while (true)
		{
			Profiler.BeginSample("FacetedTerrain.SlowUpdate()");

			// Update all the chunks, if necessary
			Vector3 cameraPos = _CachedCamera.transform.position;

			//// For testing transitions
			//int testChunkIndex = TestChunkX * ChunkPerSide + TestChunkY;

			for (int i = 0, count = _Chunks.Length; i < count; ++i)
			{
				var chunk = _Chunks[i];

				// Only consider idle chunks, otherwise things get complicated
				if (chunk.State == ChunkRuntimeData.LODState.Idle)
				{
					// Should we switch?
					int chunkLOD = chunk.CurrentLOD;

					//if (TestChunkTransitions && i == testChunkIndex)
					//{
					//	if (chunk.CurrentLOD != TestChunkLevel)
					//	{
					//		StartTransitonTo(chunk, TestChunkLevel);
					//	}
					//}
					//else
					{
						Vector3 deltaToChunk = cameraPos - chunk.Center;
						deltaToChunk.y = 0.0f;
						float currentDistance = deltaToChunk.magnitude;

						// Check for closer
						if (chunk.CurrentLOD > 0 && currentDistance < _Distances[chunkLOD - 1] * (1.0f - FlipFlopPercent))
						{
							StartTransitionTo(chunk, chunkLOD - 1);
						}

						// Check further
						if (chunkLOD < chunk.Renderers.Length - 1 && currentDistance > _Distances[chunkLOD] * (1.0f + FlipFlopPercent))
						{
							StartTransitionTo(chunk, chunkLOD + 1);
						}
					}
				}
			}

			Profiler.EndSample();
	
			// This is a slow update!!!
			yield return new WaitForSeconds(1.0f / 30.0f);
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (_Chunks == null)
		{
			// This message is printed at startup!
			//Debug.LogError("No proper chunks defined, you need to generate the terrain");
			return;
		}

		Profiler.BeginSample("UpdateChunks()");

		// Iterate the list of chunks transitioning
		for (int i = 0, count = _TransitionningChunks.Count; i < count; ++i)
		{
			var chunk = _TransitionningChunks[i];
			chunk.Timer += Time.deltaTime;
			switch (chunk.State)
			{
				case ChunkRuntimeData.LODState.Transitionning_In:
					{
						// We're fading the target (ie. next) LOD in
						var color = _MaterialBaseColor;
						color.a = Mathf.Clamp01(chunk.Timer / (LODTransitionTime * 0.5f));
						var block = new MaterialPropertyBlock();
						block.SetColor("_Color", color);
						chunk.Renderers[chunk.TargetLOD].SetPropertyBlock(block);

						if (chunk.Timer >= LODTransitionTime * 0.5f)
						{
							// Swap to full opacity for target block
							chunk.Renderers[chunk.TargetLOD].sharedMaterial = TerrainMaterial;
							chunk.Renderers[chunk.TargetLOD].SetPropertyBlock(null);

							// We're done fading in the target LOD, start fading out the current (ie. previous) LOD
							color.a = 1.0f;
							var fadeOutBlock = new MaterialPropertyBlock();
							fadeOutBlock.SetColor("_Color", color);
							chunk.Renderers[chunk.CurrentLOD].sharedMaterial = TerrainAlphaMaterial;
							chunk.Renderers[chunk.CurrentLOD].SetPropertyBlock(fadeOutBlock);

							chunk.State = ChunkRuntimeData.LODState.Transitionning_Out;
							goto case ChunkRuntimeData.LODState.Transitionning_Out;
						}
					}
					break;
				case ChunkRuntimeData.LODState.Transitionning_Out:
					{
						// We're fading the target LOD in
						var color = _MaterialBaseColor;
						color.a = Mathf.Clamp01((LODTransitionTime - chunk.Timer) / (LODTransitionTime * 0.5f));
						var block = new MaterialPropertyBlock();
						block.SetColor("_Color", color);
						chunk.Renderers[chunk.CurrentLOD].SetPropertyBlock(block);

						if (chunk.Timer >= LODTransitionTime)
						{
							// We're done with the fading, clean up!
							chunk.Renderers[chunk.CurrentLOD].enabled = false;
							chunk.Renderers[chunk.CurrentLOD].sharedMaterial = TerrainMaterial;
							chunk.Renderers[chunk.CurrentLOD].SetPropertyBlock(null);

							// Reset the shadow flags
							if (CastShadows)
							{
								//chunk.Renderers[chunk.CurrentLOD].receiveShadows = false;
								chunk.Renderers[chunk.TargetLOD].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
							}

							chunk.State = ChunkRuntimeData.LODState.Idle;
							chunk.CurrentLOD = chunk.TargetLOD;
							chunk.Timer = 0.0f;

							// Remove the chunk from the _Transitioning group
							// Avoid sliding all the elements by just replacing the element with the last one and removing the last one
							if (i != count - 1)
							{
								_TransitionningChunks[i] = _TransitionningChunks[count - 1];
							}
							_TransitionningChunks.RemoveAt(count - 1);
							count--;
						}
					}
					break;
				case ChunkRuntimeData.LODState.Idle:
					// Nothing to do here, we'll check whether we need to transition in the SlowUpdate() method
					break;
			}
		}

		Profiler.EndSample();
	}

	/// <summary>
	/// Update the lod distances
	/// </summary>
	public void UpdateDistances()
	{
		if (_Distances == null || _Distances.Length == 0)
		{
			GenerateTransitionDistances(DefaultLODDistance);
		}
		else
		{
			// Resize the array, trying to keep as many old values as possible
			var newDistances = new float[LODLevels];
			if (_Distances.Length > LODLevels)
			{
				for (int i = 0; i < LODLevels; ++i)
				{
					newDistances[i] = _Distances[i];
				}
			}
			else
			{
				int i = 0;
				for (; i < _Distances.Length; ++i)
				{
					newDistances[i] = _Distances[i];
				}
				float curDist = _Distances[i - 1] * 2.0f;
				for (; i < LODLevels; ++i)
				{
					newDistances[i] = curDist;
					curDist *= 2.0f;
				}
			}

			_Distances = newDistances;
		}
	}

	/// <summary>
	/// When changing the initial transition distance, update all of them
	/// </summary>
	void GenerateTransitionDistances(float aBaseDistance)
	{
		var distances = new float[LODLevels];
		float currentTransitionDistance = aBaseDistance;
		if (currentTransitionDistance == 0.0f)
		{
			currentTransitionDistance = 500.0f;
		}
		for (int i = 0; i < LODLevels; ++i)
		{
			distances[i] = currentTransitionDistance;
			currentTransitionDistance *= 2.0f;
		}
		SetDistances(distances);
	}

	void OnValidate()
	{
		if (_Distances == null || _Distances.Length == 0)
		{
			GenerateTransitionDistances(DefaultLODDistance);
		}
	}

}
