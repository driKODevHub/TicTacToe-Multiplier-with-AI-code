using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro; // Додано для TextMeshPro

public class NetworkGameManager : NetworkBehaviour
{
    // --- Static variable to define the game mode ---
    public static bool isAiGame = false;

    [Header("Game Objects")]
    public GameObject xPrefab;
    public GameObject oPrefab;
    public Transform[] cellPositions;

    [Header("AI Settings")]
    [Range(0, 100)]
    public float aiErrorPercentage = 10f;

    [Header("UI Elements")]
    public TMP_Text statusText; // Змінено на TMP_Text

    // --- Network Variables (synchronized automatically) ---
    private NetworkList<int> boardState;
    private NetworkVariable<bool> isGameEnded = new NetworkVariable<bool>(false);
    private NetworkVariable<int> currentTurnPlayerId = new NetworkVariable<int>(0); // 0 or 1

    // --- Local Variables ---
    private AIPlayer aiPlayer;
    private List<GameObject> spawnedMarkers = new List<GameObject>();
    private bool localPlayerCanPlay = false;

    private void Awake()
    {
        boardState = new NetworkList<int>(new int[9], NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        aiPlayer = GetComponent<AIPlayer>();
        if (aiPlayer == null)
        {
            aiPlayer = gameObject.AddComponent<AIPlayer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to network variable changes
        boardState.OnListChanged += OnBoardStateChanged;
        isGameEnded.OnValueChanged += OnGameEndedChanged;
        currentTurnPlayerId.OnValueChanged += OnTurnChanged;

        // Initialize the board and UI on first spawn
        UpdateBoardVisuals();
        OnTurnChanged(-1, currentTurnPlayerId.Value);

        if (IsServer)
        {
            if (isAiGame)
            {
                StartNewGame();
            }
            else
            {
                UpdateStatus("Waiting for the second player...");
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            }
        }

        // Find UI elements dynamically if not assigned
        if (statusText == null)
        {
            statusText = FindFirstObjectByType<TMP_Text>();
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            UpdateStatus("Player connected! Starting the game...");
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            StartNewGame();
        }
    }


    private void StartNewGame()
    {
        if (!IsServer) return;

        isGameEnded.Value = false;

        for (int i = 0; i < 9; i++)
        {
            boardState[i] = 0; // 0 - empty
        }

        currentTurnPlayerId.Value = Random.Range(0, 2);
    }

    // Player clicks on a cell
    public void OnCellClicked(int cellIndex)
    {
        if (localPlayerCanPlay && !isGameEnded.Value)
        {
            RequestMoveServerRpc(cellIndex, NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestMoveServerRpc(int cellIndex, ulong senderClientId)
    {
        int playerIndex = (int)senderClientId;
        if (playerIndex != currentTurnPlayerId.Value || boardState[cellIndex] != 0 || isGameEnded.Value)
        {
            return; // Invalid move
        }

        int mark = (playerIndex == 0) ? 1 : 2; // Player 0 is X, Player 1 is O
        boardState[cellIndex] = mark;

        if (CheckForWinner())
        {
            return; // Game over
        }

        currentTurnPlayerId.Value = 1 - currentTurnPlayerId.Value;

        if (isAiGame && currentTurnPlayerId.Value == 1)
        {
            Invoke("MakeAIMove", 0.7f);
        }
    }

    private void MakeAIMove()
    {
        if (!IsServer || isGameEnded.Value) return;

        int[,] currentBoard = new int[3, 3];
        for (int i = 0; i < 9; i++) currentBoard[i / 3, i % 3] = boardState[i];

        int bestMoveIndex = aiPlayer.FindBestMove(currentBoard, 2, 1, aiErrorPercentage);

        if (bestMoveIndex != -1 && boardState[bestMoveIndex] == 0)
        {
            boardState[bestMoveIndex] = 2; // AI is always 'O' (mark 2)
        }

        if (CheckForWinner()) return;
        currentTurnPlayerId.Value = 0; // Give turn back to player
    }

    private bool CheckForWinner()
    {
        int[,] currentBoard = new int[3, 3];
        for (int i = 0; i < 9; i++) currentBoard[i / 3, i % 3] = boardState[i];

        int winnerMark = AIPlayer.CheckWinnerMark(currentBoard);
        if (winnerMark != 0)
        {
            isGameEnded.Value = true;
            return true;
        }

        bool isDraw = true;
        for (int i = 0; i < 9; i++) { if (boardState[i] == 0) isDraw = false; }

        if (isDraw)
        {
            isGameEnded.Value = true;
            return true;
        }

        return false;
    }

    private void OnBoardStateChanged(NetworkListEvent<int> changeEvent)
    {
        UpdateBoardVisuals();
    }

    private void OnTurnChanged(int previousValue, int newValue)
    {
        if (isGameEnded.Value) return;

        localPlayerCanPlay = (int)NetworkManager.Singleton.LocalClientId == newValue;

        if (isAiGame)
        {
            UpdateStatus(newValue == 0 ? "Your Turn" : "AI is thinking...");
        }
        else
        {
            UpdateStatus(localPlayerCanPlay ? "Your Turn" : "Opponent's Turn...");
        }
    }

    private void OnGameEndedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            int[,] currentBoard = new int[3, 3];
            for (int i = 0; i < 9; i++) currentBoard[i / 3, i % 3] = boardState[i];
            int winnerMark = AIPlayer.CheckWinnerMark(currentBoard);

            string message;
            if (winnerMark == 0) message = "It's a Draw!";
            else
            {
                int winnerId = (winnerMark == 1) ? 0 : 1;
                if (isAiGame)
                {
                    message = (winnerId == 0) ? "You Win!" : "AI Wins!";
                }
                else
                {
                    message = (winnerId == (int)NetworkManager.Singleton.LocalClientId) ? "You Win!" : "You Lose!";
                }
            }
            UpdateStatus(message + " (Server will shut down in 10s)");
            if (IsServer) Invoke("ShutdownServer", 10f);
        }
    }

    private void ShutdownServer()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void UpdateBoardVisuals()
    {
        foreach (var marker in spawnedMarkers) Destroy(marker);
        spawnedMarkers.Clear();

        for (int i = 0; i < 9; i++)
        {
            if (boardState[i] != 0)
            {
                GameObject prefab = boardState[i] == 1 ? xPrefab : oPrefab;
                GameObject newMarker = Instantiate(prefab, cellPositions[i].position, Quaternion.identity);
                spawnedMarkers.Add(newMarker);
            }
        }
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null) statusText.text = text;
    }
}

