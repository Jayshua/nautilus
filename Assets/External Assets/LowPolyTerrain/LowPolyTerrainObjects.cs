using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;


[RequireComponent(typeof(LowPolyTerrain))]
public class LowPolyTerrainObjects
	: MonoBehaviour
{
#pragma warning disable 0414
	public GameObject[] ObjectPrefabs;
	public Texture2D ObjectPlacementMap;
	public float ObjectProbabilityScale = 0.1f;
	public float ObjectHeightOffset = 0.2f;
	public float ObjectScaleMin = 0.9f;
	public float ObjectScaleMax = 1.1f;
#pragma warning restore 0414

	public float LODDistance = 1000.0f;
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

		public GameObject ObjectRoot;
		public List<GameObject> Objects;

		[System.NonSerialized]
		public LODState State;

		[System.NonSerialized]
		public float Timer;

		public bool CurrentActive
		{
			get { return ObjectRoot.activeSelf; }
		}

		[System.NonSerialized]
		public bool TargetActive;

		[System.NonSerialized]
		public List<GameObject> HiddenObjects;

		public ChunkRuntimeData(GameObject aObjectRoot)
		{
			ObjectRoot = aObjectRoot;
			Objects = new List<GameObject>();
		}
	}

	[System.Serializable]
	public struct ChunkPair
	{
		public LowPolyTerrain.ChunkRuntimeData TerrainChunk;
		public ChunkRuntimeData ObjectChunk;
		public ChunkPair(LowPolyTerrain.ChunkRuntimeData aTerrainChunk, ChunkRuntimeData aObjectChunk)
		{
			TerrainChunk = aTerrainChunk;
			ObjectChunk = aObjectChunk;
		}
	}

	public  List<ChunkRuntimeData> StoredChunks
	{
		get { return _StoredChunks; }
		set { _StoredChunks = value; _Chunks = null; }
	}

	// LOD chunks
	[SerializeField]
	[HideInInspector]
	List<ChunkRuntimeData> _StoredChunks;

	// Initialized in Awake
	Dictionary<LowPolyTerrain.ChunkRuntimeData, ChunkRuntimeData> _Chunks;

	// This will store the list of chunks currently switching LOD level
	List<ChunkRuntimeData> _TransitionningChunks;
	Transform _CachedCamera;

	void Start()
	{
		if (_StoredChunks == null)
		{
			return;
		}

		_CachedCamera = Camera.main.transform;

		var terrain = GetComponent<LowPolyTerrain>();
		if (terrain != null)
		{
			_Chunks = new Dictionary<LowPolyTerrain.ChunkRuntimeData, ChunkRuntimeData>();

			for (int i = 0, count = _StoredChunks.Count; i < count; ++i)
			{
				var storedChunk = _StoredChunks[i];
				storedChunk.State = ChunkRuntimeData.LODState.Idle;
				storedChunk.Timer = 0.0f;
				storedChunk.HiddenObjects = new List<GameObject>();

				// Compute this chunk's distance to the camera, so we can determine LOD level
				Vector3 deltaToChunk = _CachedCamera.transform.position - terrain.Chunks[i].Center;
				deltaToChunk.y = 0.0f;
				float currentDistance = deltaToChunk.magnitude;

				// Initialize active/inactive
				if (currentDistance > LODDistance * (1.0f + FlipFlopPercent))
				{
					storedChunk.ObjectRoot.SetActive(false);
				}
				storedChunk.TargetActive = storedChunk.CurrentActive;

				_Chunks.Add(terrain.Chunks[i], storedChunk);
			}

			// Initialize the transition array
			_TransitionningChunks = new List<ChunkRuntimeData>();
		}

		// Watch for chunk changes
		StartCoroutine(SlowUpdate());
	}

	// This coroutine checks the chunks and triggers them switching state
	// The actual state switching happens in the Update() method because we want it to happen
	// every frame.
	IEnumerator SlowUpdate()
	{
		System.Action<ChunkRuntimeData, bool> StartTransitionTo = (chunk, active) =>
			{
				if (!chunk.CurrentActive)
				{
					// Before we start to add objects in, we must turn them all off
					for (int j = 0, objCount = chunk.Objects.Count; j < objCount; ++j)
					{
						var obj = chunk.Objects[j];
						obj.SetActive(false);
						chunk.HiddenObjects.Add(obj);
					}
					chunk.Objects.Clear();
					chunk.ObjectRoot.SetActive(true);
					chunk.Timer = 0.0f;
				}
				chunk.TargetActive = active;
				_TransitionningChunks.Add(chunk);
			};

		while (true)
		{
			Profiler.BeginSample("FacetedTerrain.SlowUpdate()");

			// Update all the chunks, if necessary
			Vector3 cameraPos = _CachedCamera.transform.position;

			foreach (var chunkPair in _Chunks)
			{
				// Only consider idle chunks, otherwise things get complicated
				var chunk = chunkPair.Value;
				var terrainChunk = chunkPair.Key;
				if (chunk.State == ChunkRuntimeData.LODState.Idle)
				{
					// Should we switch?
					bool chunkActive = chunk.ObjectRoot.activeSelf;

					Vector3 deltaToChunk = cameraPos - terrainChunk.Center;
					deltaToChunk.y = 0.0f;
					float currentDistance = deltaToChunk.magnitude;

					// Check for closer
					if (!chunkActive && currentDistance < LODDistance * (1.0f - FlipFlopPercent))
					{
						StartTransitionTo(chunk, true);
					}

					// Check further
					if (chunkActive && currentDistance > LODDistance * (1.0f + FlipFlopPercent))
					{
						StartTransitionTo(chunk, false);
					}
				}
			}

			Profiler.EndSample();

			// This is a slow update!!!
			yield return new WaitForSeconds(1.0f / 30.0f);
		}
	}

	void Update()
	{
		if (_TransitionningChunks == null)
		{
			return;
		}

		for (int i = 0, count = _TransitionningChunks.Count; i < count; ++i)
		{
			var chunk = _TransitionningChunks[i];
			chunk.Timer += Time.deltaTime;

			if (chunk.Timer > LODTransitionTime)
			{
				// Swap the root instead of individual objects
				// Reset the objects
				for (int j = 0, objCount = chunk.HiddenObjects.Count; j < objCount; ++j)
				{
					var obj = chunk.HiddenObjects[j];
					obj.SetActive(true);
					chunk.Objects.Add(obj);
				}
				chunk.HiddenObjects.Clear();
				chunk.ObjectRoot.SetActive(chunk.TargetActive);

				// And remove this from the list of transitionning chunks
				_TransitionningChunks.RemoveAt(i);
				count--;
			}
			else
			{
				float percent = chunk.Timer / LODTransitionTime;

				// Determine how many objects we need to turn on/off this update
				int totalObjects = chunk.Objects.Count + chunk.HiddenObjects.Count;
				int currentTargetCount = chunk.TargetActive ? chunk.Objects.Count : chunk.HiddenObjects.Count;
				int targetTargetCount = Mathf.CeilToInt(totalObjects * percent);

				List<GameObject> poolFrom = chunk.TargetActive ? chunk.HiddenObjects : chunk.Objects;
				List<GameObject> poolTo = chunk.TargetActive ? chunk.Objects : chunk.HiddenObjects;
				for (int j = 0, thisFrameCount = targetTargetCount - currentTargetCount; j < thisFrameCount; ++j)
				{
					// Pick a random object from the current pool
					int poolCount = poolFrom.Count;
					int objIndex = Random.Range(0, poolCount);

					// Remove it from the pool and add it to the other pool
					var obj = poolFrom[objIndex];
					poolFrom[objIndex] = poolFrom[poolCount - 1];
					poolFrom.RemoveAt(poolCount - 1);
					poolCount--;

					// Flip it
					obj.SetActive(chunk.TargetActive);

					poolTo.Add(obj);
				}
			}
		}
	}

	public void ClearObjects()
	{
		// Remove previous objects, if any
		if (StoredChunks != null)
		{
			foreach (var chunk in StoredChunks)
			{
				foreach (var terrainObj in chunk.Objects)
				{
					Object.DestroyImmediate(terrainObj);
				}
				chunk.Objects.Clear();
			}
		}

		// These guys will keep track of matching chunks to terrain chunks
		StoredChunks = null;
	}
}
