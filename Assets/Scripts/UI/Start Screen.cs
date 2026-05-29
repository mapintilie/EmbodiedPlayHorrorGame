using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void SwitchToGameScene()
    {
        SceneManager.LoadScene(1); //switch to game scene aka sample scene
    }

    public void SwitchToCreditScene()
    {
        SceneManager.LoadScene(2); //switch to credit scene
    }
    
    public void SwitchToStartScene()
    {
        SceneManager.LoadScene(0); //switch to start scene
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
