using UnityEngine;
using UnityEngine.SceneManagement; 

public class LevelReloader : MonoBehaviour
{
    public static LevelReloader Instance;
    public bool reloadWithKey = true;
    // Choose which key reloads the scene (default: R)
    public KeyCode reloadKey = KeyCode.R;

    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    void Update()
    {
        if (reloadWithKey && Input.GetKeyDown(reloadKey))
        {
            ReloadLevel();
        }
    }

    public static void ReloadLevel()
    {
        Instance._ReloadLevel();
    }

    public void ReloadLevelAnim()
    {
        Instance._ReloadLevel();
    }

    private void _ReloadLevel()
    {
        // Get the current active scene
        Scene currentScene = SceneManager.GetActiveScene();

        // Reload it by name
        SceneManager.LoadScene(currentScene.name);
    }
}