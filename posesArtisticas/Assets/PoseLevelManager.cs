using UnityEngine;
using UnityEngine.SceneManagement;

public class PoseLevelManager : MonoBehaviour
{
    [Header("Configuración de Escena y Esferas")]
    [Tooltip("Nombre de la escena a la que pasa si se cumple la condición de las esferas o se presiona la tecla E.")]
    public string nextSceneName;

    [Tooltip("Arrastra aquí todas las esferas que deben recibir hover simultáneo.")]
    public GameObject[] activeSpheres;

    [Header("Configuración de Tiempo Límite")]
    [Tooltip("Tiempo en segundos antes de pasar de escena automáticamente.")]
    public float timeLimit = 10f;

    [Tooltip("Escena por defecto si se agota el tiempo (deja vacío si es la misma que nextSceneName).")]
    public string fallbackSceneOnTimeout;

    private float timer;

    void Start()
    {
        timer = timeLimit;
    }

    void Update()
    {
        // 1. Entrada de teclado (Tecla E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            LoadNextScene(nextSceneName);
            return;
        }

        // 2. Temporizador
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            TimeOutTrigger();
            return;
        }

        // 3. Verificación de Hover Simultáneo
        if (CheckAllSpheresHoveredSimultaneously())
        {
            LoadNextScene(nextSceneName);
        }
    }

    bool CheckAllSpheresHoveredSimultaneously()
    {
        if (Camera.main == null || activeSpheres == null || activeSpheres.Length == 0)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Si el raycast no golpea nada, ninguna esfera está siendo apuntada
        if (!Physics.Raycast(ray, out hit))
        {
            return false;
        }

        GameObject hoveredObject = hit.collider.gameObject;

        // Comprobamos si el objeto bajo el mouse es una de nuestras esferas activas
        bool isHittingAnActiveSphere = false;
        foreach (var sphere in activeSpheres)
        {
            if (sphere == hoveredObject)
            {
                isHittingAnActiveSphere = true;
                break;
            }
        }

        // Nota: Como el mouse solo puede apuntar a un objeto a la vez en un raycast simple,
        // si tienes varias esferas separadas en el espacio, requeriría múltiples raycasts 
        // (o usar colliders/triggers de área). 
        // Si quieres que se detecte si el mouse está colisionando con una zona general 
        // o si te refieres a que el mouse pase por todas de forma secuencial sin perder el orden, 
        // avísame. Si tus esferas están juntas y forman un solo grupo, este enfoque de un único 
        // objeto detectado cambiaría a verificar triggers.

        return isHittingAnActiveSphere && activeSpheres.Length == 1;
        // (Nota: Si son varias esferas físicas separadas en pantalla, el mouse no puede tocarlas 
        // a todas al mismo tiempo con un solo puntero, a menos que uses Colliders grandes o áreas de Trigger).
    }

    void TimeOutTrigger()
    {
        string sceneToLoad = !string.IsNullOrEmpty(fallbackSceneOnTimeout) ? fallbackSceneOnTimeout : nextSceneName;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("Tiempo agotado. Cambiando a: " + sceneToLoad);
            LoadNextScene(sceneToLoad);
        }
    }

    void LoadNextScene(string scene)
    {
        if (!string.IsNullOrEmpty(scene))
        {
            SceneManager.LoadScene(scene);
        }
        else
        {
            Debug.LogWarning("Intentaste cargar una escena pero el nombre está vacío en el Inspector.");
        }
    }
}