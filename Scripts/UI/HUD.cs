using Godot;

public partial class HUD : CanvasLayer
{
    [Export] public Label GlimmersLabel;
    [Export] public Label FamilyLabel;
    [Export] public Label HealthLabel;
    [Export] public Label SpellLabel;

    private ElaineController _player;

    public override void _Ready()
    {
        GlimmersLabel ??= GetNodeOrNull<Label>("Control/GlimmersLabel") ?? FindChild("GlimmersLabel", true, false) as Label;
        FamilyLabel ??= GetNodeOrNull<Label>("Control/FamilyLabel") ?? FindChild("FamilyLabel", true, false) as Label;
        HealthLabel ??= GetNodeOrNull<Label>("Control/HealthLabel") ?? FindChild("HealthLabel", true, false) as Label;
        SpellLabel ??= GetNodeOrNull<Label>("Control/SpellLabel") ?? FindChild("SpellLabel", true, false) as Label;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GlimmersChanged += OnGlimmersChanged;
            GameManager.Instance.FamilyRescuedChanged += OnFamilyRescuedChanged;
        }

        ResolvePlayer();
        Refresh();
    }

    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
        {
            ResolvePlayer();
        }

        RefreshPlayerStatus();
    }

    public override void _ExitTree()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GlimmersChanged -= OnGlimmersChanged;
            GameManager.Instance.FamilyRescuedChanged -= OnFamilyRescuedChanged;
        }

        DisconnectPlayer();
    }

    private void ResolvePlayer()
    {
        ElaineController nextPlayer = GetTree().GetFirstNodeInGroup("player") as ElaineController;
        if (nextPlayer == _player)
        {
            return;
        }

        DisconnectPlayer();
        _player = nextPlayer;

        if (_player != null)
        {
            _player.HealthChanged += OnPlayerHealthChanged;
        }

        RefreshPlayerStatus();
    }

    private void DisconnectPlayer()
    {
        if (_player != null && IsInstanceValid(_player))
        {
            _player.HealthChanged -= OnPlayerHealthChanged;
        }

        _player = null;
    }

    private void OnGlimmersChanged(int _glimmers)
    {
        Refresh();
    }

    private void OnFamilyRescuedChanged(int _familyCount)
    {
        Refresh();
    }

    private void OnPlayerHealthChanged(float _currentHealth, float _maxHealth)
    {
        RefreshPlayerStatus();
    }

    private void Refresh()
    {
        if (GameManager.Instance != null)
        {
            if (GlimmersLabel != null)
            {
                GlimmersLabel.Text = $"Glimmers: {GameManager.Instance.Glimmers}";
            }

            if (FamilyLabel != null)
            {
                FamilyLabel.Text = $"Family Rescued: {GameManager.Instance.FamilyRescuedCount} / {GameManager.TotalFamilyMembers}";
            }
        }

        RefreshPlayerStatus();
    }

    private void RefreshPlayerStatus()
    {
        if (_player == null)
        {
            if (HealthLabel != null)
            {
                HealthLabel.Text = "Health: --";
            }

            if (SpellLabel != null)
            {
                SpellLabel.Text = "Spell: --";
            }

            return;
        }

        if (HealthLabel != null)
        {
            HealthLabel.Text = $"Health: {Mathf.CeilToInt(_player.GetCurrentHealth())} / {Mathf.CeilToInt(_player.GetMaxHealth())}";
        }

        if (SpellLabel != null)
        {
            SpellLabel.Text = _player.GetSpellHudText();
        }
    }
}
