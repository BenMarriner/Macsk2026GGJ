using System.Collections.Generic;
using UnityEngine;

public class GreenObject : ColouredObject
{
    [SerializeField] private List<Renderer> _highlightMeshes;
    [SerializeField] private string _solhouetteLayer;

    protected bool _greenMaskMode = false;
    private IInteractable _interactable;
    private int _defaultObjectLayer;
    protected bool _silhouetteEnabled = false;
    protected bool _wasEnabledAtStart;
    private Transform[] _allObjectTransforms;

    protected virtual void Start()
    {
        if (_interactable == null && TryGetComponent(out IInteractable pairedInteractable))
        {
            _interactable = pairedInteractable;
        }

        _defaultObjectLayer = gameObject.layer;

        _wasEnabledAtStart = _isEnabled;

        Unhighlight();
        _allObjectTransforms = GetComponentsInChildren<Transform>();
        _defaultMaterialList = GetDefaultMaterialList(_allObjectTransforms);
        
        SetGreenEffect(_greenMaskMode);
    }

    public override void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        
        SetGreenEffect(_greenMaskMode);
    }

    protected override void SetGreenEffect(bool greenEnabled)
    {
        _greenMaskMode = greenEnabled;
        bool interactionEnabled = _greenMaskMode;
        if (_effectReversed)
        {
            interactionEnabled = !enabled;
        }

        UpdateAmbientSFX(_silhouetteEnabled && _greenMaskMode && _isEnabled);

        if (interactionEnabled && _isEnabled)
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
        // Loop over over mesh-default materials pairs, then
        // loop over the mesh's current materials and replace them with _colouredMaterial
        if (!_isEnabled) return;
        for (int i = 0; i < _defaultMaterialList.Count; i++)
        {
            Material[] meshCurrentMaterials = _defaultMaterialList[i].First.materials;
            for (int l = 0; l < meshCurrentMaterials.Length; l++)
            {
                meshCurrentMaterials[l] = _colouredMaterial;
            }

            _defaultMaterialList[i].First.materials = meshCurrentMaterials;
        }

        if (_silhouetteEnabled)
        {
            SetSelfAndChildrenLayers(LayerMask.NameToLayer(_solhouetteLayer));
        }
    }

    protected virtual void DisableGreenEffect()
    {
        // Loop over over mesh-default materials pairs, loop over the mesh's current materials
        // then loop over the mesh's default materials and replace the current with the default material
        for (int i = 0; i < _defaultMaterialList.Count; i++)
        {
            Material[] meshDefaultMaterials = _defaultMaterialList[i].Second;
            Material[] meshCurrentMaterials = _defaultMaterialList[i].First.materials;
            for (int l = 0; l < meshCurrentMaterials.Length; l++)
            {
                for (int j = 0; j < meshDefaultMaterials.Length; j++)
                {
                    meshCurrentMaterials[l] = meshDefaultMaterials[j];
                }
            }

            _defaultMaterialList[i].First.materials = meshDefaultMaterials;
        }

        SetSelfAndChildrenLayers(_defaultObjectLayer);
        Unhighlight();
    }

    public virtual void Highlight()
    {
        if (!_isEnabled) return;
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

        UpdateAmbientSFX(_silhouetteEnabled && _greenMaskMode && _isEnabled);

        if (_silhouetteEnabled && _greenMaskMode && _isEnabled)
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
