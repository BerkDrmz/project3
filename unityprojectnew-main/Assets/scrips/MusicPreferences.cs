using UnityEngine;

public static class MusicPreferences
{
    private const string MusicEnabledKey = "NeuroCharge_MusicEnabled";

    public static bool IsMusicEnabled
    {
        get { return PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1; }
    }

    public static void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyToLoopingAudioSources();
    }

    public static void ApplyToAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.mute = !IsMusicEnabled;
    }

    public static void ApplyToLoopingAudioSources()
    {
        AudioSource[] audioSources = Object.FindObjectsOfType<AudioSource>();
        for (int i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null && audioSources[i].loop)
            {
                ApplyToAudioSource(audioSources[i]);
            }
        }
    }
}
