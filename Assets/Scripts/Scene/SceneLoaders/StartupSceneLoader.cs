using System;
using System.Collections;
using xFrame.Infrastructure;
using Scene.SceneFlow;
using UnityEngine;

namespace Scene.SceneLoaders
{
    public class StartupSceneLoader : SceneLoader<StartupScene>
    {
        protected override IEnumerator LoadScene(StartupScene scene, Action<float, string> progressDelegate)
        {
            Debug.Log("LoadScene");
            yield break;
        }

        protected override IEnumerator UnloadScene(StartupScene scene, Action<float, string> progressDelegate)
        {
            yield break;
        }
    }
}