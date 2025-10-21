using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Button kickPlayerButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject readyGameObject; // The indicator (check mark, text, etc.)

    private Player player;
    private bool isReady = false;

    private void Awake()
    {
        kickPlayerButton.onClick.AddListener(KickPlayer);

        readyButton.onClick.AddListener(() => {
            isReady = !isReady;
            LobbyManager.Instance.SetPlayerReadyStatus(isReady);
            UpdateReadyButtonText();
        });
    }

    public void SetKickPlayerButtonVisible(bool visible)
    {
        kickPlayerButton.gameObject.SetActive(visible);
    }

    private void UpdateReadyButtonText()
    {
        if (isReady)
        {
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Unready";
        }
        else
        {
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
        }
    }

    public void UpdatePlayer(Player player)
    {
        this.player = player;
        playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;

        // Activate the ready button ONLY for the local player
        bool isLocalPlayer = player.Id == AuthenticationService.Instance.PlayerId;
        readyButton.gameObject.SetActive(isLocalPlayer);

        // Update the ready status indicator for everyone
        bool playerIsReady = player.Data[LobbyManager.KEY_PLAYER_READY].Value == "1";
        readyGameObject.SetActive(playerIsReady);

        if (isLocalPlayer)
        {
            isReady = playerIsReady;
            UpdateReadyButtonText();
        }
    }

    private void KickPlayer()
    {
        if (player != null)
        {
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }
}

