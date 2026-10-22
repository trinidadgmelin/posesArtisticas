using UnityEngine;
using UnityEngine.SceneManagement;

public class cambioNivel1 : MonoBehaviour
{
    [SerializeField] private string escenaACargar = "nivel1";
    [SerializeField] private KeyCode teclaParaAvanzar = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyDown(teclaParaAvanzar))
        {
            SceneManager.LoadScene(escenaACargar);
        }
    }
}