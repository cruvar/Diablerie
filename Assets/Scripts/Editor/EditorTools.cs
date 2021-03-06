﻿using System.IO;
using UnityEngine;
using UnityEditor;

public class EditorTools
{
    [MenuItem("Assets/Load DS1")]
    static public void LoadDS1()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        DT1.ResetCache();
        DS1.Load(assetPath);
    }

    [MenuItem("Assets/Load DS1", true)]
    static public bool LoadDS1Validate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("ds1");
    }

    [MenuItem("Assets/Convert DT1 to PNG")]
    static public void ConvertDT1ToPNG()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        Palette.LoadPalette(0);
        var dt1 = DT1.Load(assetPath);
        int i = 0;
        foreach (var texture in dt1.textures)
        {
            var pngData = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            var pngPath = assetPath + "." + i + ".png";
            File.WriteAllBytes(pngPath, pngData);
            AssetDatabase.ImportAsset(pngPath);
            ++i;
        }
    }

    [MenuItem("Assets/Convert DT1 to PNG", true)]
    static public bool ConvertDT1ToPNGValidate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("dt1");
    }

    [MenuItem("Assets/Convert DCC to PNG")]
    static public void ConvertDCCToPNG()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        Palette.LoadPalette(0);
        DCC dcc = DCC.Load(assetPath, loadAllDirections: true, ignoreCache: true);
        int i = 0;
        foreach (var texture in dcc.textures)
        {
            var pngData = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            var pngPath = assetPath + "." + i + ".png";
            File.WriteAllBytes(pngPath, pngData);
            AssetDatabase.ImportAsset(pngPath);
            ++i;
        }
    }

    [MenuItem("Assets/Convert DCC to PNG", true)]
    static public bool ConvertDCCToPNGValidate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("dcc");
    }

    [MenuItem("Assets/Reset DT1 cache")]
    static public void ResetDT1()
    {
        DT1.ResetCache();
    }

    [MenuItem("Assets/Create font from DC6")]
    static public void CreateFontFromDC6()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var name = Path.GetFileNameWithoutExtension(assetPath);

        int textureSize = 1024;
        if (name.Contains("font16") || name.Contains("font24") || name.Contains("font30"))
            textureSize = 512;

        var dc6 = DC6.Load(assetPath, textureSize);
        var metrics = File.ReadAllText(Path.GetDirectoryName(assetPath) + "/" + name + ".txt").Split(',');

        var characterInfo = new CharacterInfo[dc6.framesPerDirection];
        for (int i = 0; i < dc6.framesPerDirection; i++)
        {
            int glyphHeight = int.Parse(metrics[i * 2].Trim());
            int glyphWidth = int.Parse(metrics[i * 2 + 1].Trim());
            var frame = dc6.frames[i];
            characterInfo[i].index = i;
            characterInfo[i].advance = glyphWidth;
            characterInfo[i].minX = 0;
            characterInfo[i].maxX = glyphWidth;
            characterInfo[i].minY = -glyphHeight;
            characterInfo[i].maxY = 0;
            characterInfo[i].glyphWidth = glyphWidth;
            characterInfo[i].glyphHeight = glyphHeight;

            var uv = new Rect(
                frame.textureX / (float)textureSize,
                (textureSize - (frame.textureY + frame.height)) / (float)textureSize,
                glyphWidth / (float)textureSize,
                glyphHeight / (float)textureSize);
            characterInfo[i].uvBottomLeft = new Vector2(uv.xMin, uv.yMin);
            characterInfo[i].uvBottomRight = new Vector2(uv.xMax, uv.yMin);
            characterInfo[i].uvTopLeft = new Vector2(uv.xMin, uv.yMax);
            characterInfo[i].uvTopRight = new Vector2(uv.xMax, uv.yMax);
        }

        
        var filepath = "Assets/Fonts/" + name;

        var pngData = dc6.texture.EncodeToPNG();
        Object.DestroyImmediate(dc6.texture);
        var texturePath = filepath + ".png";
        File.WriteAllBytes(texturePath, pngData);
        AssetDatabase.ImportAsset(texturePath);

        var fontPath = filepath + ".fontsettings";

        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font)
        {
            font.characterInfo = characterInfo;
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }
        else
        {
            font = new Font(name);
            font.characterInfo = characterInfo;
            AssetDatabase.CreateAsset(font, fontPath);
            AssetDatabase.ImportAsset(fontPath);
        }
    }

    [MenuItem("Assets/Create font from DC6", true)]
    static public bool CreateFontFromDC6Validate()
    {
        var assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        return assetPath.ToLower().EndsWith("dc6");
    }

    [MenuItem("Assets/Test serialization")]
    static public void TestSerialization()
    {
        var rb = Obj.Find(1, 2, 2);
        Debug.Log(rb.description);
    }
}

public static class ScriptableObjectUtility
{
    /// <summary>
    //	This makes it easy to create, name and place unique new ScriptableObject asset files.
    /// </summary>
    public static T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }

        string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New" + typeof(T).ToString() + ".asset");

        AssetDatabase.CreateAsset(asset, assetPathAndName);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        return asset;
    }
}
