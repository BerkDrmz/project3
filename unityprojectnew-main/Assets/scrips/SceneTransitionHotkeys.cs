using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionHotkeys : MonoBehaviour
{
    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "SampleScene";

    [Header("Hotkeys")]
    public KeyCode toggleSceneKey = KeyCode.F9;
    public KeyCode reloadSceneKey = KeyCode.F10;

    private void Update()
    {
        if (Input.GetKeyDown(toggleSceneKey))
        {
            ToggleScene();
        }

        if (Input.GetKeyDown(reloadSceneKey))
        {
            ReloadCurrentScene();
        }
    }

    public void ToggleScene()
    {
        Time.timeScale = 1f;

        string activeSceneName = SceneManager.GetActiveScene().name;
        string targetSceneName = activeSceneName == mainMenuSceneName
            ? gameplaySceneName
            : mainMenuSceneName;

        SceneManager.LoadScene(targetSceneName);
    }

    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
