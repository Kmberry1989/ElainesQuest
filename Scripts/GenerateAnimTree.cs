using Godot;

[Tool]
public partial class GenerateAnimTree : SceneTree
{
    public override void _Initialize()
    {
        var stateMachine = new AnimationNodeStateMachine();
        
        var idleAnim = new AnimationNodeAnimation { Animation = "Mixamo/idle" };
        var runAnim = new AnimationNodeAnimation { Animation = "Mixamo/run" };
        var jumpAnim = new AnimationNodeAnimation { Animation = "Mixamo/jump" };
        var hoverAnim = new AnimationNodeAnimation { Animation = "Mixamo/flying" };
        
        stateMachine.AddNode("Idle", idleAnim);
        stateMachine.AddNode("Run", runAnim);
        stateMachine.AddNode("Jump", jumpAnim);
        stateMachine.AddNode("Hover", hoverAnim);
        
        var startToIdle = new AnimationNodeStateMachineTransition();
        startToIdle.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
        stateMachine.AddTransition("Start", "Idle", startToIdle);

        foreach (string from in new[] { "Idle", "Run", "Jump", "Hover" })
        {
            foreach (string to in new[] { "Idle", "Run", "Jump", "Hover" })
            {
                if (from != to)
                {
                    var t = new AnimationNodeStateMachineTransition();
                    stateMachine.AddTransition(from, to, t);
                }
            }
        }

        string path = "res://Scenes/Characters/Player/ElaineAnimStateMachine.tres";
        Error err = ResourceSaver.Save(stateMachine, path);
        if (err == Error.Ok)
        {
            GD.Print("Saved StateMachine successfully to " + path);
        }
        else
        {
            GD.PrintErr("Failed to save StateMachine: " + err);
        }
        
        Quit();
    }
}
