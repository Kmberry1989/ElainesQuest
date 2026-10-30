using Godot;

public partial class RebuildLibrary : SceneTree
{
    public override void _Initialize()
    {
        var lib = new AnimationLibrary();
        var dir = DirAccess.Open("res://Assets/Animations/Mixamo");
        
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (fileName.EndsWith(".glb"))
            {
                var scene = ResourceLoader.Load<PackedScene>($"res://Assets/Animations/Mixamo/{fileName}");
                var node = scene.Instantiate();
                var animPlayer = node.GetNode<AnimationPlayer>("AnimationPlayer");
                if (animPlayer != null && animPlayer.HasAnimation("mixamo_com"))
                {
                    var anim = animPlayer.GetAnimation("mixamo_com");
                    var name = fileName.Replace(".glb", "");
                    
                    var newAnim = (Animation)anim.Duplicate();
                    
                    for (int i = 0; i < newAnim.GetTrackCount(); i++)
                    {
                        var path = newAnim.TrackGetPath(i).ToString();
                        int idx = path.IndexOf("RootNode/Skeleton3D");
                        if (idx != -1)
                        {
                            newAnim.TrackSetPath(i, path.Substring(idx));
                        }
                    }
                    
                    lib.AddAnimation(name, newAnim);
                    GD.Print($"Added {name}");
                }
                node.QueueFree();
            }
            fileName = dir.GetNext();
        }
        
        ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
        GD.Print("Rebuilt MixamoLibrary.res!");
        Quit();
    }
}
