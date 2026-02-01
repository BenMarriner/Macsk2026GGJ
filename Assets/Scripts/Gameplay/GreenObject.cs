using System.Collections.Generic;
using UnityEngine;

public class GreenObject : MaskChangeDetector
{
    [SerializeField] private Material _greenMaterial;
    private Material _defaultObjectMaterial;
    [SerializeField] private List<Renderer> _highlightMeshes;

    private Renderer _objectRenderer;

    protected virtual void Start()
    {
        _objectRenderer = GetComponentInChildren<Renderer>();
        if (_objectRenderer != null)
        {
            _defaultObjectMaterial = _objectRenderer.material;
        }

        Unhighlight();
        DisableGreenEffect();
    }

    protected override void EnableGreenEffect()
    {
        _objectRenderer.material = _greenMaterial;
    }

    protected override void DisableGreenEffect()
    {
        _objectRenderer.material = _defaultObjectMaterial;
        Unhighlight();
    }

    public virtual void Highlight()
    {
        foreach (Renderer item in _highlightMeshes)
        {
            item.enabled = true;
        }
    }

    public virtual void Unhighlight()
    {
        foreach (Renderer item in _highlightMeshes)
        {
            item.enabled = false;
        }
    }
}
