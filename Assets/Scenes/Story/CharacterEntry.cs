// Assets/Scripts/Story/CharacterEntry.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum Team { None, TeamA, TeamB }

public class CharacterEntry : MonoBehaviour
{
    [Header("UI refs")]
    public TMP_Text nameLabel;     // assign in prefab
    public Button teamButton;      // assign in prefab

    public CharacterDefinition Character { get; private set; }
    public Team Team { get; private set; } = Team.None;

    public void Init(CharacterDefinition character)
    {
        Character = character;
        if (nameLabel) nameLabel.text = character ? character.CharacterName : "(null)";
        SetTeam(Team.None);

        if (teamButton) teamButton.onClick.AddListener(CycleTeam);
    }

    void CycleTeam()
    {
        SetTeam(Team switch
        {
            Team.None => Team.TeamA,
            Team.TeamA => Team.TeamB,
            _ => Team.None
        });
    }

    void SetTeam(Team t)
    {
        Team = t;
        if (!teamButton) return;
        var label = teamButton.GetComponentInChildren<TMP_Text>();
        if (label) label.text = t switch
        {
            Team.TeamA => "Team A",
            Team.TeamB => "Team B",
            _ => "None"
        };
    }
}
