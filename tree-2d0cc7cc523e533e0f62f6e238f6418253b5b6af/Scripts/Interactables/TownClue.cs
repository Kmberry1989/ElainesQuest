using Godot;

public partial class TownClue : Area3D
{
    [Export] public string ClueId = "";
    [Export] public string Speaker = "Elaine";
    [Export] public string DiscoveryText = "This must be connected to the strange portal.";
    [Export] public int GlimmerReward = 1;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        if (GameManager.Instance?.HasTownClue(ClueId) ?? false)
        {
            QueueFree();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not ElaineController && !body.IsInGroup("player")) return;
        if (GameManager.Instance?.FindTownClue(ClueId) ?? false)
        {
            GameManager.Instance.AddGlimmers(GlimmerReward);
            GameManager.Instance.RequestDialogue(Speaker, DiscoveryText);
        }
        QueueFree();
    }
}
