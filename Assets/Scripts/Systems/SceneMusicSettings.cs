using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Scene Music Settings")]
public class SceneMusicSettings : ScriptableObject
{
    [SerializeField] private List<SceneMusicTrack> tracks = new();

    public bool TryGetTrack(string sceneName, out SceneMusicTrack track)
    {
        foreach (SceneMusicTrack candidate in tracks)
        {
            if (candidate.SceneName == sceneName)
            {
                track = candidate;
                return true;
            }
        }

        track = null;
        return false;
    }
}

[Serializable]
public class SceneMusicTrack
{
    [SerializeField] private string sceneName;
    [SerializeField] private AudioClip clip;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool continueCurrentTrack;

    public string SceneName => sceneName;
    public AudioClip Clip => clip;
    public bool Loop => loop;
    public bool ContinueCurrentTrack => continueCurrentTrack;
}
