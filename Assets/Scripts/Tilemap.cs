﻿using UnityEngine;

public class Tilemap : MonoBehaviour {

    static private Tilemap instance;

    public struct Cell
    {
        public bool passable;
        public GameObject gameObject;
    }

    private int width = 1024;
    private int height = 1024;
    private int origin;
    private Cell[] map;

    void Awake()
    {
        map = new Cell[width * height];
        origin = map.Length / 2;
        instance = this;
        for (int i = 0; i < map.Length; ++i)
            map[i].passable = true;
    }

    void DrawDebugCellGrid()
    {
        Color color = new Color(1, 0, 0, 0.3f);
        Color freeColor = new Color(1, 1, 1, 0.03f);
        Vector3 pos = Iso.Snap(Iso.MapToIso(Camera.main.transform.position));
        int debugWidth = 100;
        int debugHeight = 100;
        pos.x -= debugWidth / 2;
        pos.y -= debugHeight / 2;
        int index = instance.MapToIndex(Iso.Snap(pos));
        for (int y = 0; y < debugHeight; ++y)
        {
            for (int x = 0; x < debugWidth; ++x)
            {
                if (!instance.map[index + x].passable)
                    Iso.DebugDrawTile(pos + new Vector3(x, y), color, 0.9f);
                else
                    Iso.DebugDrawTile(pos + new Vector3(x, y), freeColor, 0.9f);
            }
            index += width;
        }
    }

    private int MapToIndex(Vector3 pos)
    {
		return origin + Mathf.RoundToInt(pos.x + pos.y * width);
	}

    public static Cell GetCell(Vector3 pos)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        return instance.map[index];
    }

    public static void SetCell(Vector3 pos, Cell cell)
    {
        var tilePos = Iso.Snap(pos);
        int index = instance.MapToIndex(tilePos);
        instance.map[index] = cell;
    }

    public static bool Passable(Vector3 pos, int radius = 0, bool debug = false, GameObject ignore = null)
    {
        var tilePos = Iso.Snap(pos);
        return PassableTile(tilePos, radius, debug, ignore);
    }

    public static bool PassableTile(Vector3 tilePos, int radius = 0, bool debug = false, GameObject ignore = null)
    {
        UnityEngine.Profiling.Profiler.BeginSample("PassableTile");
        int index = instance.MapToIndex(tilePos);
        var c0 = instance.map[index];
        bool passable = c0.passable || (ignore != null && ignore == c0.gameObject);
        if (radius > 0)
        {
            var c1 = instance.map[index - 1];
            var c2 = instance.map[index + 1];
            var c3 = instance.map[index - instance.width];
            var c4 = instance.map[index + instance.width];

            passable = passable && (c1.passable || (ignore != null && ignore == c1.gameObject));
            passable = passable && (c2.passable || (ignore != null && ignore == c2.gameObject));
            passable = passable && (c3.passable || (ignore != null && ignore == c3.gameObject));
            passable = passable && (c4.passable || (ignore != null && ignore == c4.gameObject));
        }

        UnityEngine.Profiling.Profiler.EndSample();

        if (debug)
        {
            Iso.DebugDrawTile(tilePos, 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(1, 0), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(-1, 0), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(0, 1), 0.1f);
            Iso.DebugDrawTile(tilePos + new Vector3(0, -1), 0.1f);
        }
        return passable;
    }

    public static void SetPassable(Vector3 tilePos, bool passable)
    {
        int index = instance.MapToIndex(tilePos);
        instance.map[index].passable = passable;
    }

    public static void SetPassable(Vector3 tilePos, int sizeX, int sizeY, bool passable)
    {
        int index = instance.MapToIndex(tilePos) - sizeX / 2 - sizeY / 2 * instance.height;
        int step = instance.width - sizeX;
        for (int y = 0; y < sizeY; ++y)
        {
            int end = index + sizeX;
            while (index < end)
            {
                instance.map[index++].passable = passable;
            }
            index += step;
        }
    }

    public struct RaycastHit
    {
        public bool hit;
        public GameObject gameObject;
        public Vector2 pos;

        public static implicit operator bool(RaycastHit value)
        {
            return value.hit;
        }
    }

    static public RaycastHit Raycast(Vector2 from, Vector2 to, float rayLength = Mathf.Infinity, float maxRayLength = Mathf.Infinity, GameObject ignore = null, bool debug = false)
    {
        var hit = new RaycastHit();
        var diff = to - from;
        var stepLen = 0.2f;
        if (rayLength == Mathf.Infinity)
            rayLength = Mathf.Min(diff.magnitude, maxRayLength);
        int stepCount = Mathf.RoundToInt(rayLength / stepLen);
        var step = diff.normalized * stepLen;
        var pos = from;
        for (int i = 0; i < stepCount; ++i)
        {
            pos += step;
            if (debug)
                Iso.DebugDrawTile(Iso.Snap(pos), margin: 0.3f, duration: 0.5f);
            Cell cell = GetCell(pos);
            bool passable = Passable(pos, 2, debug, ignore);
            if (!passable)
            {
                hit.hit = !passable;
                hit.gameObject = cell.gameObject;
                break;
            }
        }
        return hit;
    }

    static public int OverlapBox(Vector2 center, Vector2 size, GameObject[] result)
    {
        int count = 0;
        if (result.Length == 0)
            return 0;
        int rows = Mathf.RoundToInt(size.y);
        int columns = Mathf.RoundToInt(size.x);
        int index = instance.MapToIndex(Iso.Snap(center - size / 2));
        for(int row = 0; row < rows; ++row)
        {
            for(int column = 0; column < columns; ++column)
            {
                var gameObject = instance.map[index + column].gameObject;
                if (gameObject != null)
                {
                    result[count] = gameObject;
                    count += 1;
                    if (count >= result.Length)
                        return count;
                }
            }
            index += instance.width;
        }
        return count;
    }

    static public void Move(Vector2 from, Vector2 to, GameObject gameObject)
    {
        from = Iso.Snap(from);
        to = Iso.Snap(to);

        int indexFrom = instance.MapToIndex(from);
        int indexTo = instance.MapToIndex(to);

        instance.map[indexFrom].passable = true;
        instance.map[indexFrom].gameObject = null;

        instance.map[indexTo].passable = false;
        instance.map[indexTo].gameObject = gameObject;
    }

    void OnDrawGizmos()
    {
        var cameraTile = Iso.MacroTile(Iso.MapToIso(Camera.current.transform.position));
        Gizmos.color = new Color(0.35f, 0.35f, 0.35f);
        for (int x = -10; x < 10; ++x)
        {
            var pos = Iso.MapToWorld(cameraTile + new Vector3(x, 10) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, -10f));
        }

        for (int y = -10; y < 10; ++y)
        {
            var pos = Iso.MapToWorld(cameraTile + new Vector3(-10, y) - new Vector3(0.5f, 0.5f)) / Iso.tileSize;
            Gizmos.DrawRay(pos, new Vector3(20, 10f));
        }
    }
}
