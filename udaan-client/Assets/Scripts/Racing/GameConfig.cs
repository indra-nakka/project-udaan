using UnityEngine;

/// <summary>
/// Cross-scene game settings chosen at the start menu (name, difficulty, volume), persisted via
/// PlayerPrefs. The gameplay bootstrap reads Difficulty; the menu writes it. Kept tiny and static
/// so nothing needs wiring in the Inspector.
/// </summary>
public static class GameConfig
{
    public static MissionDirector.Difficulty Difficulty = MissionDirector.Difficulty.Medium;
    public static string PlayerName = "Ira";
    public static float Volume = 1f;

    /// <summary>Set by the Restart button so the reloaded scene skips the menu and plays immediately.</summary>
    public static bool AutoPlay = false;

    private const string KDiff = "udaan_diff", KName = "udaan_name", KVol = "udaan_vol";

    public static void Load()
    {
        Difficulty = (MissionDirector.Difficulty)PlayerPrefs.GetInt(KDiff, (int)MissionDirector.Difficulty.Medium);
        PlayerName = PlayerPrefs.GetString(KName, "Ira");
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(KVol, 1f));
        AudioListener.volume = Volume;
    }

    public static void Save()
    {
        if (string.IsNullOrWhiteSpace(PlayerName)) PlayerName = "Ira";
        PlayerPrefs.SetInt(KDiff, (int)Difficulty);
        PlayerPrefs.SetString(KName, PlayerName);
        PlayerPrefs.SetFloat(KVol, Volume);
        PlayerPrefs.Save();
        AudioListener.volume = Volume;
    }
}
