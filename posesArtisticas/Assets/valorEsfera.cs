using UnityEngine;

public class valorEsfera : MonoBehaviour
{
    [Tooltip("Puntos que otorga esta esfera según su tamaño (ej: 5, 10 o 20).")]
    public int scoreValue = 20;

    [HideInInspector]
    public bool isHovered = false;

    // --- Detección por Mouse (para pruebas rápidas en PC) ---
    void OnMouseEnter()
    {
        isHovered = true;
    }

    void OnMouseExit()
    {
        isHovered = false;
    }

    // --- Detección óptica 2D / Colliders del cuerpo (cuando implementes la captura) ---
    void OnTriggerStay(Collider other)
    {
        isHovered = true;
    }

    void OnTriggerExit(Collider other)
    {
        isHovered = false;
    }
}