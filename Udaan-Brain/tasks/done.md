# ✅ Completed Tasks

### Task ECON-01: The Scrap Prefab
- **Goal:** Create `ScrapItem.cs`.
- **Details:**
  - Requires a Rigidbody (falling/scattering physics).
  - Requires a Collider set to `Is Trigger`.
  - Should represent a physical, shiny yellow cube/gear (placeholder).

### Task ECON-02: The Piñata Drop
- **Goal:** Update `TargetHealth.cs`.
- **Details:**
  - Add variables for `GameObject scrapPrefab` and `int dropCount = 3`.
  - Inside the `Pop()` function, right before `Destroy(gameObject)`, instantiate 3 scrap pieces.
  - Apply a random upward and outward explosive force (`rb.AddForce`) so they scatter.
  "Crucial: Because this is a multiplayer game, the instantiated scrap must have a NetworkObject component, and you must call newScrap.GetComponent<NetworkObject>().Spawn(); after instantiating it so it syncs across the network."

### Task ECON-03: The Drone Wallet
- **Goal:** Create a collection system.
- **Details:**
  - Create a new script `PlayerEconomy.cs` attached to the Drone. Inherit from `NetworkBehaviour`.
  - Add a synchronized integer: `public NetworkVariable<int> scrapCount = new NetworkVariable<int>(0);`
  - Use `OnTriggerEnter(Collider other)` to detect when the drone flies through an object tagged or identified as a `ScrapItem`.
  - **Crucial Multiplayer Logic:** Inside the trigger, check `if (!IsServer) return;`. ONLY the Server should add +10 to the `scrapCount.Value`. 
  - To delete the scrap, the Server must call `other.GetComponent<NetworkObject>().Despawn();` (do NOT use `Destroy()`).

### Task ECON-04: Upgrade Tree Framework
- **Goal:** Implement DroneUpgradeData ScriptableObject and TryPurchaseUpgrade logic.

### Task ECON-05: Minimalist HUD Overlay Controller
- **Goal:** Create PlayerHUDController to display player's scrap count dynamically with defensive checks.
