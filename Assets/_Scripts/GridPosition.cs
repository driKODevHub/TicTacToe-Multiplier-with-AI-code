using UnityEngine;

public class GridPosition : MonoBehaviour {


    [SerializeField] private int x;
    [SerializeField] private int y;


    private void OnMouseDown() {
        // �볺������ ��������: �� ����� �������?
        if (GameManager.Instance.GetPlayerTypeAt(x, y) != GameManager.PlayerType.None) {
            Debug.Log("Cell is already occupied.");
            return; // �� ���������� RPC, ���� ������� �������
        }
        
        // �볺������ ��������: �� ����� ��� ����� ������?
        if (GameManager.Instance.GetCurrentPlayablePlayerType() != GameManager.Instance.GetLocalPlayerType()) {
            Debug.Log("Not your turn.");
            return; // �� ���������� RPC, ���� �� ��� ���
        }

        // ³���������� ��� �� ������ ��� �������
        GameManager.Instance.ClickedOnGridPositionRpc(x, y, GameManager.Instance.GetLocalPlayerType());
    }

}

