using System;
using UnityEngine;

public class ResponseActivation : AbstractActivation
{
    [SerializeField] private SoundAnimPair[] _soundAnimPairs;
    [SerializeField] private Animator animator;
    [SerializeField] private bool _loopAnimations = true;
    [SerializeField] protected AudioSource _responseSfxSource;
    private int _activationAmount = 0;
    
    public override void Activate()
    {
        _activated = true;
        
        if (_soundAnimPairs.Length < _activationAmount)
        {
            return;
        }

        if (_responseSfxSource && _soundAnimPairs[_activationAmount].SoundClip)
        {
            _responseSfxSource.clip = _soundAnimPairs[_activationAmount].SoundClip;
            _responseSfxSource.Play();
        }

        animator.Play(_soundAnimPairs[_activationAmount].AnimClip.name);
        _activationAmount++;

        if (_soundAnimPairs.Length < _activationAmount &&_loopAnimations) 
        {
            _activationAmount = 0;
        }
    }

    [Serializable]
    public struct SoundAnimPair
    {
        public AudioClip SoundClip;
        public AnimationClip AnimClip;

        public SoundAnimPair(AudioClip inSoundClip, AnimationClip inAnimClip)
        {
            SoundClip = inSoundClip;
            AnimClip = inAnimClip;
        }
    }
}
