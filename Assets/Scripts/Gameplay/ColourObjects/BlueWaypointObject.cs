using System.Collections;
using UnityEngine;

public class BlueWaypointObject : BlueObject
{
    [SerializeField] protected GameObject _movingPlatform;
    [SerializeField] protected WaypointPath _waypointPath;
    [SerializeField] private bool _useWaypointRotation;
    [SerializeField] private bool _slowNearEnd;
    [SerializeField] private float _arrivalPauseTime = 0.5f;

    [Header("SFX")]
    [SerializeField] private bool _playSoundWhenReachedWaypoint = false;
    [SerializeField] private AudioSource _waypointReachedSound;
    [SerializeField] private AudioClip[] _ambientClips;
    private int _audioClipIndex = 0;

    protected int _targetWaypointIndex;

    protected Transform _previousWaypoint;
    protected Transform _targetWaypoint;

    protected float _elapsedTime;

    protected override void Start()
    {
        _defaultMaterialList = GetDefaultMaterialList(GetComponentsInChildren<Transform>());
        _movingPlatform.transform.position = _waypointPath.GetWaypoint(_targetWaypointIndex).transform.position;
        TargetNextWaypoint();

        if (_ambientSFXSource)
        {
            _ambientSFXSource.volume = 1f * _maxVolumeMultiplier;
        }

        IncrementAmbientSFX();
        SetBlueEffect(false);
    }

    private void FixedUpdate()
    {
        if (!_isEnabled || !_isMoving) return;

        _elapsedTime += Time.deltaTime;

        if (_movingPaused)
        {
            if (_elapsedTime > _arrivalPauseTime) Unpause();
            return;
        }

        float elapsedPercentage = _elapsedTime / _speed;
        
        if (_slowNearEnd)
        {
            elapsedPercentage = Mathf.SmoothStep(0, 1, elapsedPercentage);
        }

        _movingPlatform.transform.position = Vector3.Lerp(_previousWaypoint.position, _targetWaypoint.position, elapsedPercentage);

        if (_useWaypointRotation)
        {
            _movingPlatform.transform.rotation = Quaternion.Lerp(_previousWaypoint.rotation, _targetWaypoint.rotation, elapsedPercentage);
        }

        if (elapsedPercentage >= 1)
        {
            TargetNextWaypoint();
            _movingPaused = true;
            if (_playSoundWhenReachedWaypoint || _waypointReachedSound)
            {
                _waypointReachedSound.Play();
            }
            UpdateAmbientSFX(_isMoving && !_movingPaused);
            IncrementAmbientSFX();
        }
    }

    protected virtual void TargetNextWaypoint()
    {
        _previousWaypoint = _waypointPath.GetWaypoint(_targetWaypointIndex);
        _targetWaypointIndex = _waypointPath.GetNextWaypointIndex(_targetWaypointIndex);
        _targetWaypoint = _waypointPath.GetWaypoint(_targetWaypointIndex);

        _elapsedTime = 0f;
    }

    protected virtual void Unpause()
    {
        _elapsedTime = 0f;
        _movingPaused = false;
        UpdateAmbientSFX(_isMoving && !_movingPaused);
    }

    protected override void SetMusicSyncTime(object eventData){}

    protected override void StartSyncedAmbientSFX(object eventData){}

    protected override void UpdateAmbientSFX(bool isActive)
    {
        if (!_ambientSFXSource) return;

        if (isActive)
        {
            _ambientSFXSource.Play();
        }
        else
        {
            _ambientSFXSource.Stop();
        }
    }

    protected virtual void IncrementAmbientSFX()
    {
        if (!_ambientSFXSource) return;
        if (_ambientClips.Length < 2) return;

        if (_audioClipIndex > _ambientClips.Length-1)
        {
            _audioClipIndex = 0;
        }

        _ambientSFXSource.clip = _ambientClips[_audioClipIndex];

        _audioClipIndex++;
    }
}
