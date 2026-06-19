using UnityEngine;
using Unity.Netcode;
using TMPro;

[RequireComponent(typeof(PlayerEconomy))]
public class PlayerHUDController : NetworkBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scrapTextDisplay;
    
    private PlayerEconomy playerEconomy;

    private void Awake()
    {
        playerEconomy = GetComponent<PlayerEconomy>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Defensive check against null references during async multiplayer client spawns
        if (playerEconomy != null && scrapTextDisplay != null)
        {
            int currentValue = playerEconomy.scrapCount.Value;
            scrapTextDisplay.text = $"Scrap: {currentValue}";
        }
    }
}
