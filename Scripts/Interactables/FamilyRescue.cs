using Godot;

public partial class FamilyRescue : Area3D
{
    [Export] public string FamilyId = "Matthew";
    [Export] public string DisplayName = "Matthew";
    [Export(PropertyHint.MultilineText)] public string RescueDialogue = "Elaine! I knew you'd find me.";
    [Export] public int BonusGlimmers = 10;
    [Export] public bool ConsumeOnRescue = true;

    public override void _Ready()
    {
        if (GameManager.Instance?.HasRescuedFamilyMember(FamilyId) == true && ConsumeOnRescue)
        {
            QueueFree();
            return;
        }

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not ElaineController && !body.IsInGroup("player"))
        {
            return;
        }

        if (GameManager.Instance?.HasRescuedFamilyMember(FamilyId) == true)
        {
            if (ConsumeOnRescue)
            {
                QueueFree();
            }

            return;
        }

        GameManager.Instance?.RescueFamilyMember(FamilyId);

        if (BonusGlimmers != 0)
        {
            GameManager.Instance?.AddGlimmers(BonusGlimmers);
        }

        GameManager.Instance?.RequestDialogue(DisplayName, RescueDialogue);

        if (ConsumeOnRescue)
        {
            QueueFree();
        }
    }
}
