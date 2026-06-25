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

### Task DRONE-01: Base 'Striker' Class Blueprint
- **Goal:** Create DroneClassData.cs ScriptableObject template with health and physics parameters.

### Task DRONE-02: Runtime Class Integration
- **Goal:** Integrate DroneClassData into DroneFlightController and TargetHealth to dynamically overwrite physics and health values at runtime.

### Task DOC-01: Create Architecture Data Glossary
- **Goal:** Create a structured documentation layout tracking the active variables in our core gameplay loops.

### Task DOC-02: Integrate Glossary Maintenance Rules
- **Goal:** Update UDAAN_ROUTER.md to explicitly map the glossary and enforce strict read/write protocols for variables.

### Task DRONE-03: Multi-Class Selection Network Spawner
- **Goal:** Create DroneClassSpawner to coordinate dynamic class selections and parameter injection over the network via ServerRpc.

### Task DRONE-04: Class Selection UI Controller
- **Goal:** Create a UI controller to link front-end user selection buttons to the backend network spawner RPCs.

### Task DRONE-05: Sandbox Optimization and Selection Validation
- **Goal:** Update the target dummy to infinitely respawn inside a sandbox range and validate UI-to-network class application loops via formatted logging.

### Task DRONE-06: UI Panel Hiding, Input Suppression, and Netcode GUI Cleanup
- **Goal:** Disable legacy GUI elements and suppress weapon inputs to isolate flight calibration testing.

### Task DRONE-07: Restore Xbox Input Profiles and Multiplayer UI Routing
- **Goal:** Expose explicit Host/Client network routing logic, separate it from class selection UI clicks, and re-enable gamepad input layers.

### Task DRONE-08: Complete Network Button Assignment
- **Goal:** Explicitly bind Host/Client buttons inside ClassSelectionUI to trigger connections and send the cached class.

### Task DRONE-09: Ace Combat FPS Hybrid Flight Engine Overhaul
- **Goal:** Rewrite DroneFlightController to map precise 5-axis analog inputs, gracefully handle keyboard fallbacks, and suppress hover dampeners upon manual override.

### Task DRONE-10: Implement Definitive Controller Blueprint
- **Goal:** Strip legacy keyboard fallback checks from DroneFlightController to rigidly enforce the true 5-axis gamepad profiles.
