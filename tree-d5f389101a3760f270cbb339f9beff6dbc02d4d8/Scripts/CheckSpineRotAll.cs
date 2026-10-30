using Godot;

public partial class CheckSpineRotAll : SceneTree
{
    public override void _Initialize()
    {
        var lib = ResourceLoader.Load<AnimationLibrary>("res://Assets/Animations/MixamoLibrary.res");
        
        foreach (var name in new string[]{"idle", "run"})
        {
            var anim = lib.GetAnimation(name);
            if (anim == null) continue;
            
            GD.Print($"\nAnimation: {name}");
            for (int i = 0; i < anim.GetTrackCount(); i++)
            {
                var path = anim.TrackGetPath(i).ToString();
                if (path.Contains("Spine") || path.Contains("Hips") || path.Contains("Shoulder"))
                {
                    GD.Print($"  {path.Substring(path.IndexOf("mixamorig_"))}: {anim.TrackGetKeyValue(i, 0)}");
                }
            }
        }
        Quit();
    }
}
