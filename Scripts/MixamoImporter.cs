using Godot;

public partial class MixamoImporter : SceneTree
{
    public override void _Initialize()
    {
        var lib = new AnimationLibrary();
        string dirPath = "res://Assets/Animations/Mixamo";
        using var dir = DirAccess.Open(dirPath);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (fileName.EndsWith(".glb") && !dir.CurrentIsDir())
                {
                    string path = dirPath + "/" + fileName;
                    var scene = ResourceLoader.Load<PackedScene>(path);
                    if (scene != null)
                    {
                        var node = scene.Instantiate();
                        var player = node.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
                        if (player != null)
                        {
                            var anim = player.GetAnimation("mixamo.com");
                            if (anim == null)
                            {
                                var animList = player.GetAnimationList();
                                if (animList.Length > 0)
                                {
                                    anim = player.GetAnimation(animList[0]);
                                }
                            }

                            if (anim != null)
                            {
                                string animName = fileName.Replace(".glb", "");
                                lib.AddAnimation(animName, anim);
                                GD.Print("Added animation: " + animName);
                            }
                        }
                        node.QueueFree();
                    }
                }
                fileName = dir.GetNext();
            }
            ResourceSaver.Save(lib, "res://Assets/Animations/MixamoLibrary.res");
            GD.Print("Saved MixamoLibrary.res successfully.");
        }
        Quit();
    }
}
