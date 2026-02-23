using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneField
{
    [SerializeField] private UnityEngine.Object _sceneAsset;
    [SerializeField] private string _sceneName;
    public string SceneName => _sceneName;
    //public int SceneID => _sceneAsset;
    public static implicit operator string(SceneField obj) { return obj.SceneName; }
}

public static class GamePlayStatics
{ 
    public static GameObject GetPlayer()
    {
        return GameObject.FindGameObjectWithTag("Player");
    }
}

