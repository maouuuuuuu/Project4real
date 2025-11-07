// Assets/Scripts/Core/Bootstrapper.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    public static Bootstrapper Instance { get; private set; }

    [Header("Catalog")]
    public CharacterCatalog characterCatalog;   // assign in your first scene

    // Expose the full character list (shared to Story & others)
    public IReadOnlyList<CharacterDefinition> AllCharacters =>
        characterCatalog ? characterCatalog.All : System.Array.Empty<CharacterDefinition>();

    [Header("Runtime Teams (Story edits these)")]
    public List<CharacterDefinition> teamA = new();
    public List<CharacterDefinition> teamB = new();

    [Header("Optional auto-load")]
    public bool autoLoadOnStart = false;
    public string firstSceneName = "Story";

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        if (autoLoadOnStart && !string.IsNullOrWhiteSpace(firstSceneName))
            await LoadScene(firstSceneName);
    }

    // --- Tiny API for Story/Battle ---
    public void ClearTeams()
    {
        teamA ??= new List<CharacterDefinition>();
        teamB ??= new List<CharacterDefinition>();
        teamA.Clear();
        teamB.Clear();
    }

    public bool AddToTeamA(CharacterDefinition c)
    {
        if (!c) return false;
        if (teamB != null) teamB.Remove(c);
        if (teamA == null) teamA = new List<CharacterDefinition>();
        if (!teamA.Contains(c)) teamA.Add(c);
        return true;
    }

    public bool AddToTeamB(CharacterDefinition c)
    {
        if (!c) return false;
        if (teamA != null) teamA.Remove(c);
        if (teamB == null) teamB = new List<CharacterDefinition>();
        if (!teamB.Contains(c)) teamB.Add(c);
        return true;
    }

    // --- Minimal scene loader ---
    public async Task LoadScene(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
    }
}
