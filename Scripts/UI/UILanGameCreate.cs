using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILanGameCreate : UIBase
{
    public MapData[] maps;
    public static readonly Dictionary<string, MapData> Maps = new Dictionary<string, MapData>();

    protected override void Awake()
    {
        base.Awake();

        Maps.Clear();
        foreach (var map in maps)
        {
            Maps[map.GetId()] = map;
        }
    }
}
