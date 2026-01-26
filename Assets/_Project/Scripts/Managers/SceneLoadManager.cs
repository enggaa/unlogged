using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.Managers
{
    public class SceneLoadManager : MonoBehaviour
    {
        public bool IsLoading { get; private set; }

        public void LoadScene(string sceneName)
        {
            if (!IsLoading)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
        }

        public void LoadScene(int sceneIndex)
        {
            if (!IsLoading)
            {
                StartCoroutine(LoadSceneAsync(sceneIndex));
            }
        }

        public void ReloadCurrentScene()
        {
            if (!IsLoading)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                StartCoroutine(LoadSceneAsync(currentScene));
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            IsLoading = true;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);

                if (operation.progress >= 0.9f)
                {
                    yield return new WaitForSeconds(0.5f);
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            IsLoading = false;
        }

        private IEnumerator LoadSceneAsync(int sceneIndex)
        {
            IsLoading = true;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);

                if (operation.progress >= 0.9f)
                {
                    yield return new WaitForSeconds(0.5f);
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }

            IsLoading = false;
        }

        public void LoadSceneAdditive(string sceneName)
        {
            StartCoroutine(LoadSceneAdditiveAsync(sceneName));
        }

        private IEnumerator LoadSceneAdditiveAsync(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            while (!operation.isDone)
            {
                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(loadedScene);
        }

        public void UnloadScene(string sceneName)
        {
            StartCoroutine(UnloadSceneAsync(sceneName));
        }

        private IEnumerator UnloadSceneAsync(string sceneName)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            
            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}