// Unity 6 Compatible - SceneLoadData.cs
// Updated: 2026-01-31

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "SceneLoadData", menuName = "System/SceneLoadData", order = 1)]
public class SceneLoadData : ScriptableObject {

    public string sceneName;
    public int    sceneBuildIndex;
    public string sceneDescription;
    public Sprite loadScreenScreenshot;

}
