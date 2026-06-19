using UnityEngine;

[CreateAssetMenu(fileName = "NewDroneUpgrade", menuName = "Drone Upgrade Data")]
public class DroneUpgradeData : ScriptableObject
{
    public string upgradeName;
    public int scrapCost;
    public float speedModifier = 1.0f;
    public float fireRateModifier = 1.0f;
}
