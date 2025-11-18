using UnityEngine;
using System.Collections.Generic;

public class MagicNotes_Spawner : MonoBehaviour
{
    [System.Serializable]
    public class NoteConfig
    {
        public int midiNote;
        public string prefabOnPress;
        public string prefabOnRelease;

        public bool spawnOnPress = true;
        public bool spawnOnRelease = false;
        public bool destroyOnRelease = false;

        public bool continuous = false;

        // Continuous effect (head/middle/tail)
        public string headPrefab;
        public string middlePrefab;
        public string tailPrefab;

        [HideInInspector] public GameObject activeHead;
        [HideInInspector] public List<GameObject> activeMiddle = new List<GameObject>();
    }

    public List<NoteConfig> configs;

    private Dictionary<int, NoteConfig> configByNote;

    private void Awake()
    {
        configByNote = new Dictionary<int, NoteConfig>();
        foreach (var c in configs)
            configByNote.Add(c.midiNote, c);
    }

    // ---------------------------------------------------------

    public void OnNotePressed(int midiNote, int velocity)
    {
        if (!configByNote.TryGetValue(midiNote, out NoteConfig cfg))
            return;

        Vector3 pos = MagicNotes_Keyboard.GetNotePosition(midiNote);

        if (cfg.spawnOnPress)
        {
            MagicNotes_Pool.Instance.Spawn(cfg.prefabOnPress, pos, Quaternion.identity);
        }

        if (cfg.continuous)
        {
            // head
            cfg.activeHead = MagicNotes_Pool.Instance.Spawn(cfg.headPrefab, pos, Quaternion.identity);
        }
    }

    public void OnNoteReleased(int midiNote)
    {
        if (!configByNote.TryGetValue(midiNote, out NoteConfig cfg))
            return;

        Vector3 pos = MagicNotes_Keyboard.GetNotePosition(midiNote);

        if (cfg.spawnOnRelease)
        {
            MagicNotes_Pool.Instance.Spawn(cfg.prefabOnRelease, pos, Quaternion.identity);
        }

        if (cfg.destroyOnRelease && cfg.activeHead != null)
        {
            MagicNotes_Pool.Instance.Despawn(cfg.headPrefab, cfg.activeHead);
            cfg.activeHead = null;
        }

        if (cfg.continuous)
        {
            // tail
            MagicNotes_Pool.Instance.Spawn(cfg.tailPrefab, pos, Quaternion.identity);
        }
    }

    private void Update()
    {
        // Continuous trails
        foreach (var cfg in configByNote.Values)
        {
            if (!cfg.continuous) continue;

            if (cfg.activeHead != null)
            {
                Vector3 pos = MagicNotes_Keyboard.GetNotePosition(cfg.midiNote);
                cfg.activeHead.transform.position = pos;

                // adiciona mid segments
                GameObject mid = MagicNotes_Pool.Instance.Spawn(cfg.middlePrefab, pos, Quaternion.identity);
                cfg.activeMiddle.Add(mid);
            }
        }
    }
}
