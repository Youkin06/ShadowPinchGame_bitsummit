// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Mediapipe.Unity
{
  public class StartSceneController : MonoBehaviour
  {
    private const string _TAG = nameof(StartSceneController);

    [SerializeField] private Image _screen;
    [SerializeField] private GameObject _consolePrefab;

    private IEnumerator Start()
    {
      var _ = Instantiate(_consolePrefab, _screen.transform);

      var bootstrap = GetComponent<Bootstrap>();

      yield return new WaitUntil(() => bootstrap.isFinished);

      DontDestroyOnLoad(gameObject);

      var firstSceneBuildIndex = FindFirstLoadableSceneBuildIndex();
      if (firstSceneBuildIndex < 0)
      {
#if UNITY_EDITOR
        Logger.LogWarning(_TAG, "No sample scene is registered in Build Settings. Trying to load a sample scene directly from the project...");
        var editorLoadReq = TryLoadFirstSampleSceneInEditor();
        if (editorLoadReq == null)
        {
          Logger.LogError(_TAG, "No loadable sample scene was found. Add sample scenes to Build Settings or open a solution scene directly.");
          yield break;
        }
        yield return new WaitUntil(() => editorLoadReq.isDone);
        yield break;
#else
        Logger.LogError(_TAG, "No loadable scene is registered in Build Settings. Add sample scenes to Build Settings or open a solution scene directly.");
        yield break;
#endif
      }

      Logger.LogInfo(_TAG, $"Loading the first scene (buildIndex={firstSceneBuildIndex})...");
      var sceneLoadReq = SceneManager.LoadSceneAsync(firstSceneBuildIndex);
      if (sceneLoadReq == null)
      {
        Logger.LogError(_TAG, $"Failed to load scene (buildIndex={firstSceneBuildIndex}).");
        yield break;
      }
      yield return new WaitUntil(() => sceneLoadReq.isDone);
    }

    private static int FindFirstLoadableSceneBuildIndex()
    {
      var activeScene = SceneManager.GetActiveScene();
      var activeSceneBuildIndex = activeScene.buildIndex;
      var activeScenePath = activeScene.path;
      var sceneCount = SceneManager.sceneCountInBuildSettings;
      var isMediaPipeStartScene = activeScenePath.EndsWith("/MediaPipeUnity/Samples/Scenes/Start Scene.unity");

      if (isMediaPipeStartScene)
      {
        for (var i = 0; i < sceneCount; i++)
        {
          var path = SceneUtility.GetScenePathByBuildIndex(i);
          if (string.IsNullOrEmpty(path))
          {
            continue;
          }

          if (path.Contains("/MediaPipeUnity/Samples/Scenes/") && !path.EndsWith("/MediaPipeUnity/Samples/Scenes/Start Scene.unity"))
          {
            return i;
          }
        }

        return -1;
      }

      for (var i = 0; i < sceneCount; i++)
      {
        if (i == activeSceneBuildIndex)
        {
          continue;
        }

        var path = SceneUtility.GetScenePathByBuildIndex(i);
        if (!string.IsNullOrEmpty(path))
        {
          return i;
        }
      }

      return -1;
    }

#if UNITY_EDITOR
    private static AsyncOperation TryLoadFirstSampleSceneInEditor()
    {
      string[] sampleSceneRoots = {
        "Assets/MediaPipeUnityPlugin-all/Assets/MediaPipeUnity/Samples/Scenes",
        "Assets/MediaPipeUnity/Samples/Scenes",
      };

      foreach (var root in sampleSceneRoots)
      {
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { root });
        foreach (var sceneGuid in sceneGuids)
        {
          var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
          if (string.IsNullOrEmpty(scenePath) || scenePath.EndsWith("/Start Scene.unity"))
          {
            continue;
          }

          Logger.LogInfo(_TAG, $"Loading sample scene from project path: {scenePath}");
          return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }
      }

      return null;
    }
#endif
  }
}
