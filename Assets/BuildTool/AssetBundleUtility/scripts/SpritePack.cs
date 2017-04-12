using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpritePack : MonoBehaviour
{

    public const string AssetBundle_AdditionWord = "_Pack";
    public Sprite[] Sprites;
    Dictionary<string, Sprite> metaData = new Dictionary<string, Sprite>();
    bool init = false;

    public static string ResourceBasePath = "OfflineData/";

    void Initial()
    {
        foreach (Sprite sp in Sprites)
        {
            if (!metaData.ContainsKey(sp.name))
            {
                metaData.Add(sp.name, sp);
            }
            else
            {
                Debug.LogError("duplicate key : " + sp.name + " in " + this.gameObject.name);
                metaData[sp.name] = sp;
            }
        }
        init = true;
    }

    public Sprite GetSprite(string name)
    {
        if (!init)
            Initial();

        if (metaData.ContainsKey(name))
        {
            return metaData[name];
        } 

        string dePackName = name.Replace(AssetBundle_AdditionWord, "");
        if (metaData.ContainsKey(dePackName))
        {
            return metaData[dePackName];
        }
        if (name.Contains(AssetBundle_AdditionWord))
        {
            return metaData["f9999"];
        }
        else
        {
            if (metaData.ContainsKey("z9999_icon"))
            {
                return metaData["z9999_icon"];
            }
            else
            {
                return null;
            }
        }
        return null;
    }

    public Texture2D GetTexture(string name)
    {
        return textureFromSprite(GetSprite(name));
    }

    public  Texture2D textureFromSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            sprite = RandomSpriteName();
        }
        if (sprite.rect.width != sprite.texture.width)
        {
            Texture2D newText = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, TextureFormat.ARGB32, false);
            Color[] newColors = sprite.texture.GetPixels((int)sprite.textureRect.x, 
                                    (int)sprite.textureRect.y, 
                                    (int)sprite.textureRect.width, 
                                    (int)sprite.textureRect.height);
            newText.filterMode = FilterMode.Bilinear;
            newText.SetPixels(newColors);
            newText.Apply();
            return newText;
        }
        else
            return sprite.texture;
    }

    public static void LoadSingleSprite(string spriteName, System.Action<string, string, Object> OnFinish)
    {
        AssetBundleLoader.Instance.LoadTask(spriteName + AssetBundle_AdditionWord, OnFinish);
    }

    public static Sprite CreateFromObject(Object obj, string name)
    {
        if (obj == null)
        {
            obj = Resources.Load(ResourceBasePath + "f9999_Pack");
        }
        return ((GameObject)obj).GetComponent<SpritePack>().GetSprite(name);
    }

    public Sprite RandomSpriteName()
    {
        return Sprites[Random.Range(0, Sprites.Length - 1)];
    }
}
