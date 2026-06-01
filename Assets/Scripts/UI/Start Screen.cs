// csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    [Tooltip("If true, try to destroy known persistent singletons before loading the game scene.")]
    public bool destroyKnownSingletonsBeforeLoad = true;

    public void SwitchToGameScene()
    {
        if (destroyKnownSingletonsBeforeLoad)
        {
            // Beispiel: TobiiManager als bekannter Singleton (nur wenn in Projekt vorhanden)
            var tobii = FindObjectOfType<TobiiManager>();
            if (tobii != null)
                Destroy(tobii.gameObject);

            // Falls ihr eigene persistenten Manager unter einem GameObject gruppiert habt:
            var persistentRoot = GameObject.Find("PersistentManagers");
            if (persistentRoot != null)
                Destroy(persistentRoot);
        }

        StartCoroutine(LoadGameSceneCoroutine());
    }

    private IEnumerator LoadGameSceneCoroutine()
    {
        // Asynchron im Single‑Modus laden (ersetzt aktuelle Szene)
        var ao = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        ao.allowSceneActivation = true;

        while (!ao.isDone)
            yield return null;

        // Optional: Ressourcen freigeben, damit alles sauber neu initialisiert wird
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    public void SwitchToCreditScene()
    {
        SceneManager.LoadScene(2);
    }

    public void SwitchToStartScene()
    {
        SceneManager.LoadScene(0);
    }

    void Update() { }
}