using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Per-run scorecard. MissionDirector creates one at the start of each run and prints it to the
/// Console at Victory/Defeat. Cross-object counters (allies) use the static Active instance.
/// </summary>
public class MissionStats
{
    public static MissionStats Active;

    public float startTime;
    public int kills, alliesSpawned, livesUsed, livesRemaining, outpostsCaptured;
    public int playerKills, allyKills, alliesLost;
    public float damageDealt, damageTaken, allyDamage;
    public readonly List<float> waveClearTimes = new List<float>(); // seconds-from-start per wave
    public string result = "—";
    public string defeatReason = "";   // filled only on a loss
    public string endPhase = "";       // mission phase the run ended in

    public void Print()
    {
        float dur = Time.time - startTime;
        var sb = new StringBuilder();
        sb.AppendLine("================  SKY SENTINEL — RUN SUMMARY  ================");
        sb.AppendLine($"  Result:              {result}");
        if (result == "DEFEAT")
            sb.AppendLine($"  Why:                 {(string.IsNullOrEmpty(defeatReason) ? "Mission failed" : defeatReason)}{(string.IsNullOrEmpty(endPhase) ? "" : $"  (during {endPhase})")}");
        sb.AppendLine($"  Total time:          {Fmt(dur)}");
        for (int i = 0; i < waveClearTimes.Count; i++)
            sb.AppendLine($"  Wave {i + 1} cleared at:   {Fmt(waveClearTimes[i])}");
        sb.AppendLine($"  Enemy kills:         {kills}   (you {playerKills} · allies {allyKills})");
        sb.AppendLine($"  Outposts captured:   {outpostsCaptured}");
        sb.AppendLine($"  Allies spawned:      {alliesSpawned}   (lost {alliesLost})");
        sb.AppendLine($"  Damage dealt (you):  {damageDealt:0}");
        sb.AppendLine($"  Damage dealt (allies):{allyDamage:0}");
        sb.AppendLine($"  Damage taken:        {damageTaken:0}");
        sb.AppendLine($"  Lives used:          {livesUsed}   (remaining {livesRemaining})");
        sb.Append("=============================================================");
        Debug.Log(sb.ToString());
        WriteCsv();
    }

    // Append one row per run to a CSV in persistentDataPath — real telemetry for balance tuning.
    public void WriteCsv()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "udaan_runs.csv");
            bool exists = File.Exists(path);
            using (var w = new StreamWriter(path, true))
            {
                if (!exists)
                    w.WriteLine("timestamp,result,reason,endPhase,totalTime,kills,playerKills,allyKills,outpostsCaptured,alliesSpawned,alliesLost,damageDealt,allyDamage,damageTaken,livesUsed,livesRemaining");
                float dur = Time.time - startTime;
                w.WriteLine($"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss},{result},{Csv(defeatReason)},{endPhase},{dur:0.0}," +
                            $"{kills},{playerKills},{allyKills},{outpostsCaptured},{alliesSpawned},{alliesLost}," +
                            $"{damageDealt:0},{allyDamage:0},{damageTaken:0},{livesUsed},{livesRemaining}");
            }
            Debug.Log("[STATS] run appended to " + path);
        }
        catch (System.Exception e) { Debug.LogWarning("[STATS] CSV write failed: " + e.Message); }
    }

    private static string Csv(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace(",", ";");

    private static string Fmt(float t)
    {
        int m = (int)(t / 60f);
        float s = t - m * 60f;
        return $"{m:00}:{s:00.0}";
    }
}
