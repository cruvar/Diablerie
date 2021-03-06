﻿using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class DS1
{
    public GameObject root;
    public Vector3 center;
    public Vector3 entry;
    public int width;
    public int height;

    struct Cell
    {
        public byte prop1;
        public byte prop2;
        public byte prop3;
        public byte prop4;
    };

    static byte[] dirLookup = {
                  0x00, 0x01, 0x02, 0x01, 0x02, 0x03, 0x03, 0x05, 0x05, 0x06,
                  0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
                  0x0F, 0x10, 0x11, 0x12, 0x14
               };

    static readonly int mapEntryIndex = DT1.Tile.Index(30, 11, 10);
    static readonly int townEntryIndex = DT1.Tile.Index(30, 0, 10);
    static readonly int townEntry2Index = DT1.Tile.Index(31, 0, 10);
    static readonly int corpseLocationIndex = DT1.Tile.Index(32, 0, 10);
    static readonly int portalLocationIndex = DT1.Tile.Index(33, 0, 10);

    static public DS1 Load(string ds1Path)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var stream = new MemoryStream(File.ReadAllBytes(ds1Path));
        var reader = new BinaryReader(stream);
        int version = reader.ReadInt32();
        int width = reader.ReadInt32() + 1;
        int height = reader.ReadInt32() + 1;

        int act = 0;
        if (version >= 8)
        {
            act = reader.ReadInt32();
            act = Mathf.Min(act, 4);
        }

        Palette.LoadPalette(act);

        if (version >= 10)
        {
            reader.ReadInt32(); // tagType

            //// adjust eventually the # of tag layer
            //if ((tagType == 1) || (tagType == 2))
            //    t_num = 1;
        }

        if (version >= 3)
        {
            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                string filename = "";
                char c;
                while ((c = reader.ReadChar()) != 0)
                {
                    filename += c;
                }
                filename = filename.Replace("tg1", "dt1");
                filename = filename.Replace("C:", "");
                DT1.Load(Application.streamingAssetsPath + filename);
            }
        }

        // skip 2 dwords ?
        if ((version >= 9) && (version <= 13))
            stream.Seek(8, SeekOrigin.Current);

        int wallLayerCount = 1;
        int floorLayerCount = 1;
        int shadowLayerCount = 1;
        int tagLayerCount = 0;

        if (version >= 4)
        {
            wallLayerCount = reader.ReadInt32();

            if (version >= 16)
            {
                floorLayerCount = reader.ReadInt32();
            }
        }
        else
        {
            tagLayerCount = 1;
        }

        Debug.Log("layers : (2 * " + wallLayerCount + " walls) + " + floorLayerCount + " floors + " + shadowLayerCount + " shadow + " + tagLayerCount + " tag");

        Cell[][] walls = new Cell[wallLayerCount][];
        for (int i = 0; i < wallLayerCount; ++i)
            walls[i] = new Cell[width * height];

        int layerCount = 0;
        int[] layout = new int[14];
        if (version < 4)
        {
            layout[0] = 1; // wall 1
            layout[1] = 9; // floor 1
            layout[2] = 5; // orientation 1
            layout[3] = 12; // tag
            layout[4] = 11; // shadow
            layerCount = 5;
        }
        else
        {
            layerCount = 0;
            for (int x = 0; x < wallLayerCount; x++)
            {
                layout[layerCount++] = 1 + x; // wall x
                layout[layerCount++] = 5 + x; // orientation x
            }
            for (int x = 0; x < floorLayerCount; x++)
                layout[layerCount++] = 9 + x; // floor x
            if (shadowLayerCount != 0)
                layout[layerCount++] = 11;    // shadow
            if (tagLayerCount != 0)
                layout[layerCount++] = 12;    // tag
        }

        var result = new DS1();
        result.center = MapToWorld(width, height) / 2;
        result.entry = result.center;
        result.root = new GameObject(Path.GetFileName(ds1Path));
        result.width = width;
        result.height = height;

        var floorLayers = new GameObject[floorLayerCount];
        for (int i = 0; i < floorLayerCount; ++i)
        {
            floorLayers[i] = new GameObject("f" + (i + 1));
            floorLayers[i].transform.SetParent(result.root.transform);
        }

        var wallLayers = new GameObject[wallLayerCount];
        for (int i = 0; i < wallLayerCount; ++i)
        {
            wallLayers[i] = new GameObject("w" + (i + 1));
            wallLayers[i].transform.SetParent(result.root.transform);
        }

        for (int n = 0; n < layerCount; n++)
        {
            int p;
            int i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    switch (layout[n])
                    {
                        // walls
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                            {
                                p = layout[n] - 1;
                                walls[p][i].prop1 = reader.ReadByte();
                                walls[p][i].prop2 = reader.ReadByte();
                                walls[p][i].prop3 = reader.ReadByte();
                                walls[p][i].prop4 = reader.ReadByte();
                                break;
                            }

                        // orientations
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                            {
                                p = layout[n] - 5;
                                int orientation = reader.ReadByte();
                                if (version < 7)
                                    orientation = dirLookup[orientation];

                                reader.ReadBytes(3);

                                if (walls[p][i].prop1 == 0)
                                    break;

                                int prop2 = walls[p][i].prop2;
                                int prop3 = walls[p][i].prop3;
                                int prop4 = walls[p][i].prop4;

                                int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                                int subIndex = prop2;
                                int index = DT1.Tile.Index(mainIndex, subIndex, orientation);
                                if (index == mapEntryIndex)
                                {
                                    result.entry = MapToWorld(x, y);
                                    Debug.Log("Found map entry at " + x + " " + y);
                                    break;
                                }
                                else if (index == townEntryIndex)
                                {
                                    result.entry = MapToWorld(x, y);
                                    Debug.Log("Found town entry at " + x + " " + y);
                                    break;
                                }
                                else if (index == townEntry2Index)
                                {
                                    break;
                                }
                                else if (index == corpseLocationIndex)
                                {
                                    break;
                                }
                                else if (index == portalLocationIndex)
                                {
                                    break;
                                }

                                DT1.Tile tile;
                                if (DT1.Find(index, out tile))
                                {
                                    var tileObject = CreateTile(tile, x, y);
                                    tileObject.transform.SetParent(wallLayers[p].transform);
                                }
                                else
                                {
                                    Debug.LogWarning("wall tile not found (index " + mainIndex + " " + subIndex + " " + orientation + ") at " + x + ", " + y);
                                }

                                if (orientation == 3)
                                {
                                    index = DT1.Tile.Index(mainIndex, subIndex, 4);
                                    if (DT1.Find(index, out tile))
                                    {
                                        var tileObject = CreateTile(tile, x, y);
                                        tileObject.transform.SetParent(wallLayers[p].transform);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("wall tile not found (index " + mainIndex + " " + subIndex + " " + orientation + ") at " + x + ", " + y);
                                    }
                                }

                                break;
                            }

                        // floors
                        case 9:
                        case 10:
                            {
                                p = layout[n] - 9;
                                int prop1 = reader.ReadByte();
                                int prop2 = reader.ReadByte();
                                int prop3 = reader.ReadByte();
                                int prop4 = reader.ReadByte();

                                if (prop1 == 0) // no tile here
                                    break;

                                int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                                int subIndex = prop2;
                                int orientation = 0;
                                int index = DT1.Tile.Index(mainIndex, subIndex, orientation);
                                DT1.Tile tile;
                                if (DT1.Find(index, out tile))
                                {
                                    var tileObject = CreateTile(tile, x, y, orderInLayer: p);
                                    tileObject.transform.SetParent(floorLayers[p].transform);
                                }
                                break;
                            }

                        // shadow
                        case 11:
                            reader.ReadBytes(4);
                            //if ((x < new_width) && (y < new_height))
                            //{
                            //    p = layout[n] - 11;
                            //    s_ptr[p]->prop1 = *bptr;
                            //    bptr++;
                            //    s_ptr[p]->prop2 = *bptr;
                            //    bptr++;
                            //    s_ptr[p]->prop3 = *bptr;
                            //    bptr++;
                            //    s_ptr[p]->prop4 = *bptr;
                            //    bptr++;
                            //    s_ptr[p] += s_num;
                            //}
                            //else
                            //    bptr += 4;
                            break;

                        // tag
                        case 12:
                            reader.ReadBytes(4);
                            //if ((x < new_width) && (y < new_height))
                            //{
                            //    p = layout[n] - 12;
                            //    t_ptr[p]->num = (UDWORD) * ((UDWORD*)bptr);
                            //    t_ptr[p] += t_num;
                            //}
                            //bptr += 4;
                            break;
                    }
                    ++i;
                }
            }
        }

        if (version >= 2)
        {
            int objectCount = reader.ReadInt32();
            Debug.Log("Objects " + objectCount);

            for (int n = 0; n < objectCount; n++)
            {
                int type = reader.ReadInt32();
                int id = reader.ReadInt32();
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();

                if (version > 5)
                {
                    reader.ReadInt32(); // flags
                }

                var pos = MapSubCellToWorld(x, y);
                Obj obj = Obj.Find(act, type, id);
                var gameObject = CreateObject(obj, pos);
                if (gameObject != null)
                    gameObject.transform.SetParent(result.root.transform);
                else
                    Debug.LogWarning("Object not instantiated " + obj.description);
            }
        }

        sw.Stop();
        Debug.Log("DS1 loaded in " + sw.Elapsed);

        return result;
    }

    static Vector3 MapToWorld(int x, int y)
    {
        var pos = Iso.MapToWorld(new Vector3(x, y)) / Iso.tileSize;
        pos.y = -pos.y;
        return pos;
    }

    static Vector3 MapSubCellToWorld(int x, int y)
    {
        var pos = Iso.MapToWorld(new Vector3(x - 2, y - 2));
        pos.y = -pos.y;
        return pos;
    }

    static GameObject CreateTile(DT1.Tile tile, int x, int y, int orderInLayer = 0)
    {
        var texture = tile.texture;
        var pos = MapToWorld(x, y);

        GameObject gameObject = new GameObject();
        gameObject.name = tile.mainIndex + "_" + tile.subIndex + "_" + tile.orientation;
        gameObject.transform.position = pos;
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        float x0 = tile.textureX;
        float y0 = tile.textureY;
        float w = tile.width / Iso.pixelsPerUnit;
        float h = (-tile.height) / Iso.pixelsPerUnit;
        if(tile.orientation == 0 || tile.orientation == 15)
        {
            var topLeft = new Vector3(-1f, 0.5f);
            if (tile.orientation == 15)
                topLeft.y += tile.roofHeight / Iso.pixelsPerUnit;
            mesh.vertices = new Vector3[] {
                topLeft,
                topLeft + new Vector3(0, -h),
                topLeft + new Vector3(w, -h),
                topLeft + new Vector3(w, 0)
            };
            mesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };
            mesh.uv = new Vector2[] {
                new Vector2 (x0 / texture.width, -y0 / texture.height),
                new Vector2 (x0 / texture.width, (-y0 +tile.height) / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, (-y0 +tile.height) / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, -y0 / texture.height)
            };

            meshRenderer.sortingLayerName = tile.orientation == 0 ? "Floor" : "Roof";
            meshRenderer.sortingOrder = orderInLayer;
        }
        else
        {
            var topLeft = new Vector3(-1f, h - 0.5f);
            mesh.vertices = new Vector3[] {
                topLeft,
                topLeft + new Vector3(0, -h),
                topLeft + new Vector3(w, -h),
                topLeft + new Vector3(w, 0)
            };
            mesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };
            mesh.uv = new Vector2[] {
                new Vector2 (x0 / texture.width, (-y0 - tile.height) / texture.height),
                new Vector2 (x0 / texture.width, -y0 / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, -y0 / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, (-y0 - tile.height) / texture.height)
            };
            meshRenderer.sortingOrder = Iso.SortingOrder(pos) - 4;
        }
        meshFilter.mesh = mesh;

        if (Application.isPlaying)
        {
            int flagIndex = 0;
            for (int dx = -2; dx < 3; ++dx)
            {
                for (int dy = 2; dy > -3; --dy)
                {
                    if ((tile.flags[flagIndex] & (1 + 8)) != 0)
                    {
                        var subCellPos = Iso.MapToIso(pos) + new Vector3(dx, dy);
                        Tilemap.SetPassable(subCellPos, false);
                    }
                    ++flagIndex;
                }
            }
        }

        meshRenderer.material = tile.material;
        return gameObject;
    }

    static GameObject CreateObject(Obj obj, Vector3 pos)
    {
        if (obj.type == 2)
        {
            ObjectInfo objectInfo = ObjectInfo.sheet.rows[obj.objectId];
            var staticObject = World.SpawnObject(objectInfo, pos);
            staticObject.modeName = obj.mode;
            return staticObject.gameObject;
        }
        else
        {
            var monStat = MonStat.Find(obj.act, obj.id);
            if (monStat == null)
                return null;
            return World.SpawnMonster(monStat, pos).gameObject;
        }
    }
}
