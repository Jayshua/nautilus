using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


public static class UnityEditorUtilities
{
#if UNITY_EDITOR
	/// <summary>
	/// Gets the prefab object for the given GameObject.
	/// </summary>
	/// <returns>
	/// The prefab object.
	/// </returns>
	/// <param name='aGameObject'>
	/// A game object.
	/// </param>
	public static Object GetPrefabObject(GameObject aGameObject)
	{
		Object prefab = null;
		if (aGameObject != null)
		{
			// Get the prefab object
			prefab = PrefabUtility.GetPrefabParent(aGameObject);
			if (prefab == null)
			{
				// If that fails, try the root
				prefab = PrefabUtility.FindPrefabRoot(aGameObject);
			}
		}
		return prefab;
	}

	static public string CleanPathForUnity(string aPath)
	{
		// Unity doesn't like backward slashes and remove accidental double slashes
		return aPath.Replace("\\", "/").Replace("//", "/");
	}

	/// <summary>
	/// Takes a full (absolute) system path of a project file
	/// and returns the path relative to the project (starting with 'Asset').
	/// </summary>
	/// <param name="aFullPath">A full (absolute) system path or pathname</param>
	/// <returns>A path relative to the project</returns>
	/// <remarks>The input path is returned if it's not within the project</remarks>
	static public string GetAssetPath(string aFullPath)
	{
		aFullPath = UnityEditorUtilities.CleanPathForUnity(aFullPath);

		// Get the full path of the Unity's project folder (without 'Assets')
		var projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);

		// Take out the project path from the given path
		string retProjectPath = aFullPath;
		if (aFullPath.StartsWith(projectPath))
		{
			retProjectPath = aFullPath.Substring(projectPath.Length);
			retProjectPath = retProjectPath.Replace("\\", "/");
			if (retProjectPath.StartsWith("/"))
			{
				retProjectPath = retProjectPath.Substring(1);
			}
		}

		return retProjectPath;
	}

	/// <summary>
	/// Takes an asset path (starting with 'Assets')
	/// and returns the full (absolute) system path.
	/// </summary>
	/// <param name="aAssetPath">An asset path or pathname</param>
	/// <returns>The full system path</returns>
	/// <remarks>If the asset path is actually a full path, it will be returned as is</remarks>
	static public string GetFullPath(string aAssetPath)
	{
		// Get the full path of the Unity's project folder (without 'Assets')
		var projectPath = System.IO.Path.GetDirectoryName(Application.dataPath);

		// Combine it with the given asset path
		return System.IO.Path.Combine(projectPath, aAssetPath).Replace("\\\\", "/");
	}

	/// <summary>
	/// Creates the given asset folder
	/// and also creates any of the parent folder that doesn't exist.
	/// </summary>
	/// <param name="aPathname">A asset path, or a full path</param>
	/// <param name="aIsFullPath">Whether or not the given path is an asset path (default) or a full path</param>
	/// <remarks>This method is similar to AssetDatabase.CreateFolder() except that it's recursive</remarks>
	static public void CreateAssetFolder(string aPathname, bool aIsFullPath = false)
	{
		// Get the full path and check if the folder exist
		string fullPath = (aIsFullPath ? aPathname : UnityEditorUtilities.GetFullPath(aPathname));
		if (!System.IO.Directory.Exists(fullPath))
		{
			// Get the asset path
			string assetPath = (aIsFullPath ? UnityEditorUtilities.GetAssetPath(aPathname) : aPathname);

			// Get the parent asset path
			var parentPath = System.IO.Path.GetDirectoryName(assetPath);

			// Recursively create the parent folder
			CreateAssetFolder(parentPath, aIsFullPath: false);

			// Create the given folder
			var dirName = System.IO.Path.GetFileName(assetPath);
			AssetDatabase.CreateFolder(parentPath, dirName);
		}
	}

	/// <summary>
	/// Enable (or disable) the read/write on a texture and reimport it's asset
	/// It will checkout the file if needed
	/// </summary>
	public static void EnableTextureReadWrite(string aTextureAssetPathname, bool aReadable = true)
	{
		TextureImporter sourceImporter = (TextureImporter)TextureImporter.GetAtPath(aTextureAssetPathname);
		if (sourceImporter != null)
		{
			if (sourceImporter.isReadable != aReadable)
			{
				UnityEditorUtilities.CheckoutTexture(aTextureAssetPathname, aReadable);
				sourceImporter = (TextureImporter)TextureImporter.GetAtPath(aTextureAssetPathname);
				sourceImporter.isReadable = aReadable;
				sourceImporter.textureCompression = TextureImporterCompression.Uncompressed;
				AssetDatabase.ImportAsset(aTextureAssetPathname, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}
		}
		else
		{
			Debug.Log("Could not find importer for " + aTextureAssetPathname);
		}
	}

	/// <summary>
	/// Enable (or disable) the read/write on a texture and reimport it's asset
	/// It will checkout the file if needed
	/// </summary>
	public static void EnableTextureReadWrite(Texture aTexture, bool aReadable = true)
	{
		if (aTexture != null)
		{
			string texturePath = AssetDatabase.GetAssetPath(aTexture);
			EnableTextureReadWrite(texturePath, aReadable);
		}
	}

	/// <summary>
	/// Enable (or disable) the read/write on a texture and reimport it's asset
	/// It will checkout the file if needed
	/// </summary>
	public static void SetTextureImporterOptions(Texture aTexture, System.Action<TextureImporter> aImporterAction)
	{
		if (aTexture != null)
		{
			string texturePath = AssetDatabase.GetAssetPath(aTexture);
			TextureImporter sourceImporter = (TextureImporter)TextureImporter.GetAtPath(texturePath);
			if (sourceImporter != null)
			{
				UnityEditorUtilities.CheckoutTexture(texturePath);
				sourceImporter = (TextureImporter)TextureImporter.GetAtPath(texturePath);
				aImporterAction(sourceImporter);
				AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}
			else
			{
				Debug.Log("Could not find importer for " + texturePath);
			}
		}
	}

	/// <summary>
	/// Checkout the texture's file asset
	/// </summary>
	public static void CheckoutTexture(string aTextureAssetPathname)
	{
		CheckoutTexture(aTextureAssetPathname, null);
	}

	/// <summary>
	/// Checkout the texture's file asset and optionnaly set its read/write attribute
	/// </summary>
	static void CheckoutTexture(string aTextureAssetPathname, bool? aDesiredTextureReadableState)
	{
		// Check if file is readonly
		if (System.IO.File.Exists(aTextureAssetPathname))
		{
			string fullPathname = UnityEditorUtilities.GetFullPath(aTextureAssetPathname);
			if ((System.IO.File.GetAttributes(fullPathname) & System.IO.FileAttributes.ReadOnly) != 0)
			{
				// This trigger a checkout with P4Connect
				var ti = (TextureImporter)AssetImporter.GetAtPath(aTextureAssetPathname);
				ti.isReadable = !ti.isReadable;
				AssetDatabase.ImportAsset(aTextureAssetPathname, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

				// Restore attribute
				if ((aDesiredTextureReadableState == null) || (ti.isReadable != aDesiredTextureReadableState.Value))
				{
					ti.isReadable = !ti.isReadable;
					AssetDatabase.ImportAsset(aTextureAssetPathname, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
				}
			}
		}
	}
#endif
}
