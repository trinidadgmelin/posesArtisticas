using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PoseLevelManager : MonoBehaviour
{
    [Header("Configuración de Flujo")]
    [SerializeField] private string siguienteEscena; // Ej: "pose2", "pose3", "pose4" o "ganar"
    [SerializeField] private string escenaPerder = "perder";
    [SerializeField] private float tiempoLimite = 10f;

    [Header("UI del Timer (Opcional)")]
    [SerializeField] private Image timerImage;
    [SerializeField] private Sprite[] timerSprites; // Si usás una barra de sprites progresiva

    private float tiempoRestante;
    private bool nivelTerminado = false;

    void Start()
    {
        tiempoRestante = tiempoLimite;

        if (timerImage != null && timerSprites != null && timerSprites.Length > 0)
        {
            timerImage.sprite = timerSprites[0];
        }
    }

    void Update()
    {
        if (nivelTerminado) return;

        // 1. Manejo del Timer
        tiempoRestante -= Time.deltaTime;

        ActualizarUI();

        if (tiempoRestante <= 0f)
        {
            nivelTerminado = true;
            CargarEscena(escenaPerder);
            return;
        }

        // 2. Input para avanzar de pose
        if (Input.GetKeyDown(KeyCode.E))
        {
            nivelTerminado = true;
            CargarEscena(siguienteEscena);
        }
    }

    private void ActualizarUI()
    {
        if (timerImage == null || timerSprites == null || timerSprites.Length == 0) return;

        // Calcula qué sprite mostrar según la proporción de tiempo transcurrido
        float tiempoTranscurrido = tiempoLimite - tiempoRestante;
        float porcentaje = tiempoTranscurrido / tiempoLimite;

        int spriteIndex = Mathf.FloorToInt(porcentaje * timerSprites.Length);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, timerSprites.Length - 1);

        timerImage.sprite = timerSprites[spriteIndex];
    }

    private void CargarEscena(string nombreEscena)
    {
        if (!string.IsNullOrEmpty(nombreEscena))
        {
            SceneManager.LoadScene(nombreEscena);
        }
        else
        {
            Debug.LogError("No se asignó la escena de destino en el Inspector.");
        }
    }
}