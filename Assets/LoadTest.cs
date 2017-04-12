using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoadTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		//where assetinfo will be
		print (AssetBundleLoader.Instance.LocalStoragePath);
		//on stand alone windows, assetbundle cache will be C:\Users\Username\AppData\LocalLow\Unity\WebPlayer\Cache
		
		//setting properties
	    //default cache mode : NATIVE
		AssetBundleLoader.Mode = AssetBundleLoader.CacheMode.NATIVE;
		//for NATIVE, we need to set caching properties.
		//make cache compressed and save about 2/3 size.
		Caching.compressionEnabled = true;
		//set a maximum cache size to prevent cache size grow up after update over and over.
		Caching.maximumAvailableDiskSpace = 100 * 1024 * 1024; //number of bytes

		//if you choose to use LOCALSPECIFY, you don't need above settings, but first loading of resource is slower than NATIVE.
		//and local file size is just what you downloaded.
		
	    //set source file url
		AssetBundleLoader.UrlBase = @"file://F:/AssetbundleTool/Assets/BuildTool/BuiltAssetBundles/";
		
		//update and get new file
		AssetBundleLoader.Instance.LoadAllBundle (Done);
	}
				

	void Done()
	{
		print ("update assetinfo done");
		//test multiple load
		AssetBundleLoader.Instance.LoadTask ("a1", finished);
		AssetBundleLoader.Instance.LoadTask ("a1", finished);
		AssetBundleLoader.Instance.LoadTask ("a1", finished);
		AssetBundleLoader.Instance.LoadTask ("a1", finished);
		AssetBundleLoader.Instance.LoadTask ("a1", finished);

		AssetBundleLoader.Instance.LoadTask ("a2", finished);
		AssetBundleLoader.Instance.LoadTask ("a3", finished);
	}

	void finished(string name, string result, Object obj)
	{
		print (name + " " + result);
	}
	
	// Update is called once per frame
	void Update () {
	
	}


}
