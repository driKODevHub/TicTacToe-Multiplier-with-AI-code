using UnityEngine;

public class Cell : MonoBehaviour
{
    [Tooltip("Індекс цієї клітинки (від 0 до 8)")]
    public int cellIndex;

    // OnMouseDown() та Start() більше не потрібні.
    // GameManager тепер сам знаходить клітинки за допомогою Raycast.
}

