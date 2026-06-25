using UnityEngine;
using Unity.Netcode;

public class DroneClassSpawner : NetworkBehaviour
{
    [Header("Available Classes")]
    public DroneClassData[] availableClasses;

    [ServerRpc(RequireOwnership = false)]
    public void RequestClassSelectionServerRpc(int classIndex, ulong clientId)
    {
        if (classIndex < 0 || classIndex >= availableClasses.Length)
        {
            Debug.LogWarning($"DroneClassSpawner: Class selection failed for client {clientId}. Index {classIndex} out of bounds.");
            return;
        }

        DroneClassData selectedClass = availableClasses[classIndex];

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            NetworkObject playerObj = client.PlayerObject;
            if (playerObj != null)
            {
                // Inject the class data into the flight controller and initialize
                DroneFlightController flightController = playerObj.GetComponent<DroneFlightController>();
                if (flightController != null)
                {
                    flightController.InitializeClassData(selectedClass);
                }

                // Inject the class data into the health component and initialize
                TargetHealth targetHealth = playerObj.GetComponent<TargetHealth>();
                if (targetHealth != null)
                {
                    targetHealth.InitializeClassData(selectedClass);
                }
                
                Debug.Log($"DroneClassSpawner: Successfully applied class '{selectedClass.className}' to Client {clientId}");
            }
        }
    }
}
