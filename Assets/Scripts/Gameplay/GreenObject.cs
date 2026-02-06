using System.Collections.Generic;
using UnityEngine;

public class GreenObject : ColouredObject
{
    [SerializeField] private List<Renderer> _highlightMeshes;
    [SerializeField] private string _solhouetteLayer;

    private bool _greenMaskMode = false;
    private IInteractable _interactable;
    private int _defaultObjectLayer;
    private bool _silhouetteEnabled = false;
    private Transform[] _allObjectTransforms;

    protected virtual void Start()
    {
        if (_interactable == null && TryGetComponent(out IInteractable pairedInteractable))
        {
            _interactable = pairedInteractable;
        }

        _defaultObjectLayer = gameObject.layer;
        _allObjectTransforms = GetComponentsInChildren<Transform>();

        _defaultMaterialList = GetDefaultMaterialList(_allObjectTransforms);

        Unhighlight();
        DisableGreenEffect();
    }

    protected override void SetGreenEffect(bool enabled)
    {
        if (_effectReversed)
        {
            enabled = !enabled;
        }

        if (enabled)
        {
            EnableGreenEffect();
        }
        else
        {
            DisableGreenEffect();
        }
    }

    protected virtual void EnableGreenEffect()
    {
        _greenMaskMode = true;
        foreach (GenericCouple<Renderer, Material> item in _defaultMaterialList)
        {
            item.First.material = _colouredMaterial;
        }

        if (_silhouetteEnabled)
        {
            SetSelfAndChildrenLayers(LayerMask.NameToLayer(_solhouetteLayer));
        }
    }

    protected virtual void DisableGreenEffect()
    {
        _greenMaskMode = false;
        foreach (GenericCouple<Renderer, Material> item in _defaultMaterialList)
        {
            item.First.material = item.Second;
        }

        SetSelfAndChildrenLayers(_defaultObjectLayer);
        Unhighlight();
    }

    public virtual void Highlight()
    {
        _interactable.SetCanBeInteracted(true);
        foreach (Renderer item in _highlightMeshes)
        {
            item.enabled = true;
        }
    }

    public virtual void Unhighlight()
    {
        _interactable.SetCanBeInteracted(false);
        foreach (Renderer item in _highlightMeshes)
        {
            item.enabled = false;
        }
    }

    public virtual void SetSilhouetteEnabled(bool enabled)
    {
        _silhouetteEnabled = enabled;

        if (_silhouetteEnabled && _greenMaskMode)
        {
            SetSelfAndChildrenLayers(LayerMask.NameToLayer(_solhouetteLayer));
        }

        if (!_silhouetteEnabled)
        {
            SetSelfAndChildrenLayers(_defaultObjectLayer);
        }
    }

    protected virtual void SetSelfAndChildrenLayers(int layerId)
    {
        foreach (Transform item in _allObjectTransforms)
        {
            item.gameObject.layer = layerId;
        }
    }
}
