using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[CustomEditor(typeof(AssetBundleBuilder))] 
public class AssetBundleBuilderInspector : Editor {
	
#if UNITY_EDITOR
	 public override void OnInspectorGUI () {
		base.OnInspectorGUI();
		AssetBundleBuilder myTarget = (AssetBundleBuilder)target;
		
		bool rdyForBuild = true;
		bool bOk = false;

		if (GUILayout.Button("select resource path"))
		{
			string tempPath = EditorUtility.OpenFolderPanel("select an resource folder", "./", "*.*");
			int idx = tempPath.LastIndexOf("Assets");
			if(idx < 0)
				Debug.LogError("please select a folder in unity project");
			else
				myTarget.ResourcePath = tempPath.Substring(idx, tempPath.Length-idx) + "/";
		}

		if(GUILayout.Button("GenerateByteFile"))
		{
			myTarget.convertTextureToByte();
		}

		if (GUILayout.Button("Generate SpritePacks"))
		{
			myTarget.GenerateSpritePacks();
		}
		
		GUI.color = ( bOk = (!System.String.IsNullOrEmpty(myTarget.FromPath)) )? Color.green : Color.red;
		rdyForBuild = rdyForBuild && bOk;
		if (GUILayout.Button("select folder to build from"))
		{
			string tempPath = EditorUtility.OpenFolderPanel("select an asset folder", "./", "*.*");
			int idx = tempPath.LastIndexOf("Assets");
			if(idx < 0)
				Debug.LogError("please select a folder in unity project");
			else
				myTarget.FromPath = tempPath.Substring(idx, tempPath.Length-idx) + "/";
   		}
		
		GUI.color = ( bOk = (!System.String.IsNullOrEmpty(myTarget.BuiltPath)) )? Color.green : Color.red;
		rdyForBuild = rdyForBuild && bOk;
		if (GUILayout.Button("select folder for desination"))
		{
			string tempPath = EditorUtility.OpenFolderPanel("select an asset folder", "./", "*.*");
			int idx = tempPath.LastIndexOf("Assets");
			if(idx < 0)
				Debug.LogError("please select a folder in unity project");
			else
				myTarget.BuiltPath = tempPath.Substring(idx, tempPath.Length-idx) + "/";
   		}
		
		GUI.color = rdyForBuild ? Color.green : Color.red;
		if (GUILayout.Button("Build Asset Bundle"))
		{
			buildByPlatform(false);
   		}
		
		GUI.color = rdyForBuild ? Color.green : Color.red;
		if (GUILayout.Button("Force Build All"))
		{
			buildByPlatform(true);
   		}

		if(GUILayout.Button("View AssetBundleInfo"))
		{
			string path = myTarget.BuiltPath + AssetBundleLoader.GetPlatformPath();

			AssetBundleInfoData data = new AssetBundleInfoData();
			myTarget.AssetBundleInfos = new List<AssetBundleInfoData.AssetBundleInfo>();
			myTarget.AssetObjectInfos = new List<AssetBundleInfoData.AssetObjectInfo>();

			data.ReadAssetBundleInfo( path + AssetBundleLoader.AssetInfoFileName );
			foreach( KeyValuePair< string, AssetBundleInfoData.AssetBundleInfo > pair in data.AssetBundleInfos)
			{
				myTarget.AssetBundleInfos.Add( pair.Value );
			}

			foreach( KeyValuePair< string, AssetBundleInfoData.AssetObjectInfo > pair in data.AssetObjectInfos)
			{
				myTarget.AssetObjectInfos.Add( pair.Value );
			}

			myTarget.version = data.version;
		}
    }
	
	void buildByPlatform(bool foraceAll)
	{
		AssetBundleBuilder myTarget = (AssetBundleBuilder)target;
		string path = myTarget.BuiltPath + AssetBundleLoader.GetPlatformPath();
		AssetBundleBuilder.BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget, path, myTarget.FromPath, foraceAll, myTarget.Data, myTarget.CurData);
		myTarget.setAssetInfo (myTarget.Data);
	}
#endif
}
