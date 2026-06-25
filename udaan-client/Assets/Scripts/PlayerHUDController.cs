using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerHUDController : NetworkBehaviour
{
    private PlayerEconomy playerEconomy;
    private TextMeshProUGUI scrapTextDisplay;

    void Awake()
    {
        playerEconomy = GetComponent<PlayerEconomy>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Safety lock: Only find and update the HUD for the local player driving this drone!
        if (!IsOwner) return;

        // Hunt down the UI element in the scene dynamically using our custom tag
        GameObject uiTarget = GameObject.FindWithTag("HUD_ScrapText");
        if (uiTarget != null)
        {
            scrapTextDisplay = uiTarget.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("HUD Controller Error: Could not find any scene object tagged 'HUD_ScrapText'!");
        }
    }

    void Update()
    {
        // Guard checking: Ensure we only write to our own UI display and that it's linked
        if (!IsOwner || scrapTextDisplay == null || playerEconomy == null) return;

        // Pull the live synchronized NetworkVariable value seamlessly
        scrapTextDisplay.text = $"Scrap: {playerEconomy.scrapCount.Value}";
    }
}
