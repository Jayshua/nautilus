using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class, this is put on a Unity Terrain, so we can display a custom editor to "finish editing" the terrain
/// </summary>
public class LowPolyHelper : MonoBehaviour
{
	[HideInInspector]
	public LowPolyTerrain Terrain;
}
