using UnityEngine;

public static class MagicNotes_Keyboard
{
    public static Vector3 GetNotePosition(int midi)
    {
        float x = (midi - 60) * 0.22f;
        return new Vector3(x, 0, 0);
    }
}
