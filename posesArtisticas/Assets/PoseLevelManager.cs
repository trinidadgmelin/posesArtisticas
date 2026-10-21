using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PoseLevelManager : MonoBehaviour
{
    [Header("Esferas Activas (Deben tocarse TODAS AL MISMO TIEMPO)")]
    public GameObject[] activeSpheres;

    [Header("UI y Escenas")]
    public TextMeshProUGUI scoreText;
    public string nextSceneName;
    public string skipSceneName;
    public string winSceneName = "ganar";       // Escena a la que va si llega a 100 puntos
    public string timeoutSceneName = "perder";   // Escena a la que va si se acaba el tiempo

    [Header("Configuración de Tiempo Límite")]
    public float timeLimit = 30f;

    private float timer;
    private bool nivelCompletado = false;
    private static int currentScore = 0; // Static para que se acumule entre escenas

    void Start()
    {
        timer = timeLimit;
        UpdateScoreUI();
    }

    void Update()
    {
        if (nivelCompletado) return;

        // 1. Tecla E para saltar nivel (atajo preservado)
        if (Input.GetKeyDown(KeyCode.E))
        {
            LoadScene(!string.IsNullOrEmpty(skipSceneName) ? skipSceneName : nextSceneName);
            return;
        }

        // 2. Temporizador
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            TimeOutTrigger();
            return;
        }

        // 3. Verificación estricta de hover simultáneo
        VerificarHoverSimultaneo();
    }

    void VerificarHoverSimultaneo()
    {
        if (activeSpheres == null || activeSpheres.Length == 0) return;

        bool todasTocadasAlMismoTiempo = true;
        int sumaNivelParcial = 0;

        foreach (var sphere in activeSpheres)
        {
            if (sphere != null)
            {
                valorEsfera val = sphere.GetComponent<valorEsfera>();
                if (val != null)
                {
                    if (!val.isHovered)
                    {
                        todasTocadasAlMismoTiempo = false;
                        break;
                    }
                    else
                    {
                        sumaNivelParcial += val.scoreValue;
                    }
                }
                else
                {
                    todasTocadasAlMismoTiempo = false;
                    break;
                }
            }
        }

        // Si TODAS las esferas de la lista están siendo tocadas al mismo tiempo
        if (todasTocadasAlMismoTiempo)
        {
            nivelCompletado = true;
            currentScore += sumaNivelParcial; // Sumamos al acumulado global
            UpdateScoreUI();

            Debug.Log("¡Pose simultánea completada! Puntos totales acumulados: " + currentScore);

            // Verificamos si con esto alcanzó o superó los 100 puntos
            if (currentScore >= 100)
            {
                Invoke("WinGameCompletely", 1f);
            }
            else
            {
                Invoke("WinGame", 1f);
            }
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Puntos: " + currentScore;
        }
    }

    void WinGame()
    {
        LoadScene(nextSceneName);
    }

    void WinGameCompletely()
    {
        LoadScene(winSceneName);
    }

    void TimeOutTrigger()
    {
        LoadScene(timeoutSceneName);
    }

    void LoadScene(string scene)
    {
        if (!string.IsNullOrEmpty(scene))
        {
            SceneManager.LoadScene(scene);
        }
    }
}