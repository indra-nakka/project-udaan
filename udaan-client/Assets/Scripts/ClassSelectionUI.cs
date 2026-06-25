using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ClassSelectionUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject selectionPanel;

    [Header("Class Buttons")]
    public Button strikerButton;
    public Button bulwarkButton;

    [Header("Network Buttons")]
    public Button hostButton;
    public Button clientButton;

    private DroneClassSpawner spawner;
    private int cachedClassIndex = -1; // -1 means no class selected

    void Start()
    {
        // Safety guard: ensure fields are assigned via Inspector
        if (selectionPanel == null || strikerButton == null || bulwarkButton == null || hostButton == null || clientButton == null)
        {
            Debug.LogError("ClassSelectionUI Error: One or more UI references are missing! Please assign them in the inspector.");
            return;
        }

        // Locate the spawner dynamically in the scene
        spawner = FindAnyObjectByType<DroneClassSpawner>();

        if (spawner == null)
        {
            Debug.LogError("ClassSelectionUI Error: Could not find DroneClassSpawner in the scene!");
            return;
        }

        // Bind Striker Button (Index 0)
        strikerButton.onClick.AddListener(() =>
        {
            cachedClassIndex = 0;
            TrySendClassSelection();
        });

        // Bind Bulwark Button (Index 1)
        bulwarkButton.onClick.AddListener(() =>
        {
            cachedClassIndex = 1;
            TrySendClassSelection();
        });

        // Bind Host/Client Network Connections
        hostButton.onClick.AddListener(() => OnHostPressed());
        clientButton.onClick.AddListener(() => OnClientPressed());
    }

    public void OnHostPressed()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
            StartCoroutine(WaitForConnectionAndSendClass());
        }
    }

    public void OnClientPressed()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
            StartCoroutine(WaitForConnectionAndSendClass());
        }
    }

    private void TrySendClassSelection()
    {
        // If we are already fully connected, bypass the wait and send immediately
        if (NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            spawner.RequestClassSelectionServerRpc(cachedClassIndex, NetworkManager.Singleton.LocalClientId);
            selectionPanel.SetActive(false);
        }
        else
        {
            Debug.Log("ClassSelectionUI: Class selected locally. Awaiting explicit Host/Client connection...");
        }
    }

    private System.Collections.IEnumerator WaitForConnectionAndSendClass()
    {
        // Wait until the client connects and the PlayerObject spawns
        yield return new WaitUntil(() => NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null);

        // Yield an extra frame to be absolutely certain initialization hooks have fired
        yield return null;

        // Automatically dispatch the cached class if one was selected
        if (cachedClassIndex != -1)
        {
            spawner.RequestClassSelectionServerRpc(cachedClassIndex, NetworkManager.Singleton.LocalClientId);
            selectionPanel.SetActive(false);
        }
    }
}
