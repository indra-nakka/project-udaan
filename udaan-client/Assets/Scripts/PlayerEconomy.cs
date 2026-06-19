using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerEconomy : NetworkBehaviour
{
    public NetworkVariable<int> scrapCount = new NetworkVariable<int>(0);
    [Header("Debug Testing Box")]
    public DroneUpgradeData testUpgradeAsset;

    // 1. Place the ContextMenu attribute onto a function with no parameters
    [ContextMenu("Try Purchase Upgrade")]
    private void DebugPurchaseTestAsset()
    {
        // 2. Defensive check: Did you actually drag the asset into the slot?
        if (testUpgradeAsset == null)
        {
            Debug.LogError("Debug Execution Failed: The 'Test Upgrade Asset' slot is empty! Drag your asset there first.");
            return;
        }

        // 3. Safely pass the asset card into the main server transactional engine loop
        TryPurchaseUpgrade(testUpgradeAsset);
    }

    public event Action<float> OnSpeedModifierUpgraded;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        ScrapItem scrapItem = other.GetComponent<ScrapItem>();
        if (scrapItem != null)
        {
            scrapCount.Value += 10;
            other.GetComponent<NetworkObject>().Despawn();
            Debug.Log($"Scrap Picked Up! Current Balance: {scrapCount.Value}");
        }
    }

    // [ContextMenu("Try Purchase Upgrade")]
    public void TryPurchaseUpgrade(DroneUpgradeData upgrade)
    {
        if (!IsServer) return;

        if (scrapCount.Value >= upgrade.scrapCost)
        {
            scrapCount.Value -= upgrade.scrapCost;
            OnSpeedModifierUpgraded?.Invoke(upgrade.speedModifier);
            Debug.Log($"Purchased Upgrade: {upgrade.upgradeName}. Remaining Balance: {scrapCount.Value}");
        }
    }
}
