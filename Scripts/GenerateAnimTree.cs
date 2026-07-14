using Godot;

[Tool]
public partial class GenerateAnimTree : SceneTree
{
    public override void _Initialize()
    {
        var stateMachine = new AnimationNodeStateMachine();

        stateMachine.AddNode("Idle", new AnimationNodeAnimation { Animation = "Mixamo/idle" }, new Vector2(120, 40));
        stateMachine.AddNode("Walk", new AnimationNodeAnimation { Animation = "Mixamo/walk" }, new Vector2(380, 40));
        stateMachine.AddNode("Run", new AnimationNodeAnimation { Animation = "Mixamo/run" }, new Vector2(620, 220));
        stateMachine.AddNode("Jump", new AnimationNodeAnimation { Animation = "Mixamo/jump" }, new Vector2(620, -120));
        stateMachine.AddNode("Hover", new AnimationNodeAnimation { Animation = "Mixamo/flying" }, new Vector2(620, 20));
        stateMachine.AddNode("Talk", new AnimationNodeAnimation { Animation = "Mixamo/talk" }, new Vector2(380, -170));
        stateMachine.AddNode("Cast", new AnimationNodeAnimation { Animation = "Mixamo/cast" }, new Vector2(380, -300));
        stateMachine.AddNode("CastBurst", new AnimationNodeAnimation { Animation = "Mixamo/cast_burst" }, new Vector2(620, -300));
        stateMachine.AddNode("CastLance", new AnimationNodeAnimation { Animation = "Mixamo/cast_lance" }, new Vector2(860, -300));
        stateMachine.AddNode("Wave", new AnimationNodeAnimation { Animation = "Mixamo/wave" }, new Vector2(380, 190));

        var startToIdle = new AnimationNodeStateMachineTransition();
        startToIdle.AdvanceMode = AnimationNodeStateMachineTransition.AdvanceModeEnum.Auto;
        stateMachine.AddTransition("Start", "Idle", startToIdle);

        AddTransitions(stateMachine, new[]
        {
            ("Idle", "Walk"),
            ("Idle", "Run"),
            ("Idle", "Jump"),
            ("Idle", "Hover"),
            ("Idle", "Talk"),
            ("Idle", "Cast"),
            ("Idle", "CastBurst"),
            ("Idle", "CastLance"),
            ("Idle", "Wave"),
            ("Walk", "Idle"),
            ("Walk", "Run"),
            ("Walk", "Jump"),
            ("Walk", "Hover"),
            ("Walk", "Talk"),
            ("Walk", "Cast"),
            ("Walk", "CastBurst"),
            ("Walk", "CastLance"),
            ("Walk", "Wave"),
            ("Run", "Idle"),
            ("Run", "Walk"),
            ("Run", "Jump"),
            ("Run", "Hover"),
            ("Run", "Talk"),
            ("Run", "Cast"),
            ("Run", "CastBurst"),
            ("Run", "CastLance"),
            ("Run", "Wave"),
            ("Jump", "Idle"),
            ("Jump", "Walk"),
            ("Jump", "Run"),
            ("Jump", "Hover"),
            ("Hover", "Idle"),
            ("Hover", "Walk"),
            ("Hover", "Run"),
            ("Hover", "Jump"),
            ("Talk", "Idle"),
            ("Cast", "Idle"),
            ("CastBurst", "Idle"),
            ("CastLance", "Idle"),
            ("Wave", "Idle"),
        });

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

    private static void AddTransitions(AnimationNodeStateMachine stateMachine, (string From, string To)[] transitions)
    {
        foreach ((string from, string to) in transitions)
        {
            stateMachine.AddTransition(from, to, new AnimationNodeStateMachineTransition());
        }
    }
}
