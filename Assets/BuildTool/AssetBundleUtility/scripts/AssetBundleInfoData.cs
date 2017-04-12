using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Security.Cryptography;
#endif
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

public class AssetBundleInfoData
{

    public int version;

    [System.Serializable()]
    public class AssetBundleInfo
    {

        public 	string Name = "";
        public 	int version = 0;
        public  bool isNew = false;
        public	float progress = 0.0f;

        [System.NonSerialized()]
        public	HashSet<string>	objs = new HashSet<string>();

        public AssetBundleInfo()
        {
        }

        public AssetBundleInfo(BundleSerializeData sdata)
        {
            Name = sdata.Name;
            version = sdata.version;
            isNew = sdata.isNew;
            progress = sdata.progress;
        }

		
        public static bool checkEqual(AssetBundleInfoData data1, AssetBundleInfo info1, AssetBundleInfoData data2, AssetBundleInfo info2)
        {
            if (info1.objs.Count != info2.objs.Count)
                return false;
            foreach (string name in info1.objs)
            {
                if (info2.objs.Contains(name))
                {
                    if (data1.GetMD5(name) != data2.GetMD5(name))
                        return false;
                }
                else
                    return false;
            }
            return true;
        }
    }

    [System.Serializable()]
    public class AssetObjectInfo
    {
        public 	string Name = "";
        public 	string	MD5 = "";
        public 	int version = 0;

        [System.NonSerialized()]
        public 	HashSet<string>	PackName = new HashSet<string>();
        public	Object	obj;

        public AssetObjectInfo()
        {
        }

        public AssetObjectInfo(ObjectSerializeData osd)
        {
            Name = osd.Name;
            MD5 = osd.MD5;
            foreach (string s in osd.PackNames)
            {
                PackName.Add(s);
            }
        }

        public bool Equals(AssetObjectInfo info)
        {
            return (MD5 == info.MD5);
        }

        public static bool operator ==(AssetObjectInfo info1, AssetObjectInfo info2)
        {
            return (info1.MD5 == info2.MD5);
        }

        public static bool operator !=(AssetObjectInfo info1, AssetObjectInfo info2)
        {
            return (info1.MD5 != info2.MD5);
        }
    }

    [System.Serializable]
    public class BundleSerializeData
    {
        public 	string Name = "";
        public 	int version = 0;
        public  bool isNew = false;
        public	float progress = 0.0f;

        public BundleSerializeData(AssetBundleInfo info)
        {
            Name = info.Name;
            version = info.version;
            isNew = info.isNew;
            progress = info.progress;
        }
    }

    [System.Serializable]
    public class ObjectSerializeData
    {
        public 	string Name = "";
        public 	string MD5 = "";
        public	List<string> PackNames = new List<string>();

        public ObjectSerializeData(AssetObjectInfo info)
        {
            Name = info.Name;
            MD5 = info.MD5;
            foreach (string s in info.PackName)
            {
                PackNames.Add(s);
            }
        }
    }

    [System.Serializable]
    class SerializeData
    {
        public int Version;
        public List<BundleSerializeData> bundles = new List<BundleSerializeData>();
        public List<ObjectSerializeData> objects = new List<ObjectSerializeData>();
    }

    public Dictionary<string, AssetObjectInfo> AssetObjectInfos = new Dictionary<string, AssetObjectInfo>();
    public Dictionary<string, AssetBundleInfo> AssetBundleInfos = new Dictionary<string, AssetBundleInfo>();

    bool IsNewBundle(string bundleName, AssetBundleInfoData old)
    {
        if (!old.AssetBundleInfos.ContainsKey(bundleName))
		{
#if UNITY_EDITOR
			MonoBehaviour.print("new bundle : " + bundleName + ", ver " + AssetBundleInfos[bundleName].version);
#endif
            return true;
        }
        else if (!AssetBundleInfos.ContainsKey(bundleName))
            return false;
        else if (old.AssetBundleInfos[bundleName].version < AssetBundleInfos[bundleName].version)
        {
#if UNITY_EDITOR
			MonoBehaviour.print("new bundle : " + bundleName + ", ver " + AssetBundleInfos[bundleName].version);
#endif
            return true;
        }
        return false;
    }

    public string GetMD5(string Name)
    {
        return AssetObjectInfos[Name].MD5;
    }

    public AssetBundleInfo[] UpdateAll(AssetBundleInfoData prevData)
    {
        foreach (KeyValuePair<string, AssetBundleInfo> pair in AssetBundleInfos)
        {
            if (prevData.AssetBundleInfos.ContainsKey(pair.Key))
            {
                pair.Value.version = prevData.AssetBundleInfos[pair.Key].version + 1;
            }
        }
		
        foreach (KeyValuePair<string, AssetObjectInfo> pair in AssetObjectInfos)
        {
            if (prevData.AssetObjectInfos.ContainsKey(pair.Key))
            {
                pair.Value.version = prevData.AssetObjectInfos[pair.Key].version + 1;
            }
        }

        version = prevData.version + 1;
		
        return AssetBundleInfos.Values.ToArray();
    }

