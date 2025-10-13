using Unity.Netcode;

public class Player : NetworkBehaviour
{
    // This placeholder script is required by the NetworkManager
    // to identify player objects.

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Player-specific logic can be added here,
            // like initializing a camera or UI for this player.
        }
    }
}

