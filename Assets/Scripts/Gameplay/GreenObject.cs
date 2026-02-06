using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GreenObject : MaskChangeDetector
{
    [SerializeField] private Material _greenMaterial;
    [SerializeField] private List<Renderer> _highlightMeshes;
    [SerializeField] private string _solhouetteLayer;

    private List<GenericCouple<Renderer, Material>> _defaultMaterialList = new();

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
        _allObjectTransforms.Append(transform);

        // Loop through all children of the gameobject, getting the renderers and 
        // their default material, then adding them to a list
        //
        // not the most performant, but easier for designers to add it to an object
        foreach (Transform item in _allObjectTransforms)
        {
            if (item.TryGetComponent(out Renderer renderer))
            {
                Material objectMaterial = renderer.material;
                _defaultMaterialList.Add(new GenericCouple<Renderer, Material>(renderer, objectMaterial));
            }
        }

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
            item.First.material = _greenMaterial;
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
