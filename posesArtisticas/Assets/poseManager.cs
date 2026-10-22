using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class poseManager : MonoBehaviour
{
    [Header("UI del Cuadro")]
    public Image displayImage;                  // Componente Image de UI donde se muestra el cuadro
    public TextMeshProUGUI paintingNameText;    // (Opcional) Texto con el nombre del cuadro

    [Header("Recursos por Nivel (Elemento 0 = Nivel 1, Elemento 1 = Nivel 2, etc.)")]
    public Sprite[] cuadroSprites;             // Arrastrá acá las 5 imágenes de los cuadros en orden
    public string[] cuadroNombres;             // (Opcional) Nombres o descripciones de los cuadros

    [Header("Navegación")]
    public float displayDuration = 5f;         // Cuántos segundos se muestra antes de avanzar al siguiente nivel

    void Start()
    {
        int nivelGuardado = PoseLevelManager.lastCompletedLevel;
        int arrayIndex = nivelGuardado - 1; // Convertimos (Nivel 1 -> Índice 0)

        // Mostrar la imagen del cuadro correspondiente al nivel
        if (cuadroSprites != null && arrayIndex >= 0 && arrayIndex < cuadroSprites.Length)
        {
            if (displayImage != null)
            {
                displayImage.sprite = cuadroSprites[arrayIndex];
            }
        }

        // Mostrar el nombre/descripción del cuadro
        if (cuadroNombres != null && arrayIndex >= 0 && arrayIndex < cuadroNombres.Length)
        {
            if (paintingNameText != null)
            {
                paintingNameText.text = cuadroNombres[arrayIndex];
            }
        }

        // Programar el paso al siguiente nivel automáticamente
        Invoke("IrAlSiguienteNivel", displayDuration);
    }

    void Update()
    {
        // Presionar espacio o click para continuar rápido sin esperar
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            CancelInvoke("IrAlSiguienteNivel");
            IrAlSiguienteNivel();
        }
    }

    void IrAlSiguienteNivel()
    {
        int proximoNivel = PoseLevelManager.lastCompletedLevel + 1;

        // Si hay más niveles, carga "Nivel2", "Nivel3", etc.
        if (proximoNivel <= 5)
        {
            SceneManager.LoadScene("Nivel" + proximoNivel);
        }
        else
        {
            // Si ya hizo los 5 niveles, va a la pantalla de ganar
            SceneManager.LoadScene("ganar");
        }
    }
}