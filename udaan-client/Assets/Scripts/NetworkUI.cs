using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        /* TEMPORARILY DISABLED (DRONE-06)
        // Start drawing UI in the top-left corner
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        // Only show the buttons if we aren't connected yet
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host", GUILayout.Width(120), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartHost();
            }
            
            // Add a little spacing
            GUILayout.Space(10);
            
            if (GUILayout.Button("Start Client", GUILayout.Width(120), GUILayout.Height(50)))
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        
        GUILayout.EndArea();
        */
    }
}