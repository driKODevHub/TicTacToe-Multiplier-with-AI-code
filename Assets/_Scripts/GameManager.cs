using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    // --- ������ ���� ��� ������������ � Unity Editor ---

    [Header("����� ��'����")]
    [Tooltip("������ ��� ������ 'X'")]
    public GameObject xPrefab;
    [Tooltip("������ ��� ������ 'O'")]
    public GameObject oPrefab;
    [Tooltip("����� ���������� ��� 9 ������� �� ���")]
    public Transform[] cellPositions;

    [Header("������������ ز")]
    [Tooltip("����, � ���� ز ������� ����������, � �� ��������� ��� (�� 0 �� 100)")]
    [Range(0, 100)]
    public float aiErrorPercentage = 10f;

    [Header("UI �������� (�����������)")]
    [Tooltip("�������� ���� ��� ����������� ������� ���")]
    public UnityEngine.UI.Text statusText;


    // --- ������� ���� ---

    private int[,] boardState = new int[3, 3];
    private bool isPlayerTurn;
    private bool isGameEnded = false;
    private int playerMark = 1;
    private int aiMark = 2;
    private AIPlayer aiPlayer;
    private List<GameObject> spawnedMarkers = new List<GameObject>();
    private bool isRestartRequested = false;


    void Start()
    {
        aiPlayer = GetComponent<AIPlayer>();
        if (aiPlayer == null)
        {
            aiPlayer = gameObject.AddComponent<AIPlayer>();
        }
        StartNewGame();
    }

    public void StartNewGame()
    {
        // ��������� ����-�� ���������� �������, ��� �������� ���� ��� �������� �������
        CancelInvoke();
        isGameEnded = false;
        isRestartRequested = false;
        ClearBoard();

        boardState = new int[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                boardState[i, j] = 0;
            }
        }

        if (Random.Range(0, 2) == 0)
        {
            playerMark = 1;
            aiMark = 2;
            isPlayerTurn = true;
            UpdateStatus("��� ��� (X)");
        }
        else
        {
            playerMark = 2;
            aiMark = 1;
            isPlayerTurn = false;
            UpdateStatus("ճ� ز (X)");
            Invoke("MakeAIMove", 1f);
        }
    }

    public void PlayerMove(int cellIndex)
    {
        if (!isPlayerTurn || isGameEnded || isRestartRequested)
            return;

        int row = cellIndex / 3;
        int col = cellIndex % 3;

        if (boardState[row, col] == 0)
        {
            PlaceMarker(cellIndex, playerMark);
            boardState[row, col] = playerMark;

            if (CheckForWinner())
            {
                return;
            }

            isPlayerTurn = false;
            UpdateStatus("ճ� ز...");
            Invoke("MakeAIMove", 0.7f);
        }
    }

    private void MakeAIMove()
    {
        if (isGameEnded) return;

        int bestMoveIndex = aiPlayer.FindBestMove(boardState, aiMark, playerMark, aiErrorPercentage);

        if (bestMoveIndex != -1)
        {
            PlaceMarker(bestMoveIndex, aiMark);
            int row = bestMoveIndex / 3;
            int col = bestMoveIndex % 3;
            boardState[row, col] = aiMark;
        }

        if (CheckForWinner())
        {
            return;
        }

        isPlayerTurn = true;
        UpdateStatus("��� ���");
    }

    private void PlaceMarker(int cellIndex, int mark)
    {
        GameObject prefabToSpawn = (mark == 1) ? xPrefab : oPrefab;
        if (prefabToSpawn != null && cellPositions[cellIndex] != null)
        {
            GameObject newMarker = Instantiate(prefabToSpawn, cellPositions[cellIndex].position, Quaternion.identity);
            spawnedMarkers.Add(newMarker);
        }
    }

    private bool CheckForWinner()
    {
        int winner = AIPlayer.EvaluateBoard(boardState);

        if (winner == aiMark)
        {
            EndGame("ز ������!");
            return true;
        }
        if (winner == playerMark)
        {
            EndGame("�� ���������!");
            return true;
        }

        bool isDraw = true;
        foreach (int cell in boardState)
        {
            if (cell == 0)
            {
                isDraw = false;
                break;
            }
        }

        if (isDraw)
        {
            EndGame("ͳ���!");
            return true;
        }

        return false;
    }

    private void EndGame(string message)
    {
        isGameEnded = true;
        UpdateStatus(message + " �������� 'R' ��� ��������.");
    }

    void Update()
    {
        if (Keyboard.current == null) return; // ���������, ���� ������� ����� �� ���������

        // ������� ϲ��� ���������� ��� (������ R)
        if (isGameEnded && Keyboard.current.rKey.wasPressedThisFrame)
        {
            StartNewGame();
        }

        // ������� ϲ� ��� ��� (Shift + R)
        if (!isGameEnded && !isRestartRequested)
        {
            bool isShiftPressed = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            if (isShiftPressed && Keyboard.current.rKey.wasPressedThisFrame)
            {
                isRestartRequested = true;
                UpdateStatus("�� ��������� �������. ز �����������...");
                Invoke("StartNewGame", 1.5f); // ��������� ������� � ��������� ���������
            }
        }

        // ������� ���� ����
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && isPlayerTurn && !isGameEnded && !isRestartRequested)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                Cell clickedCell = hit.collider.GetComponent<Cell>();
                if (clickedCell != null)
                {
                    PlayerMove(clickedCell.cellIndex);
                }
            }
        }
    }

    private void ClearBoard()
    {
        foreach (GameObject marker in spawnedMarkers)
        {
            Destroy(marker);
        }
        spawnedMarkers.Clear();
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
        else
        {
            Debug.Log(text);
        }
    }
}

