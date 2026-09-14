using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BbdCards.Helpers;

internal static class MaterialFactory
{
    public static Material FindTemplate(GameObject donor)
    {
        var renderer = donor.GetComponentInChildren<MeshRenderer>(true);

        if (!renderer || !renderer.sharedMaterial)
        {
            throw new InvalidOperationException("Base material is missing.");
        }

        return renderer.sharedMaterial;
    }

    public static Material Create(Material template, string name, Color color, float metallic, float smoothness)
    {
        var material = new Material(template) { name = name };

        try
        {
            foreach (string property in material.GetTexturePropertyNames())
            {
                material.SetTexture(property, null);
            }

            material.shaderKeywords = Array.Empty<string>();
            material.color = color;

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            }

            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;

            return material;
        }
        catch
        {
            Object.Destroy(material);

            throw;
        }
    }
}
