using System;

[Serializable]
public struct MusicFadeData
{
    public MusicKey MusicKey;
    public float FadeTime;
    public float FinalVolume;

    public MusicFadeData(MusicKey inMusicKey, float inFadeTime, float inFinalVolume)
    {
        MusicKey = inMusicKey;
        FadeTime = inFadeTime;
        FinalVolume = inFinalVolume;
    }
}
