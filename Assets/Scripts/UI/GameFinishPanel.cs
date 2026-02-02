using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class GameFinishPanel : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "StartManu";
        [SerializeField] private string restartScene = "Level-Tutorial";

        [Header("Pause")]
        [SerializeField] private bool pauseOnShow = true;

        private float previousTimeScale = 1f;
        private float previousFixedDeltaTime;

        private void OnEnable()
        {
            if (!pauseOnShow) return;
            previousTimeScale = Time.timeScale;
            previousFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;
        }

        private void OnDisable()
        {
            if (!pauseOnShow) return;
            Time.timeScale = previousTimeScale;
            Time.fixedDeltaTime = previousFixedDeltaTime;
        }

        public void RestartLevel()
        {
            RestoreTime();
            SceneManager.LoadScene(restartScene);
        }

        public void BackToMainMenu()
        {
            RestoreTime();
            SceneManager.LoadScene(mainMenuScene);
        }

        public void QuitGame()
        {
            RestoreTime();
            Application.Quit();
        }

        private void RestoreTime()
        {
            if (!pauseOnShow) return;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = previousFixedDeltaTime == 0f ? 0.02f : previousFixedDeltaTime;
        }
    }
}
