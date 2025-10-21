using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static GameManager;

public class GameManager : NetworkBehaviour
{


    public static GameManager Instance { get; private set; }


    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x;
        public int y;
        public PlayerType playerType;
    }
    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;
    public event EventHandler<OnRemovedObjectEventArgs> OnRemovedObject;
    public class OnRemovedObjectEventArgs : EventArgs
    {
        public int x;
        public int y;
    }


    public enum PlayerType
    {
        None,
        Cross,
        Circle,
    }

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }

    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }

    public struct PlacedObjectData : INetworkSerializable, IEquatable<PlacedObjectData>
    {
        public int x;
        public int y;
        public PlayerType playerType;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref playerType);
        }

        public bool Equals(PlacedObjectData other)
        {
            return x == other.x && y == other.y && playerType == other.playerType;
        }
    }


    private const int MAX_PLACED_OBJECTS_PER_PLAYER = 3;


    private PlayerType localPlayerType;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private NetworkVariable<SerializablePlayerTypeArray> playerTypeArrayNetwork = new NetworkVariable<SerializablePlayerTypeArray>();
    private List<Line> lineList;
    private NetworkVariable<int> playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> playerCircleScore = new NetworkVariable<int>();

    private NetworkList<PlacedObjectData> crossPlacedObjects;
    private NetworkList<PlacedObjectData> circlePlacedObjects;

    public NetworkVariable<Vector2Int> CrossNextToRemove { get; private set; } = new NetworkVariable<Vector2Int>(new Vector2Int(-1, -1));
    public NetworkVariable<Vector2Int> CircleNextToRemove { get; private set; } = new NetworkVariable<Vector2Int>(new Vector2Int(-1, -1));

    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);


    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager instance!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        crossPlacedObjects = new NetworkList<PlacedObjectData>();
        circlePlacedObjects = new NetworkList<PlacedObjectData>();

        lineList = new List<Line> {
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), }, centerGridPosition = new Vector2Int(1, 0), orientation = Orientation.Horizontal, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), }, centerGridPosition = new Vector2Int(1, 1), orientation = Orientation.Horizontal, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), }, centerGridPosition = new Vector2Int(1, 2), orientation = Orientation.Horizontal, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), }, centerGridPosition = new Vector2Int(0, 1), orientation = Orientation.Vertical, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), }, centerGridPosition = new Vector2Int(1, 1), orientation = Orientation.Vertical, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), }, centerGridPosition = new Vector2Int(2, 1), orientation = Orientation.Vertical, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(1,1), new Vector2Int(2,2), }, centerGridPosition = new Vector2Int(1, 1), orientation = Orientation.DiagonalA, },
            new Line { gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2), new Vector2Int(1,1), new Vector2Int(2,0), }, centerGridPosition = new Vector2Int(1, 1), orientation = Orientation.DiagonalB, },
        };
    }


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerTypeArrayNetwork.Value = new SerializablePlayerTypeArray(3, 3);
        }

        localPlayerType = NetworkManager.Singleton.IsHost ? PlayerType.Cross : PlayerType.Circle;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        // ЗМІНЕНО: Підписка на зміну гравця для оновлення логіки "вицвітання"
        currentPlayablePlayerType.OnValueChanged += OnCurrentPlayablePlayerTypeChanged_UpdateFading;

        playerCrossScore.OnValueChanged += (prev, next) => OnScoreChanged?.Invoke(this, EventArgs.Empty);
        playerCircleScore.OnValueChanged += (prev, next) => OnScoreChanged?.Invoke(this, EventArgs.Empty);

        isGameStarted.OnValueChanged += OnIsGameStartedChanged;
    }

    // НОВИЙ МЕТОД: Цей метод буде викликатись щоразу, коли змінюється гравець
    private void OnCurrentPlayablePlayerTypeChanged_UpdateFading(PlayerType previousValue, PlayerType newValue)
    {
        OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        if (IsServer)
        {
            UpdateNextToRemovePositions(newValue);
        }
    }

    private void OnIsGameStartedChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            OnGameStarted?.Invoke(this, EventArgs.Empty);
        }
    }

    // ЗМІНЕНО: Цей метод тепер приймає аргумент PlayerType
    private void UpdateNextToRemovePositions(PlayerType forPlayerType)
    {
        // Спочатку скидаємо обидва значення
        CrossNextToRemove.Value = new Vector2Int(-1, -1);
        CircleNextToRemove.Value = new Vector2Int(-1, -1);

        // Потім встановлюємо значення тільки для АКТУАЛЬНОГО гравця, якщо в нього 3 фігури
        if (forPlayerType == PlayerType.Cross && crossPlacedObjects.Count >= MAX_PLACED_OBJECTS_PER_PLAYER)
        {
            CrossNextToRemove.Value = new Vector2Int(crossPlacedObjects[0].x, crossPlacedObjects[0].y);
        }
        else if (forPlayerType == PlayerType.Circle && circlePlacedObjects.Count >= MAX_PLACED_OBJECTS_PER_PLAYER)
        {
            CircleNextToRemove.Value = new Vector2Int(circlePlacedObjects[0].x, circlePlacedObjects[0].y);
        }
    }


    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
            isGameStarted.Value = true;
        }
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        if (playerType != currentPlayablePlayerType.Value) return;

        PlayerType[,] currentArray = playerTypeArrayNetwork.Value.GetArray();
        if (currentArray[x, y] != PlayerType.None) return;

        if (playerType == PlayerType.Cross)
        {
            if (crossPlacedObjects.Count >= MAX_PLACED_OBJECTS_PER_PLAYER)
            {
                PlacedObjectData oldestCross = crossPlacedObjects[0];
                crossPlacedObjects.RemoveAt(0);
                currentArray[oldestCross.x, oldestCross.y] = PlayerType.None;
                TriggerOnRemovedObjectRpc(oldestCross.x, oldestCross.y);
            }
            crossPlacedObjects.Add(new PlacedObjectData { x = x, y = y, playerType = playerType });
        }
        else
        {
            if (circlePlacedObjects.Count >= MAX_PLACED_OBJECTS_PER_PLAYER)
            {
                PlacedObjectData oldestCircle = circlePlacedObjects[0];
                circlePlacedObjects.RemoveAt(0);
                currentArray[oldestCircle.x, oldestCircle.y] = PlayerType.None;
                TriggerOnRemovedObjectRpc(oldestCircle.x, oldestCircle.y);
            }
            circlePlacedObjects.Add(new PlacedObjectData { x = x, y = y, playerType = playerType });
        }

        currentArray[x, y] = playerType;
        playerTypeArrayNetwork.Value = new SerializablePlayerTypeArray(currentArray);

        GameVisualManager.Instance.RequestSpawnVisualServerRpc(x, y, playerType);

        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs { x = x, y = y, playerType = playerType });
        TriggerOnPlacedObjectRpc();

        if (TestWinner())
        {
            return;
        }

        if (IsBoardFull())
        {
            TriggerOnGameTiedRpc();
            return;
        }

        currentPlayablePlayerType.Value = (playerType == PlayerType.Cross) ? PlayerType.Circle : PlayerType.Cross;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRemovedObjectRpc(int x, int y)
    {
        OnRemovedObject?.Invoke(this, new OnRemovedObjectEventArgs { x = x, y = y });
    }

    private bool TestWinnerLine(Line line, PlayerType[,] currentArray)
    {
        PlayerType first = currentArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y];
        if (first == PlayerType.None) return false;

        return first == currentArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y] &&
               first == currentArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y];
    }

    private bool TestWinner()
    {
        PlayerType[,] currentArray = playerTypeArrayNetwork.Value.GetArray();
        foreach (Line line in lineList)
        {
            if (TestWinnerLine(line, currentArray))
            {
                PlayerType winPlayerType = currentArray[line.centerGridPosition.x, line.centerGridPosition.y];
                if (winPlayerType != PlayerType.None)
                {
                    currentPlayablePlayerType.Value = PlayerType.None;
                    if (winPlayerType == PlayerType.Cross) playerCrossScore.Value++;
                    else playerCircleScore.Value++;
                    TriggerOnGameWinRpc(Array.IndexOf(lineList.ToArray(), line), winPlayerType);
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsBoardFull()
    {
        PlayerType[,] currentArray = playerTypeArrayNetwork.Value.GetArray();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (currentArray[x, y] == PlayerType.None)
                {
                    return false;
                }
            }
        }
        return true;
    }


    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        currentPlayablePlayerType.Value = PlayerType.None;
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = lineList[lineIndex],
            winPlayerType = winPlayerType,
        });
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        playerTypeArrayNetwork.Value = new SerializablePlayerTypeArray(3, 3);
        crossPlacedObjects.Clear();
        circlePlacedObjects.Clear();

        isGameStarted.Value = false;
        isGameStarted.Value = true;

        currentPlayablePlayerType.Value = PlayerType.Cross;

        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }

    public PlayerType GetLocalPlayerType() => localPlayerType;
    public PlayerType GetCurrentPlayablePlayerType() => currentPlayablePlayerType.Value;
    public void GetScores(out int pCrossScore, out int pCircleScore)
    {
        pCrossScore = playerCrossScore.Value;
        pCircleScore = playerCircleScore.Value;
    }

    public PlayerType GetPlayerTypeAt(int x, int y)
    {
        if (playerTypeArrayNetwork.Value.GetArray() == null) return PlayerType.None;
        return playerTypeArrayNetwork.Value.GetArray()[x, y];
    }
}

public struct SerializablePlayerTypeArray : INetworkSerializable, IEquatable<SerializablePlayerTypeArray>
{
    private PlayerType[,] array;

    public SerializablePlayerTypeArray(int rows, int cols)
    {
        array = new PlayerType[rows, cols];
    }

    public SerializablePlayerTypeArray(PlayerType[,] source)
    {
        if (source == null)
        {
            array = new PlayerType[0, 0];
            return;
        }
        array = (PlayerType[,])source.Clone();
    }

    public PlayerType[,] GetArray() => array;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int rows = 0, cols = 0;
        if (!serializer.IsReader)
        {
            rows = array?.GetLength(0) ?? 0;
            cols = array?.GetLength(1) ?? 0;
        }

        serializer.SerializeValue(ref rows);
        serializer.SerializeValue(ref cols);

        if (serializer.IsReader)
        {
            array = new PlayerType[rows, cols];
        }

        if (array != null)
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    serializer.SerializeValue(ref array[r, c]);
                }
            }
        }
    }

    public bool Equals(SerializablePlayerTypeArray other)
    {
        if (array == null && other.array == null) return true;
        if (array == null || other.array == null) return false;
        return array.Equals(other.array);
    }
}

