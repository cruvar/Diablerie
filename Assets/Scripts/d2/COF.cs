﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class COF
{
    public Layer[] layers;
    public Layer[] compositLayers;
    public int framesPerDirection;
    public int directionCount;
    public int layerCount;
    public byte[] priority;
    public float frameDuration = 1.0f / 25.0f;
    public string basePath;
    public string token;
    public string mode;

    public struct Layer
    {
        public int index;
        public int compositIndex;
        public string name;
        public string weaponClass;
        public bool shadow;
        public Material material;
    }

    public static readonly string[][] ModeNames = {
        new string[] { "DT", "NU", "WL", "RN", "GH", "TN", "TW", "A1", "A2", "BL", "SC", "TH", "KK", "S1", "S2", "S3", "S4", "DD", "GH", "GH" }, // player (plrmode.txt)
        new string[] { "DT", "NU", "WL", "GH", "A1", "A2", "BL", "SC", "S1", "S2", "S3", "S4", "DD", "GH", "xx", "RN" }, // monsters (monmode.txt)
        new string[] { "NU", "OP", "ON", "S1", "S2", "S3", "S4", "S5" } // objects (objmode.txt)
    };
    static public readonly string[] layerNames = { "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" };
    static Dictionary<string, COF> cache = new Dictionary<string, COF>();

    static public COF Load(string basePath, string token, string weaponClass, string mode)
    {
        string cofFilename = Application.streamingAssetsPath + "/d2/" + basePath + "/" + token + "/cof/" + token + mode + weaponClass + ".cof";
        cofFilename.ToLower();
        if (cache.ContainsKey(cofFilename))
        {
            return cache[cofFilename];
        }

        COF cof = new COF();
        cof.basePath = basePath;
        cof.token = token;
        cof.mode = mode;

        byte[] bytes = File.ReadAllBytes(cofFilename);
        var stream = new MemoryStream(bytes);
        var reader = new BinaryReader(stream);

        cof.layerCount = reader.ReadByte();
        cof.framesPerDirection = reader.ReadByte();
        cof.directionCount = reader.ReadByte();
        stream.Seek(25, SeekOrigin.Current);

        cof.compositLayers = new Layer[16];
        cof.layers = new Layer[cof.layerCount];

        for (int i = 0; i < cof.layerCount; ++i)
        {
            Layer layer = new Layer();
            layer.index = i;
            layer.compositIndex = reader.ReadByte();
            layer.name = layerNames[layer.compositIndex];

            layer.shadow = reader.ReadByte() != 0;
            reader.ReadByte();

            bool transparent = reader.ReadByte() != 0;
            int blendMode = reader.ReadByte();
            if (transparent)
            {
                layer.material = Materials.softAdditive;
            }
            else
            {
                layer.material = Materials.normal;
            }

            layer.weaponClass = System.Text.Encoding.Default.GetString(reader.ReadBytes(4), 0, 3);

            cof.layers[i] = layer;
            cof.compositLayers[layer.compositIndex] = layer;
        }

        stream.Seek(cof.framesPerDirection, SeekOrigin.Current);
        cof.priority = reader.ReadBytes(cof.directionCount * cof.framesPerDirection * cof.layerCount);

        AnimData animData = new AnimData();
        if (AnimData.Find(token + mode + weaponClass, ref animData))
        {
            cof.frameDuration = animData.frameDuration;
            if (mode == "RN")
                cof.frameDuration *= 1.8f;
            if (mode == "WL")
                cof.frameDuration *= 1.5f;
        }
        else
        {
            Debug.LogWarning("animdata not found " + (token + mode + weaponClass));
        }

        cache.Add(cofFilename, cof);
        return cof;
    }

    public string DccFilename(Layer layer, string equip)
    {
        string filename = Application.streamingAssetsPath + "/d2/" + basePath + "/" + token + "/" + layer.name + "/" + token + layer.name + equip + mode + layer.weaponClass + ".dcc";
        return filename.ToLower();
    }
}
