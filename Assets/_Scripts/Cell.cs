using UnityEngine;

public class Cell : MonoBehaviour
{
    [Tooltip("The index of this cell (0 to 8)")]
    public int cellIndex;

    // OnMouseDown() and Start() are no longer needed.
    // The GameManager now finds cells using Raycast.
}

