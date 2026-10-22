using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PoseLevelManager : MonoBehaviour
{
    [Header("Identificación del Nivel")]
    [Tooltip("Poné 1 para el Nivel 1, 2 para el Nivel 2, etc.")]
    public int levelIndex = 1; // <-- NUEVO: Identifica cuál es este nivel

    public static int lastCompletedLevel = 1; // <-- NUEVO: Recuerda cuál fue el último nivel ganado

    [Header("Esferas Activas (Deben tocarse TODAS AL MISMO TIEMPO)")]
    public GameObject[] activeSpheres;

    [Header("UI y Escenas")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public string nextSceneName = "RevelacionCuadro"; // Apunta siempre a la escena del cuadro
    public string skipSceneName;
    public string winSceneName = "ganar";
    public string timeoutSceneName = "perder";

    [Header("Configuración de Tiempo Límite")]
    public float timeLimit = 30f;

    private float timer;
    private bool nivelCompletado = false;
    private static int currentScore = 0;

    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "tutoFin" || currentScene == winSceneName || currentScene == timeoutSceneName)
        {
            currentScore = 0;
        }

        timer = timeLimit;
        UpdateScoreUI();
        UpdateTimerUI();
    }

    void Update()
    {
        if (nivelCompletado) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            LoadScene(!string.IsNullOrEmpty(skipSceneName) ? skipSceneName : nextSceneName);
            return;
        }

        timer -= Time.deltaTime;
        UpdateTimerUI();

        if (timer <= 0f)
        {
            timer = 0f;
            UpdateTimerUI();
            TimeOutTrigger();
            return;
        }

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

        if (todasTocadasAlMismoTiempo)
        {
            nivelCompletado = true;
            currentScore += sumaNivelParcial;
            UpdateScoreUI();

            // <-- NUEVO: Guardamos qué nivel se completó justo antes de cambiar de escena
            lastCompletedLevel = levelIndex;

            Debug.Log("¡Pose completada del Nivel " + levelIndex + "! Puntos totales: " + currentScore);

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

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Tiempo: " + Mathf.Ceil(timer).ToString();
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