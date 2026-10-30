using Godot;

public partial class HUD : CanvasLayer
{
    [Export] public Label GlimmersLabel;
    [Export] public Label FamilyLabel;

    public override void _Ready()
    {
        GlimmersLabel ??= GetNodeOrNull<Label>("Control/GlimmersLabel") ?? FindChild("GlimmersLabel", true, false) as Label;
        FamilyLabel ??= GetNodeOrNull<Label>("Control/FamilyLabel") ?? FindChild("FamilyLabel", true, false) as Label;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GlimmersChanged += OnGlimmersChanged;
            GameManager.Instance.FamilyRescuedChanged += OnFamilyRescuedChanged;
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GlimmersChanged -= OnGlimmersChanged;
            GameManager.Instance.FamilyRescuedChanged -= OnFamilyRescuedChanged;
        }
    }

    private void OnGlimmersChanged(int _glimmers)
    {
        Refresh();
    }

    private void OnFamilyRescuedChanged(int _familyCount)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GlimmersLabel != null)
        {
            GlimmersLabel.Text = $"Glimmers: {GameManager.Instance.Glimmers}";
        }

        if (FamilyLabel != null)
        {
            FamilyLabel.Text = $"Family Rescued: {GameManager.Instance.FamilyRescuedCount} / {GameManager.TotalFamilyMembers}";
        }
    }
}
