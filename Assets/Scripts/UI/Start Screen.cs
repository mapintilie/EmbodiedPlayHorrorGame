using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void SwitchScene()
    {
        SceneManager.LoadScene(1); // Name der zweiten Szene
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
