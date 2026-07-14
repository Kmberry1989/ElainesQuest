using Godot;

public partial class PrintLibraryTracks : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        var anim = lib.GetAnimation("run");
        for (int i = 0; i < anim.GetTrackCount() && i < 10; i++)
        {
            GD.Print($"Track {i} path: {anim.TrackGetPath(i)}");
        }
        Quit();
    }
}
