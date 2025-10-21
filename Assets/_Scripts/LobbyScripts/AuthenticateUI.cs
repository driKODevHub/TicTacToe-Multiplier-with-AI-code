using UnityEngine;
using UnityEngine.UI;

public class AuthenticateUI : MonoBehaviour
{


    [SerializeField] private Button authenticateButton;


    private void Awake()
    {
        authenticateButton.onClick.AddListener(() => {
            LobbyManager.Instance.Authenticate(EditPlayerName.Instance.GetPlayerName());
            Hide();
        });
    }

    private void Start()
    {
        // DEBUG: Automatically authenticate to speed up testing
        authenticateButton.onClick.Invoke();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
