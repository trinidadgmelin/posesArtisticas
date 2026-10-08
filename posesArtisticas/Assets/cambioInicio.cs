using UnityEngine;
using UnityEngine.SceneManagement;

public class cambioInicio : MonoBehaviour
{
    [SerializeField] private string escenaACargar = "menu";
    [SerializeField] private KeyCode teclaParaAvanzar = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyDown(teclaParaAvanzar))
        {
            SceneManager.LoadScene(escenaACargar);
        }
    }
}