using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

class SaveLoad
{
    public static void SaveFile(string filename, System.Object obj)
    {
		Stream fileStream = null;
        try
        {
            System.Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
//			Debug.Log("Writing Stream to Disk : " + filename);
            fileStream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(fileStream, obj);
            fileStream.Close();
        } catch (System.Exception e)
        {
            Debug.LogWarning("Save.SaveFile(): Failed to serialize object to a file " + filename + " (Reason: " + e.ToString() + ")");
			
		} finally
		{
			fileStream.Close();
		}
    }
	
    public static bool LoadFile<T>(string filename, ref T obj)
    {
        Stream fileStream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Read);
        try
        {
//			Debug.Log("Reading Stream from Disk : " + filename);
            System.Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
            BinaryFormatter formatter = new BinaryFormatter();
            obj = (T)(formatter.Deserialize(fileStream));
            fileStream.Close();
            return true;
        } catch (System.Exception e)
        {
            Debug.LogWarning("SaveLoad.LoadFile(): Failed to deserialize a file " + filename + " (Reason: " + e.ToString() + ")");
		} finally{
			fileStream.Close();
		}
		return false;
    }

    public static bool LoadBytes<T>(byte[] bytes, ref T obj)
    {
        MemoryStream ms = new MemoryStream(bytes);
        try
        {
            System.Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
            BinaryFormatter formatter = new BinaryFormatter();
            obj = (T)(formatter.Deserialize(ms));
            return true;
        } catch (System.Exception e)
        {
            Debug.LogWarning("SaveLoad.LoadFile(): Failed to deserialize a byte[] " + "(Reason: " + e.ToString() + ")");
            return false;
        }       
    }
}