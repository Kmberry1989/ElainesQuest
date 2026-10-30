using Godot;

public partial class CheckHeadRotAll : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        foreach (var name in new string[]{"idle", "run", "walk", "jump"})
        {
            var anim = lib.GetAnimation(name);
            if (anim == null) continue;
            
            GD.Print($"\nAnimation: {name}");
            for (int i = 0; i < anim.GetTrackCount(); i++)
            {
                var path = anim.TrackGetPath(i).ToString();
                if (path.Contains("Neck"))
                {
                    GD.Print($"  Neck: {anim.TrackGetKeyValue(i, 0)}");
                }
                if (path.Contains("Head"))
                {
                    GD.Print($"  Head: {anim.TrackGetKeyValue(i, 0)}");
                }
            }
        }
        Quit();
    }
}
