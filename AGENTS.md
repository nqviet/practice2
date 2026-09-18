# A Unity game

## Context
You are working on a Unity 6 editor through its CLI.

## Guidelines
- Do not read project asset file contents, unless .cs files or small-size text files. Use Unity CLI to work with them if applicable.
- Use the CLI, GUI skills /unity-cli, /ui-ugui if needed

## Coding convention
**Private members**
Prefix m_
```
private bool m_IsTicking;
```

**Public members**
First letter capitalized
```
public TerrainManager Terrain { get; set; }
```

**Static members**
Prefix s_
```
private static GameManager s_Instance;
```

**Event Emitter and callback members**
Prefix On
```
// Event emitter
public UnityAction OnDeckLoaded;

// Callback
public void OnThrow()
```