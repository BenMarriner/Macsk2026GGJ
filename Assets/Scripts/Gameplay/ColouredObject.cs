using System.Collections.Generic;
using UnityEngine;

public class ColouredObject : MaskChangeDetector
{
    [SerializeField] protected Material _colouredMaterial;
    protected List<GenericCouple<Renderer, Material>> _defaultMaterialList = new();

    // Loop through all children of the gameobject, getting the renderers and 
    // their default material, then adding them to a list
    //
    // not the most performant, but easier for designers to add it to an object
    protected List<GenericCouple<Renderer, Material>> GetDefaultMaterialList(Transform[] transformArray)
    {
        List<GenericCouple<Renderer, Material>> defaultMaterialList = new();
        
        foreach (Transform item in transformArray)
        {
            if (item.TryGetComponent(out Renderer renderer))
            {
                Material objectMaterial = renderer.material;
                defaultMaterialList.Add(new GenericCouple<Renderer, Material>(renderer, objectMaterial));
            }
        }

        return defaultMaterialList;
    } 
}
