using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class SceneField
{
    [SerializeField] private UnityEngine.Object _sceneAsset;
    [SerializeField] private string _sceneName;
    public string SceneName => _sceneName;

    public static implicit operator string(SceneField obj) { return obj.SceneName; }
}