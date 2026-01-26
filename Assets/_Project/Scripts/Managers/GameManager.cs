using UnityEngine;

namespace GameCore.Managers
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        [Header("Managers")]
        public InputManager InputManager { get; private set; }
        public SceneLoadManager SceneManager { get; private set; }
        public UIManager UIManager { get; private set; }

        [Header("Game State")]
        public bool IsGamePaused { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeManagers();
        }

        private void InitializeManagers()
        {
            InputManager = gameObject.AddComponent<InputManager>();
            SceneManager = gameObject.AddComponent<SceneLoadManager>();
            UIManager = gameObject.AddComponent<UIManager>();

            Debug.Log("Game Managers Initialized");
        }

        public void PauseGame()
        {
            IsGamePaused = true;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            IsGamePaused = false;
            Time.timeScale = 1f;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}