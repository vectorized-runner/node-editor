using UnityEngine;
using UnityEditor;
using System.IO;

public static class ScriptableObjectUtility
{
	public static ScriptableObject CreateScriptableAsset(string assetName, string className)
	{
		ScriptableObject asset = ScriptableObject.CreateInstance(className);

		return GetScriptableObject(asset, assetName);
	}

	public static ScriptableObject CreateScriptableAsset<T>(string assetName) where T : ScriptableObject
	{
		ScriptableObject asset = ScriptableObject.CreateInstance<T>();

		return GetScriptableObject(asset, assetName);
	}

	static ScriptableObject GetScriptableObject(ScriptableObject asset, string assetName)
	{
		Object active = Selection.activeObject;
		string path = AssetDatabase.GetAssetPath(active);

		if(path == "")
		{
			path = "Assets";
		}
		else if(Path.GetExtension(path) != "")
		{
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
		}

		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + assetName + ".asset");

		AssetDatabase.CreateAsset(asset, assetPathAndName);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow();
		Selection.activeObject = asset;

		return asset;
	}
}