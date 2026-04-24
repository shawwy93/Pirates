using System.Reflection;
using UnityEngine;

public static class TurningPreference
{
    public enum Mode { Smooth = 0, Snap = 1 }

    const string PrefKey        = "TurnMode";
    const string SnapAngleKey   = "SnapAngleDeg";
    const string VignetteKey    = "VignetteEnabled";
    const string VolumeKey      = "MasterVolume";

    public static Mode  Current          { get; private set; } = Mode.Smooth;
    public static int   SnapAngle        { get; private set; } = 45;
    public static bool  VignetteEnabled  { get; private set; } = false;
    public static float MasterVolume     { get; private set; } = 1f;

    public static void Load()
    {
        Current         = (Mode)PlayerPrefs.GetInt(PrefKey, (int)Mode.Smooth);
        SnapAngle       = PlayerPrefs.GetInt(SnapAngleKey, 45);
        VignetteEnabled = PlayerPrefs.GetInt(VignetteKey, 0) == 1;
        MasterVolume    = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        ApplyAll();
    }

    public static void Set(Mode mode)
    {
        Current = mode;
        PlayerPrefs.SetInt(PrefKey, (int)mode);
        PlayerPrefs.Save();
        ApplyTurning();
    }

    public static void SetSnapAngle(int degrees)
    {
        SnapAngle = Mathf.Clamp(degrees, 15, 90);
        PlayerPrefs.SetInt(SnapAngleKey, SnapAngle);
        PlayerPrefs.Save();
        ApplyTurning();
    }

    public static void SetVignetteEnabled(bool enabled)
    {
        VignetteEnabled = enabled;
        PlayerPrefs.SetInt(VignetteKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetMasterVolume(float volume01)
    {
        MasterVolume = Mathf.Clamp01(volume01);
        PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        PlayerPrefs.Save();
        AudioListener.volume = MasterVolume;
    }

    static void ApplyAll()
    {
        ApplyTurning();
        AudioListener.volume = MasterVolume;
    }

    static void ApplyTurning()
    {
        foreach (var b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (b == null) continue;
            var type = b.GetType();
            var name = type.Name;

            if (name.Contains("SnapTurn"))
            {
                b.enabled = (Current == Mode.Snap);
                TrySetFloatMember(b, type, "turnAmount", SnapAngle);
                TrySetFloatMember(b, type, "m_TurnAmount", SnapAngle);
            }
            else if (name.Contains("ContinuousTurn"))
            {
                b.enabled = (Current == Mode.Smooth);
            }
        }
    }

    static void TrySetFloatMember(object target, System.Type t, string member, float value)
    {
        try
        {
            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && (f.FieldType == typeof(float) || f.FieldType == typeof(int)))
            {
                f.SetValue(target, f.FieldType == typeof(int) ? (object)Mathf.RoundToInt(value) : value);
                return;
            }
            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite && (p.PropertyType == typeof(float) || p.PropertyType == typeof(int)))
            {
                p.SetValue(target, p.PropertyType == typeof(int) ? (object)Mathf.RoundToInt(value) : value);
            }
        }
        catch {  }
    }
}
