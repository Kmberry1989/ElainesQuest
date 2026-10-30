using Godot;

public partial class CheckHeadRot : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        var anim = lib.GetAnimation("Mixamo/run");
        if (anim == null) {
            anim = lib.GetAnimation("run");
        }
        
        for (int i = 0; i < anim.GetTrackCount(); i++)
        {
            var path = anim.TrackGetPath(i).ToString();
            if (path.Contains("Head") || path.Contains("Neck"))
            {
                GD.Print($"Track: {path}");
                for (int j = 0; j < Mathf.Min(3, anim.TrackGetKeyCount(i)); j++)
                {
                    GD.Print($"  Time {anim.TrackGetKeyTime(i, j)}: {anim.TrackGetKeyValue(i, j)}");
                }
            }
        }
        Quit();
    }
}
