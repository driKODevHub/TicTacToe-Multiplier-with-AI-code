using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Додано для TextMeshPro
using System.Threading.Tasks;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField joinCodeInput; // Змінено на TMP_InputField
    public TMP_Text statusText;          // Змінено на TMP_Text
    public Button createButton;
    public Button joinButton;
    public Button aiGameButton;

    [Header("Prefabs")]
    public GameObject networkManagerPrefab;

    private NetworkManager networkManager;

    async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            UpdateStatus($"Player ID: {AuthenticationService.Instance.PlayerId}");
        }
    }

    public async void CreateGameRelay()
    {
        DisableButtons();
        UpdateStatus("Creating Game...");
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            UpdateStatus($"Game Created! Code: {joinCode}");

            UnityTransport transport = GetTransport();
            transport.SetRelayServerData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

            InstantiateNetworkManager();
            networkManager.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            UpdateStatus("Failed to create game.");
            EnableButtons();
        }
    }

    public async void JoinGameRelay()
    {
        DisableButtons();
        UpdateStatus("Joining Game...");
        try
        {
            string code = joinCodeInput.text;
            if (string.IsNullOrWhiteSpace(code))
            {
                UpdateStatus("Please enter a code!");
                EnableButtons();
                return;
            }

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            UnityTransport transport = GetTransport();
            transport.SetRelayServerData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

            InstantiateNetworkManager();
            networkManager.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
            UpdateStatus("Failed to join game.");
            EnableButtons();
        }
    }

    public void StartHostAI()
    {
        DisableButtons();
        UpdateStatus("Starting AI Game...");
        InstantiateNetworkManager();
        if (networkManager != null)
        {
            // We need to attach NetworkGameManager to the same object as NetworkManager
            NetworkGameManager gameManager = networkManager.gameObject.AddComponent<NetworkGameManager>();
            // Here you might need to assign prefabs and other settings to the gameManager
            // This part might need further refinement based on your project structure
        }
        NetworkGameManager.isAiGame = true;
        networkManager.StartHost();
    }

    private void InstantiateNetworkManager()
    {
        if (networkManager == null)
        {
            GameObject nmObject = Instantiate(networkManagerPrefab);
            networkManager = nmObject.GetComponent<NetworkManager>();
        }
    }

    private UnityTransport GetTransport()
    {
        InstantiateNetworkManager();
        return networkManager.GetComponent<UnityTransport>();
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void DisableButtons()
    {
        createButton.interactable = false;
        joinButton.interactable = false;
        aiGameButton.interactable = false;
    }

    private void EnableButtons()
    {
        createButton.interactable = true;
        joinButton.interactable = true;
        aiGameButton.interactable = true;
    }
}