    public AssetBundleInfo[] UpdateVersion(AssetBundleInfoData prevData)
    {
		
        List<AssetBundleInfo> UpdatedAssetbundle = new List<AssetBundleInfo>();
		
        foreach (KeyValuePair<string, AssetBundleInfo> pair in AssetBundleInfos)
        {
            if (prevData.AssetBundleInfos.ContainsKey(pair.Key))
            {
                AssetBundleInfo info2 = prevData.AssetBundleInfos[pair.Key];
                if (!AssetBundleInfo.checkEqual(this, pair.Value, prevData, info2))
                {
                    pair.Value.version = info2.version + 1;
                    UpdatedAssetbundle.Add(pair.Value);
                }
                else
                    pair.Value.version = info2.version;
            }
            else
                UpdatedAssetbundle.Add(pair.Value);
        }
		
        foreach (KeyValuePair<string, AssetObjectInfo> pair in AssetObjectInfos)
        {
            if (prevData.AssetObjectInfos.ContainsKey(pair.Key))
            {
                AssetObjectInfo info2 = prevData.AssetObjectInfos[pair.Key];
                if (pair.Value != info2)
                {
                    pair.Value.version = info2.version + 1;
                }
                else
                    pair.Value.version = info2.version;
            }
        }

        if (UpdatedAssetbundle.Count > 0)
            version = prevData.version + 1;
        else
            version = prevData.version;
		
        return UpdatedAssetbundle.ToArray();
    }

    public void ClearData()
    {
        foreach (KeyValuePair<string, AssetObjectInfo> objinfo in AssetObjectInfos)
        {
            objinfo.Value.obj = null;
        }
        AssetBundleInfos.Clear();
        AssetObjectInfos.Clear();
    }

    public void compareAssetBundleInfo(string oldfilename)
    {
        if (AssetBundleInfos.Count == 0 && AssetObjectInfos.Count == 0)
        {
            Debug.LogError("AssetBundleInfoData is not ready for compare!");
            return;
        }
        AssetBundleInfoData old = new AssetBundleInfoData();
        //if error recreate info file
        if (!old.ReadAssetBundleInfoFile(oldfilename))
        {
            Debug.LogWarning("recreate empty assetinfo");
            recreatEmptyAssetinfo(oldfilename);
            old.ReadAssetBundleInfoFile(oldfilename);
        }
        foreach (KeyValuePair<string, AssetBundleInfo> pair in AssetBundleInfos)
        {
            pair.Value.isNew = IsNewBundle(pair.Key, old);
        }
    }
	
    //read AssetBundle info from file to AssetBundleInfos dictionary
    public bool ReadAssetBundleInfoFile(string path)
    {
        if (!File.Exists(path))
        {
            CreateEmptyAssetinfo(path);
        }

        return ReadAssetBundleInfo(path);
    }

	public void ResetLocalAssetinfo()
    {
		_ResetLocalAssetinfo(AssetBundleLoader.Instance.LocalStoragePath + AssetBundleLoader.AssetInfoFileName);
    }

    void _ResetLocalAssetinfo(string filename)
    {
        if (!File.Exists(filename))
            CreateEmptyAssetinfo(filename);
        else
            recreatEmptyAssetinfo(filename);
    }

    public void CreateEmptyAssetinfo(string filename)
    {
        SerializeData sdata = new SerializeData();
        SaveLoad.SaveFile(filename, sdata);
    }

    public void recreatEmptyAssetinfo(string filename)
    {
        SerializeData sdata = new SerializeData();
        SaveLoad.SaveFile(filename, sdata);
    }

    //run Time usage
    public bool ReadAssetBundleInfo(byte[] bytes)
    {
        ClearData();
        try
        {
            SerializeData sdata = new SerializeData();
            if (!SaveLoad.LoadBytes<SerializeData>(bytes, ref sdata))
            {
                return false;
            }
			
            _SetAssetInfoData(sdata);
        }
        catch (System.Exception e)
        {
            Debug.LogError("local assetinfo format error");
            return false;
        }
		
        return true;
    }


    public bool ReadAssetBundleInfo(string filename)
    {
        ClearData();
        try
        {
            SerializeData sdata = new SerializeData();
            if (!SaveLoad.LoadFile<SerializeData>(filename, ref sdata))
            {
                return false;
            }

            _SetAssetInfoData(sdata);
        }
        catch (System.Exception e)
        {
            Debug.LogError("local assetinfo format error");
            return false;
        }

        return true;
    }

