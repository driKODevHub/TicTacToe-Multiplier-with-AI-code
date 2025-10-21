using UnityEngine;

public class GridPosition : MonoBehaviour {


    [SerializeField] private int x;
    [SerializeField] private int y;


    private void OnMouseDown() {
        // Клієнтська перевірка: чи вільна клітинка?
        if (GameManager.Instance.GetPlayerTypeAt(x, y) != GameManager.PlayerType.None) {
            Debug.Log("Cell is already occupied.");
            return; // Не відправляти RPC, якщо клітинка зайнята
        }
        
        // Клієнтська перевірка: чи зараз хід цього гравця?
        if (GameManager.Instance.GetCurrentPlayablePlayerType() != GameManager.Instance.GetLocalPlayerType()) {
            Debug.Log("Not your turn.");
            return; // Не відправляти RPC, якщо не наш хід
        }

        // Відправляємо клік на сервер для обробки
        GameManager.Instance.ClickedOnGridPositionRpc(x, y, GameManager.Instance.GetLocalPlayerType());
    }

}

