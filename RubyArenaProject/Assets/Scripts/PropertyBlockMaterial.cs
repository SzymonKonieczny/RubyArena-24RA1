using UnityEngine;

public class PropertyBlockMaterial
{
    public Renderer[] renderers;
    public MaterialPropertyBlock propertyBlock;

    public PropertyBlockMaterial (Renderer[] renderArr)
    {
        propertyBlock = new();
        renderers = renderArr;
        foreach (Renderer r in renderers)
        {
            r.SetPropertyBlock (propertyBlock);
        }    
    }
    public void SetFloat(string name, float value)
    {
        propertyBlock.SetFloat(name, value);
        foreach (Renderer r in renderers)
        {
            r.SetPropertyBlock(propertyBlock);
        }
    }
}