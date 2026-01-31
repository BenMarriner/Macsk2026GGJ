using System.Collections;
using UnityEngine;

public class BlueObject : MaskChangeDetector
{
    [SerializeField] private GameObject _movingPlatform;
    [SerializeField] private Transform _platformEndPointA;
    [SerializeField] private Transform _platformEndPointB;
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _delay = 1f;

    private Vector3 _currentPlatformDestination;

    private bool _isMoving = true;

    IEnumerator MovePlayformCoroutine;

    void Start()
    {
        _movingPlatform.transform.position = _platformEndPointA.position;
        _currentPlatformDestination = _platformEndPointA.position;
        DisableBlueEffect();
    }

    protected override void EnableBlueEffect()
    {
        DebugLogger.Log("trigger true");
        _isMoving = !_effectReversed;

        if (_isMoving)
        {
            StartCoroutine(MovePlatform());
        }
    }

    protected override void DisableBlueEffect()
    {
        DebugLogger.Log("trigger false");
        _isMoving = _effectReversed;

        if (_isMoving)
        {
            StartCoroutine(MovePlatform());
        }
    }

    IEnumerator MovePlatform()
    {
        while (_isMoving)
        {
            while ((_currentPlatformDestination - _movingPlatform.transform.position).sqrMagnitude > 0.01f)
            {
                if (!_isMoving)
                {
                    yield break;
                }
                _movingPlatform.transform.position = Vector3.MoveTowards(_movingPlatform.transform.position, 
                _currentPlatformDestination, _speed * Time.deltaTime);
                yield return null;
            }

            _currentPlatformDestination = _currentPlatformDestination == _platformEndPointA.position 
            ? _platformEndPointB.position : _platformEndPointA.position;

            yield return new WaitForSeconds(_delay);
        }
    }
}
