using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void ButtonLoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
