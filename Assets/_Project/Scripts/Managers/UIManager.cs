using UnityEngine;
using System.Collections.Generic;

namespace GameCore.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject loadingScreenPanel;

        private Dictionary<string, GameObject> _uiPanels = new Dictionary<string, GameObject>();
        private Stack<GameObject> _panelStack = new Stack<GameObject>();

        private void Start()
        {
            InitializePanels();
        }

        private void InitializePanels()
        {
            if (mainMenuPanel != null) RegisterPanel("MainMenu", mainMenuPanel);
            if (pauseMenuPanel != null) RegisterPanel("PauseMenu", pauseMenuPanel);
            if (hudPanel != null) RegisterPanel("HUD", hudPanel);
            if (loadingScreenPanel != null) RegisterPanel("LoadingScreen", loadingScreenPanel);

            HideAllPanels();
        }

        public void RegisterPanel(string panelName, GameObject panel)
        {
            if (!_uiPanels.ContainsKey(panelName))
            {
                _uiPanels.Add(panelName, panel);
                panel.SetActive(false);
            }
        }

        public void ShowPanel(string panelName)
        {
            if (_uiPanels.ContainsKey(panelName))
            {
                GameObject panel = _uiPanels[panelName];
                panel.SetActive(true);
                _panelStack.Push(panel);
            }
        }

        public void HidePanel(string panelName)
        {
            if (_uiPanels.ContainsKey(panelName))
            {
                _uiPanels[panelName].SetActive(false);
                
                if (_panelStack.Count > 0 && _panelStack.Peek() == _uiPanels[panelName])
                {
                    _panelStack.Pop();
                }
            }
        }

        public void HideTopPanel()
        {
            if (_panelStack.Count > 0)
            {
                GameObject topPanel = _panelStack.Pop();
                topPanel.SetActive(false);
            }
        }

        public void HideAllPanels()
        {
            foreach (var panel in _uiPanels.Values)
            {
                panel.SetActive(false);
            }
            _panelStack.Clear();
        }

        public void TogglePanel(string panelName)
        {
            if (_uiPanels.ContainsKey(panelName))
            {
                GameObject panel = _uiPanels[panelName];
                if (panel.activeSelf)
                {
                    HidePanel(panelName);
                }
                else
                {
                    ShowPanel(panelName);
                }
            }
        }

        public void ShowHUD() => ShowPanel("HUD");
        public void HideHUD() => HidePanel("HUD");

        public void ShowPauseMenu()
        {
            ShowPanel("PauseMenu");
            GameManager.Instance.PauseGame();
        }

        public void HidePauseMenu()
        {
            HidePanel("PauseMenu");
            GameManager.Instance.ResumeGame();
        }

        public void ShowLoadingScreen() => ShowPanel("LoadingScreen");
        public void HideLoadingScreen() => HidePanel("LoadingScreen");
    }
}