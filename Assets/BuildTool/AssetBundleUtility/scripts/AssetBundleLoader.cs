
using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public class AssetBundleLoader : MonoBehaviour
{

    public static string GetPlatformPath()
    {
        if (IgnorePlatformPath)
            return "";
#if UNITY_IPHONE || UNITY_STANDALONE_OSX
		return "ios/";
#elif UNITY_ANDROID
        return "android/";
#else
		return "win/";
#endif
    }
	
	public enum CacheMode{ NATIVE, LOCAL_SPECIFY }

    public static bool IgnorePlatformPath = false;
    public static string UrlBase = "";
    public static string ResourceBasePath = "OfflineData/";
    public string LocalStoragePath = "";
    public string LocalUrl = "";
    public static string LocalStorageExt = ".bsbpkg";
    public static string AssetInfoFileName = "assetinfo.bin";

    //public static bool SkipLoadedBundle = false;
	public static CacheMode Mode = CacheMode.NATIVE;

    public System.Action<float> AllBundleProgressListener;

    public AssetBundleInfoData Data = new AssetBundleInfoData();

    #region LoadingTask

    class BundleTaskData
    {
        public Dictionary<string, ObjectTaskData> ObjecstList = new Dictionary<string, ObjectTaskData>();
        public System.Action<string, string> reply;
        // bundle name, result desc
    }

    class ObjectTaskData
    {
        public System.Action<string, string, Object> reply;
        // bundle name, result desc, asset
    }

    Dictionary<string, BundleTaskData> TaskQueue = new Dictionary<string, BundleTaskData>();

    //return 0 if asset not found in assetinfo
    //return 1 if need to download
    //return 2 if already downloading
    int _AddObjectTask(string assetName, System.Action<string, string, Object> reply)
    {
        AssetBundleInfoData.AssetObjectInfo info;
        if (!Data.AssetObjectInfos.TryGetValue(assetName, out info))
        {
            Debug.LogError(assetName + " is not in AssetBundleObjects");
            return 0;
        }

        string packname = info.PackName.First();

        BundleTaskData btd;
        if (!TaskQueue.TryGetValue(packname, out btd))
        {
            //new to download
//            print(packname + "downloading...");
            btd = new BundleTaskData();
            ObjectTaskData otd = new ObjectTaskData();
            otd.reply += reply;
            btd.ObjecstList.Add(assetName, otd);
            TaskQueue.Add(packname, btd);
            return 1;
        }
        else
        {
            //downloading
            ObjectTaskData otd;
            if (!btd.ObjecstList.TryGetValue(assetName, out otd))
            {
//                print("add " + assetName);
                otd = new ObjectTaskData();
                otd.reply += reply;
                btd.ObjecstList.Add(assetName, otd);
                return 2;
            }
            else
            {
                otd.reply += reply;
                return 2;
            }
        }
    }

    #endregion

    static AssetBundleLoader _instance;

    public static AssetBundleLoader Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AssetBundleLoader");
                _instance = go.AddComponent<AssetBundleLoader>();
				_instance._initial();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

	private void _initial()
	{
		LocalStoragePath = Application.temporaryCachePath + "/";
		LocalUrl = "File:///" + Application.temporaryCachePath + "/";
	}

    public void LoadTask(string key, System.Action<string, string, Object> OnTaskFinish)
    {
        Object ob = Resources.Load(ResourceBasePath + key);
        if (ob != null && OnTaskFinish != null)
        {
            OnTaskFinish(key, "success", ob);
            return;
        }

        int result = _AddObjectTask(key, OnTaskFinish);
        if (result == 1)
        {//1:start download
			if (Mode == CacheMode.NATIVE) {
				StartCoroutine (_GetBundleOrAssetNative (key, ""));
			} else if (Mode == CacheMode.LOCAL_SPECIFY) {
				StartCoroutine (_GetBundleOrAssetLocalSpecify (key, ""));
			}
        }
        else if (result == 0)
        {//0:no such asset
            OnTaskFinish(key, "fail", null);
        }
        // 2:downloading do nothing
    }


    public void LoadAllBundle(System.Action Done)
    {
        StartCoroutine(_LoadAllBundle(Done));
    }

    HashSet<string> checkLocalAssetBundle()
    {
        HashSet<string> files = new HashSet<string>();

        if (Data.AssetBundleInfos.Count > 0)
        {
            foreach (string s in System.IO.Directory.GetFiles(LocalStoragePath, "*" + LocalStorageExt))
            {
                string temp = s.Replace(LocalStoragePath, "");
                temp = temp.Replace(LocalStorageExt, "");

                //write unused file to nothing
                if (!Data.AssetBundleInfos.ContainsKey(temp))
                {
                    System.IO.File.WriteAllText(s, "");
                }
                files.Add(temp);
            }
        }

        //assetbundles in local
        return files;
    }

    IEnumerator _LoadAllBundle(System.Action Done)
    {
        yield return StartCoroutine(AssetBundleLoader.Instance.UpdateAssetInfo(null));
		
		if (Mode == CacheMode.LOCAL_SPECIFY) 
		{
			checkLocalAssetBundle ();
		}

        foreach (KeyValuePair<string, AssetBundleInfoData.AssetBundleInfo> info in Data.AssetBundleInfos)
        {
            if (/*SkipLoadedBundle &&*/ !info.Value.isNew)
            {
                //pass
            }
            else
            {
				if (Mode == CacheMode.NATIVE)
				{
					yield return StartCoroutine(_GetBundleOrAssetNative("", info.Key));
				}
				else if(Mode == CacheMode.LOCAL_SPECIFY)
				{
					yield return StartCoroutine(_GetBundleOrAssetLocalSpecify("", info.Key));
				}
                
            }
        }
		Data.WriteAssetBundleInfo(AssetBundleLoader.Instance.LocalStoragePath + AssetInfoFileName);

        if (Done != null)
            Done();
    }

	void _loadAsset(string fname, string PackName, WWW www)
	{
		if (fname != "")
		{
			//load asset
			foreach (KeyValuePair<string, ObjectTaskData> otd in TaskQueue[PackName].ObjecstList)
			{
				Object obj = www.assetBundle.LoadAsset(otd.Key);
#if UNITY_EDITOR
				print("assetbundleloader load object : " + otd.Key);
#endif
				if (otd.Value.reply != null)
				{
					otd.Value.reply(otd.Key, "success", obj);
				}
			}
		}
		else
		{
			//load pack only
#if UNITY_EDITOR
			print("assetbundleloader load pack : " + PackName);
#endif
			if (TaskQueue.ContainsKey(PackName) && TaskQueue[PackName].reply != null)
				TaskQueue[PackName].reply(PackName, "success");
		}

		//clear task queue
		if (TaskQueue.ContainsKey(PackName))
			TaskQueue.Remove(PackName);
	}		

	IEnumerator _GetBundleOrAssetNative(string fname, string PackName)
	{
		if (PackName == "")
		{
			PackName = Data.AssetObjectInfos[fname].PackName.First();
		}

		AssetBundleInfoData.AssetBundleInfo info = Data.AssetBundleInfos[PackName];

		StringBuilder sb = new StringBuilder();
		sb.Append(UrlBase);
		sb.Append(GetPlatformPath());
		sb.Append(info.Name + ".unity3d");

		string URL = sb.ToString();
		WWW www = null;
		bool bOK = false;
		while (!bOK)
		{
			www = WWW.LoadFromCacheOrDownload(URL, info.version);
			yield return www;

			if (!System.String.IsNullOrEmpty(www.error))
			{
				Debug.LogError(www.error);
				www.Dispose();
				www = null;
				yield return new WaitForSeconds(2.0f);
			}
			else
			{
				SetProgress(PackName, www.progress);
				if (www.isDone)
				{
					bOK = true;
				}
			}
		}

		SetProgress(PackName, 1.0f);
		
		//already downloaded, mark it
		info.isNew = false;

		_loadAsset(fname, PackName, www);

		www.assetBundle.Unload(false);
		www.Dispose();
		www = null;
	}

	IEnumerator _GetBundleOrAssetLocalSpecify(string fname, string PackName)
	{
		if (PackName == "")
		{
			PackName = Data.AssetObjectInfos[fname].PackName.First();
		}

		AssetBundleInfoData.AssetBundleInfo info = Data.AssetBundleInfos[PackName];

		string localPath = LocalStoragePath + info.Name + LocalStorageExt;

		WWW www = null;
		//if local file exist and no need to update, load it
		if (!info.isNew && System.IO.File.Exists (localPath)) 
		{
#if UNITY_EDITOR
			print("assetbundleloader: load local file " + PackName);
#endif
			www = new WWW (LocalUrl + info.Name + LocalStorageExt);
			yield return www;
		}
		//else download from url
		else
		{
#if UNITY_EDITOR
			print("assetbundleloader : download file " + PackName);
#endif
			StringBuilder sb = new StringBuilder();
			sb.Append(UrlBase);
			sb.Append(GetPlatformPath());
			sb.Append(info.Name + ".unity3d");
			string URL = sb.ToString();
			bool bOK = false;
			while (!bOK)
			{
				www = new WWW(URL);
				yield return www;

				if (!System.String.IsNullOrEmpty(www.error))
				{
					Debug.LogError(www.error);
					www.Dispose();
					www = null;
					yield return new WaitForSeconds(2.0f);
				}
				else
				{
					SetProgress(PackName, www.progress);
					if (www.isDone)
					{
						bOK = true;
					}
				}
			}
			System.IO.File.WriteAllBytes(localPath, www.bytes);
		}

		SetProgress(PackName, 1.0f);

		//already downloaded, mark it
		info.isNew = false;

		_loadAsset(fname, PackName, www);

		www.assetBundle.Unload(false);
		www.Dispose();
		www = null;
	}

//    IEnumerator _GetBundleOrAsset(string fname, string PackName, bool useCaching = false)
//    {
//        if (PackName == "")
//        {
//            PackName = Data.AssetObjectInfos[fname].PackName.First();
//        }
//
//        //download assetbundle to localPath
//        yield return StartCoroutine(_GetBundleAndSave(PackName, BundleStart, BundleFinish, 0));
//
//        AssetBundleInfoData.AssetBundleInfo info = Data.AssetBundleInfos[PackName];
//
//        StringBuilder sb = new StringBuilder();
//        sb.Append(LocalUrl);
//        sb.Append(info.Name + LocalStorageExt);
//
//        string URL = sb.ToString();
//        WWW www = null;
//        bool bOK = false;
//        while (!bOK)
//        {
//            if (useCaching)
//                www = WWW.LoadFromCacheOrDownload(URL, info.version);
//            else
//            {
//                www = new WWW(URL);
//            }
//            yield return www;
//
//            if (!System.String.IsNullOrEmpty(www.error))
//            {
//                Debug.LogError(www.error);
//                www.Dispose();
//                www = null;
//                yield return new WaitForSeconds(2.0f);
//            }
//            else
//            {
//                info.progress = www.progress;
//                if (AllBundleProgressListener != null)
//                    AllBundleProgressListener(GetAllBundleProgress());
//                if (www.isDone)
//                {
//                    bOK = true;
//                }
//            }
//        }
//
//        Data.AssetBundleInfos[PackName].progress = 1.0f;
//
//        if (fname != "")
//        {
//            //load asset
//            foreach (KeyValuePair<string, ObjectTaskData> otd in TaskQueue[PackName].ObjecstList)
//            {
//                Object obj = www.assetBundle.LoadAsset(otd.Key);
//                if (otd.Value.reply != null)
//                {
//                    otd.Value.reply(otd.Key, "success", obj);
//                }
//            }
//        }
//        else
//        {
//            //download only
//            if (TaskQueue.ContainsKey(PackName) && TaskQueue[PackName].reply != null)
//                TaskQueue[PackName].reply(PackName, "success");
//        }
//
//        www.assetBundle.Unload(false);
//        www.Dispose();
//        www = null;
//
//        //clear task queue
//        if (TaskQueue.ContainsKey(PackName))
//            TaskQueue.Remove(PackName);
//    }

//    public void LoadAllBundleAndSave(System.Action Done)
//    {
//        StartCoroutine(_LoadAllBundleAndSave(Done));
//    }
//
//    IEnumerator _LoadAllBundleAndSave(System.Action Done)
//    {
//        yield return StartCoroutine(AssetBundleLoader.Instance.UpdateAssetInfo(null));
//
//		string path = AssetBundleLoader.Instance.LocalStoragePath + AssetInfoFileName;
//        //check local file
//        HashSet<string> files = checkLocalAssetBundle();
//        List<string> allreadyDonloaded = new List<string>();
//
//        foreach (KeyValuePair<string, AssetBundleInfoData.AssetBundleInfo> info in Data.AssetBundleInfos)
//        {
//            if (info.Value.isNew || !files.Contains(info.Key))
//            {
//                
//                yield return StartCoroutine(_GetBundleAndSave(info.Key, BundleStart, BundleFinish, allreadyDonloaded.Count));
//            }
//            else
//            {
//                SetProgress(info.Key, 1.0f);
//            }
//
//            allreadyDonloaded.Add(info.Key);
//            if (allreadyDonloaded.Count % 9 == 0)
//                Data.WriteAssetBundleInfoPartial(path, allreadyDonloaded);
//        }
//        Data.WriteAssetBundleInfo(path);
//
//        if (Done != null)
//            Done();
//    }

    void SetProgress(string packName, float progress)
    {
        if (Data.AssetBundleInfos.ContainsKey(packName))
        {
            Data.AssetBundleInfos[packName].progress = progress;
            if (AllBundleProgressListener != null)
                AllBundleProgressListener(GetAllBundleProgress());
        }
    }

//    IEnumerator _GetBundleAndSave(string PackName, System.Action<string, int> start, System.Action<string, int> finish, int index)
//    {
//
//        string path = LocalStoragePath + PackName + LocalStorageExt;
//
//        if (start != null)
//            start(PackName, index);
//
//        AssetBundleInfoData.AssetBundleInfo info = Data.AssetBundleInfos[PackName];
//        if (!info.isNew && System.IO.File.Exists(path))
//            yield break;
//
//        StringBuilder sb = new StringBuilder();
//        sb.Append(UrlBase);
//        sb.Append(GetPlatformPath());
//        sb.Append(info.Name + ".unity3d");
//        //sb.Append("?t="+Time.time.ToString()+Random.Range(0,10.0f).ToString());
//
//        string URL = sb.ToString();
//        WWW www = null;
//        bool bOK = false;
//        while (!bOK)
//        {
//            www = new WWW(URL);
//            yield return www;
//
////            XMain.instance.assetData[info.Name].Add(www.size.ToString());
//            if (!System.String.IsNullOrEmpty(www.error))
//            {
//                Debug.LogError(www.error);
//                www.Dispose();
//                www = null;
//                yield return new WaitForSeconds(2.0f);
//            }
//            else
//            {
//              
//                SetProgress(PackName, www.progress);
//                if (www.isDone)
//                {
//                    bOK = true;
//                }
//            }
//        }
//
//        SetProgress(PackName, 1.0f);
//
//        System.IO.File.WriteAllBytes(path, www.bytes);
//
//        if (finish != null)
//            finish(PackName, index);
//
//        www.assetBundle.Unload(false);
//        www.Dispose();
//        www = null;
//		//already downloaded, mark it
//		info.isNew = false;
//    }

	public void ReadAssetInfo()
	{
		Data.ReadAssetBundleInfo (LocalStoragePath + AssetInfoFileName);
	}

    public IEnumerator UpdateAssetInfo(System.Action done)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(UrlBase);
        sb.Append(GetPlatformPath());
        sb.Append(AssetInfoFileName);
        sb.Append("?t=" + Time.time.ToString() + Random.Range(0, 10.0f).ToString());

        WWW www = new WWW(sb.ToString());
        bool bOK = false;
        while (!bOK)
        {
            if (!System.String.IsNullOrEmpty(www.error))
            {
                Debug.LogError(UrlBase + GetPlatformPath() + AssetInfoFileName + " AssetInfoUpdate:" + www.error);
                www.Dispose();
                www = null;
                www = new WWW(sb.ToString());
                yield return new WaitForSeconds(2.0f);
            }
            else
            {
                if (www.isDone)
                    bOK = true;
                else
                    yield return null;
            }
        }
        Data.ReadAssetBundleInfo(www.bytes);
		Data.compareAssetBundleInfo(AssetBundleLoader.Instance.LocalStoragePath + AssetInfoFileName);
        www.Dispose();

        if (done != null)
            done();
    }

    bool _checkMD5(byte[] data, string md5)
    {

        MD5 _md5 = MD5.Create();
        byte[] hash = _md5.ComputeHash(data);
        StringBuilder sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < hash.Length; i++)
        {
            sBuilder.Append(hash[i].ToString("x2"));
        }
        return (sBuilder.ToString() == md5);
    }

    public float GetAllBundleProgress()
    {
        float sum = 0.0f;
        foreach (KeyValuePair<string, AssetBundleInfoData.AssetBundleInfo> pair in Data.AssetBundleInfos)
        {
            sum += pair.Value.progress;
        }
        return sum / Data.AssetBundleInfos.Count;
    }



    //    void OnGUI()
    //    {
    //
    //    }

}