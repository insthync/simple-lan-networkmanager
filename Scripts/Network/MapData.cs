using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapData : ScriptableObject
{
    public Sprite mapPreview;
    public SceneNameField scene;

    public string GetId()
    {
        return name;
    }
}
