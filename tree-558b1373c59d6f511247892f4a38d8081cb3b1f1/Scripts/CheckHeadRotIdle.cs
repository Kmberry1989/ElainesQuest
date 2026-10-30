using Godot;

public partial class CheckHeadRotIdle : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        var anim = lib.GetAnimation("idle");
        
        for (int i = 0; i < anim.GetTrackCount(); i++)
        {
            var path = anim.TrackGetPath(i).ToString();
            if (path.Contains("Head") || path.Contains("Neck"))
            {
                GD.Print($"Track: {path}");
                for (int j = 0; j < Mathf.Min(1, anim.TrackGetKeyCount(i)); j++)
                {
                    GD.Print($"  Time {anim.TrackGetKeyTime(i, j)}: {anim.TrackGetKeyValue(i, j)}");
                }
            }
        }
        Quit();
    }
}
