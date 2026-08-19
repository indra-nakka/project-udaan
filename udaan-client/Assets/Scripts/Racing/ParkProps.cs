using UnityEngine;

/// <summary>
/// Static holder for the children's-park environment models (tree/slide/swing/gym/sandbox/seesaw).
/// Mirrors <see cref="FactionVisuals"/>: DemoFlow assigns the imported FBX prefabs here in Start, and
/// <see cref="ParkMapGenerator"/> instantiates them at each scatter point. If a model is null the
/// generator falls back to its greybox primitive, so the demo always builds something.
/// </summary>
public static class ParkProps
{
    public static GameObject Tree, Slide, Swing, Gym, Sandbox, Seesaw;
    public static GameObject Playset, Merry, Dome, TyreSwing;                 // richer Indian-park stations
    public static GameObject RockWall, TyreWall, Trampoline, Bench, AnimalMerry;

    /// <summary>True if at least one park model is wired (so the generator prefers models).</summary>
    public static bool Any => Tree || Slide || Swing || Gym || Sandbox || Seesaw
                              || Playset || Merry || Dome || TyreSwing
                              || RockWall || TyreWall || Trampoline || Bench || AnimalMerry;
}
