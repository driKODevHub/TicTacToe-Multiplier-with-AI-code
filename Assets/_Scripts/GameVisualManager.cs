using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    public static GameVisualManager Instance { get; private set; }

    private const float GRID_SIZE = 3.1f;
    private readonly Color FADE_COLOR = new Color(1f, 1f, 1f, 0.3f);
    private readonly Color NORMAL_COLOR = new Color(1f, 1f, 1f, 1f);

    [SerializeField] private Transform crossPrefab;
    [SerializeField] private Transform circlePrefab;
    [SerializeField] private Transform lineCompletePrefab;

    private Dictionary<Vector2Int, GameObject> visualGameObjectDictionary;
    private GameObject crossToRemoveVisual;
    private GameObject circleToRemoveVisual;

    // ДОДАНО: Посилання на об'єкт переможної лінії
    private GameObject winLineGameObject;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameVisualManager instance!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        visualGameObjectDictionary = new Dictionary<Vector2Int, GameObject>();
    }

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
        GameManager.Instance.OnRemovedObject += GameManager_OnRemovedObject;
        GameManager.Instance.OnGameStarted += (s, e) => ClearAllVisuals();

        GameManager.Instance.CrossNextToRemove.OnValueChanged += OnNextToRemoveChanged;
        GameManager.Instance.CircleNextToRemove.OnValueChanged += OnNextToRemoveChanged;
    }

    private void OnNextToRemoveChanged(Vector2Int previousValue, Vector2Int newValue)
    {
        UpdateFadedVisuals();
    }

    private void UpdateFadedVisuals()
    {
        if (crossToRemoveVisual != null)
        {
            SpriteRenderer sr = crossToRemoveVisual.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = NORMAL_COLOR;
            crossToRemoveVisual = null;
        }
        if (circleToRemoveVisual != null)
        {
            SpriteRenderer sr = circleToRemoveVisual.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = NORMAL_COLOR;
            circleToRemoveVisual = null;
        }

        Vector2Int crossPos = GameManager.Instance.CrossNextToRemove.Value;
        if (crossPos.x != -1 && visualGameObjectDictionary.TryGetValue(crossPos, out crossToRemoveVisual))
        {
            SpriteRenderer sr = crossToRemoveVisual.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = FADE_COLOR;
        }

        Vector2Int circlePos = GameManager.Instance.CircleNextToRemove.Value;
        if (circlePos.x != -1 && visualGameObjectDictionary.TryGetValue(circlePos, out circleToRemoveVisual))
        {
            SpriteRenderer sr = circleToRemoveVisual.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = FADE_COLOR;
        }
    }

    private void GameManager_OnRemovedObject(object sender, GameManager.OnRemovedObjectEventArgs e)
    {
        Vector2Int removedPos = new Vector2Int(e.x, e.y);
        if (visualGameObjectDictionary.TryGetValue(removedPos, out GameObject objToDestroy))
        {
            visualGameObjectDictionary.Remove(removedPos);
            Destroy(objToDestroy);
        }
    }

    private void ClearAllVisuals()
    {
        foreach (GameObject visualGameObject in visualGameObjectDictionary.Values)
        {
            if (visualGameObject != null) Destroy(visualGameObject);
        }
        visualGameObjectDictionary.Clear();
        crossToRemoveVisual = null;
        circleToRemoveVisual = null;

        // ДОДАНО: Також знищуємо локальний об'єкт лінії, якщо він є
        if (winLineGameObject != null)
        {
            Destroy(winLineGameObject);
            winLineGameObject = null;
        }
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        if (IsServer)
        {
            ClearAllVisualsServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClearAllVisualsServerRpc()
    {
        // ДОДАНО: Логіка для деспавну переможної лінії на сервері
        if (winLineGameObject != null)
        {
            if (winLineGameObject.TryGetComponent<NetworkObject>(out var lineNetObj) && lineNetObj.IsSpawned)
            {
                lineNetObj.Despawn();
            }
        }

        foreach (var visual in visualGameObjectDictionary.Values)
        {
            if (visual != null && visual.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
            }
        }
        ClearAllVisualsClientRpc();
    }

    [ClientRpc]
    private void ClearAllVisualsClientRpc()
    {
        ClearAllVisuals();
    }


    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (!IsServer) return;

        float eulerZ = 0f;
        switch (e.line.orientation)
        {
            default:
            case GameManager.Orientation.Horizontal: eulerZ = 0f; break;
            case GameManager.Orientation.Vertical: eulerZ = 90f; break;
            case GameManager.Orientation.DiagonalA: eulerZ = 45f; break;
            case GameManager.Orientation.DiagonalB: eulerZ = -45f; break;
        }
        Transform lineTransform = Instantiate(lineCompletePrefab, GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y), Quaternion.Euler(0, 0, eulerZ));

        // ДОДАНО: Зберігаємо посилання на створену лінію
        winLineGameObject = lineTransform.gameObject;

        lineTransform.GetComponent<NetworkObject>().Spawn(true);
    }

    public override void OnNetworkDespawn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWin -= GameManager_OnGameWin;
            GameManager.Instance.OnRematch -= GameManager_OnRematch;
            GameManager.Instance.OnRemovedObject -= GameManager_OnRemovedObject;
            GameManager.Instance.CrossNextToRemove.OnValueChanged -= OnNextToRemoveChanged;
            GameManager.Instance.CircleNextToRemove.OnValueChanged -= OnNextToRemoveChanged;
        }
        base.OnNetworkDespawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnVisualServerRpc(int x, int y, GameManager.PlayerType playerType)
    {
        Transform prefab = playerType == GameManager.PlayerType.Cross ? crossPrefab : circlePrefab;
        Transform visualTransform = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);

        NetworkObject netObj = visualTransform.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        AddVisualToDictionaryClientRpc(netObj.NetworkObjectId, x, y);
    }

    [ClientRpc]
    private void AddVisualToDictionaryClientRpc(ulong networkObjectId, int x, int y)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            visualGameObjectDictionary[new Vector2Int(x, y)] = netObj.gameObject;
            UpdateFadedVisuals();
        }
    }


    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }
}