    void _SetAssetInfoData(SerializeData sdata)
    {
        version = sdata.Version;
		
        foreach (BundleSerializeData bsd in sdata.bundles)
        {
            AssetBundleInfo info = new AssetBundleInfo(bsd);
            AssetBundleInfos.Add(info.Name, info);
        }

        foreach (ObjectSerializeData osd in sdata.objects)
        {
            AssetObjectInfo info = new AssetObjectInfo(osd);
            AssetObjectInfos.Add(info.Name, info);
            foreach (string packname in info.PackName)
            {
                if (AssetBundleInfos.ContainsKey(packname))
                {
                    AssetBundleInfos[packname].objs.Add(info.Name);
                }
            }
        }
    }

    public void WriteAssetBundleInfoPartial(string path, List<string> partialList)
    {
        SerializeData sdata = new SerializeData();
		
        sdata.Version = version;
		
        foreach (string s in partialList)
        {
            BundleSerializeData bsd = new BundleSerializeData(AssetBundleInfos[s]);
            sdata.bundles.Add(bsd);
        }
		
        SaveLoad.SaveFile(path, sdata);
    }

    public void WriteAssetBundleInfo(string path)
    {
        SerializeData sdata = new SerializeData();

        sdata.Version = version;

        foreach (KeyValuePair<string, AssetBundleInfo> pair in AssetBundleInfos)
        {
            BundleSerializeData bsd = new BundleSerializeData(pair.Value);
            sdata.bundles.Add(bsd);
        }

        foreach (KeyValuePair<string, AssetObjectInfo> pair in AssetObjectInfos)
        {
            ObjectSerializeData osd = new ObjectSerializeData(pair.Value);
            sdata.objects.Add(osd);
        }

        SaveLoad.SaveFile(path, sdata);

//		StringBuilder sb = new StringBuilder();
//		sb.Append("<<Version>>" + version.ToString());
//
//		sb.Append("<<AssetInfo>>");
//		if(AssetBundleInfos.Count > 0)
//		{
//			foreach( KeyValuePair<string, AssetBundleInfo> info in AssetBundleInfos ){
//				sb.AppendFormat("{0},{1}#", info.Value.Name, info.Value.version);
//			}
//			sb.Remove(sb.Length-1, 1);
//		}
//
//		sb.Append("<<PackInfo>>");
//		if(AssetObjectInfos.Count > 0)
//		{
//			foreach( KeyValuePair<string, AssetObjectInfo> info in AssetObjectInfos ){
//				string packNames = "";
//				foreach(string s in info.Value.PackName){
//					packNames += (s + "+");
//				}
//				packNames = packNames.Substring(0, packNames.Length-1);
//				
//				sb.AppendFormat("{0},{1},{2},{3}#", info.Value.Name, packNames, info.Value.version, info.Value.MD5);
//			}
//			sb.Remove(sb.Length-1, 1);
//		}
//		
//		using( StreamWriter fileWriter = new StreamWriter(path+"assetinfo.txt") ){
//			fileWriter.Write(sb.ToString());
//		}
    }

    #if UNITY_EDITOR
    public void AddAssetObjectInfo(string ObjectName, string PackName, Object obj)
    {
        AssetBundleInfoData.AssetObjectInfo info = null;
        if (AssetObjectInfos.ContainsKey(ObjectName))
            info = AssetObjectInfos[ObjectName];
        else
            info = new AssetObjectInfo();
        info.Name = ObjectName;
        info.PackName.Add(PackName);
        info.obj = obj;
        info.MD5 = GetMD5HashFromFile(AssetDatabase.GetAssetPath(obj));
		
        if (AssetObjectInfos.ContainsKey(ObjectName))
        {
            if (AssetObjectInfos[ObjectName].obj != obj)
            {
                Debug.LogError("Adding different object with the same name " + info.Name + " to Assetbundle builder, please try different name. ");
                return;
            }
        }
        else
            AssetObjectInfos.Add(ObjectName, info);
		
        if (System.String.IsNullOrEmpty(PackName))
        {
            AssetBundleInfo Ainfo = new AssetBundleInfo();
            Ainfo.Name = info.Name;
            Ainfo.objs.Add(info.Name);
            AssetBundleInfos.Add(Ainfo.Name, Ainfo);
        }
        else
        {
            if (AssetBundleInfos.ContainsKey(PackName))
            {
                AssetBundleInfos[PackName].objs.Add(ObjectName);
            }
            else
            {
                AssetBundleInfo Ainfo = new AssetBundleInfo();
                Ainfo.Name = PackName;
                Ainfo.objs.Add(info.Name);
                AssetBundleInfos.Add(Ainfo.Name, Ainfo);
            }
        }
    }

    static string GetMD5HashFromFile(string fileName)
    {
        StreamReader file = new StreamReader(fileName);
        MD5 md5 = MD5.Create();
		
        byte[] hash = md5.ComputeHash(file.BaseStream);
        StringBuilder sBuilder = new StringBuilder();
	
        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < hash.Length; i++)
        {
            sBuilder.Append(hash[i].ToString("x2"));
        }
        // Return the hexadecimal string.
        return sBuilder.ToString();
		
    }
    #endif

}
