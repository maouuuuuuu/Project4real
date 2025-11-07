// Assets/Scripts/Story/StorySceneController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StorySceneController : MonoBehaviour
{
    [Header("UI wiring")]
    public Transform listRoot;                  // a VerticalLayoutGroup container
    public GameObject characterEntryPrefab;     // prefab with CharacterEntry + TMP name + Button
    public Button startButton;                  // Start / Continue button

    private readonly List<CharacterEntry> _entries = new();

    void Start()
    {
        var boot = Bootstrapper.Instance;
        if (!boot)
        {
            Debug.LogError("[Story] No Bootstrapper found.");
            return;
        }

        BuildList(boot.AllCharacters);

        if (startButton) startButton.onClick.AddListener(OnStartPressed);
    }

    void BuildList(IReadOnlyList<CharacterDefinition> characters)
    {
        // clear existing children
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);
        _entries.Clear();

        if (characters == null)
        {
            Debug.LogWarning("[Story] No characters available.");
            return;
        }
        if (!characterEntryPrefab)
        {
            Debug.LogError("[Story] characterEntryPrefab not assigned.");
            return;
        }

        foreach (var c in characters)
        {
            if (!c) continue;
            var go = Instantiate(characterEntryPrefab, listRoot);
            var entry = go.GetComponent<CharacterEntry>();
            if (!entry)
            {
                Debug.LogError("[Story] Prefab missing CharacterEntry component.");
                Destroy(go);
                continue;
            }
            entry.Init(c);
            _entries.Add(entry);
        }
    }

    // in StorySceneController.cs
    async void OnStartPressed()
    {
        var boot = Bootstrapper.Instance;
        if (!boot) return;

        boot.teamA.Clear();
        boot.teamB.Clear();

        foreach (var e in _entries)
        {
            if (!e || !e.Character) continue;
            switch (e.Team)
            {
                case Team.TeamA: boot.teamA.Add(e.Character); break;
                case Team.TeamB: boot.teamB.Add(e.Character); break;
            }
        }

        await Bootstrapper.Instance.LoadScene("Battle");
    }
}
