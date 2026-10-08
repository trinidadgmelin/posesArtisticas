using UnityEngine;
using UnityEngine.SceneManagement;

public class cambioPose1 : MonoBehaviour
{
    [SerializeField] private string escenaACargar = "pose1";
    [SerializeField] private KeyCode teclaParaAvanzar = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyDown(teclaParaAvanzar))
        {
            SceneManager.LoadScene(escenaACargar);
        }
    }
}