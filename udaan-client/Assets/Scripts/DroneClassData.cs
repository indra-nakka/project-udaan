using UnityEngine;

[CreateAssetMenu(fileName = "NewDroneClass", menuName = "Drone Class Data")]
public class DroneClassData : ScriptableObject
{
    public string className;
    public float maxHealth = 100f;
    public float baseThrustForce = 15f;
    public float baseDragValue = 3f;
}
