using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BbdCards.Helpers;

internal static class StockCollider
{
    internal const float Padding = 0.015f;

    internal static BoxCollider FindTemplate(GameObject donor)
    {
        foreach (var box in donor.GetComponentsInChildren<BoxCollider>(true))
        {
            if (!box.isTrigger && box.enabled && box.GetComponent<PhysGrabObjectCollider>()
                && box.GetComponent<PhysGrabObjectBoxCollider>() && box.sharedMaterial)
            {
                return box;
            }
        }

        throw new InvalidOperationException("Base prefab has no supported stock valuable collider.");
    }

    internal static void Attach(BoxCollider template, Transform parent, string name,
        Vector3 center, Vector3 size, Quaternion rotation)
    {
        var obj = Object.Instantiate(template.gameObject, parent, false);

        obj.name = name;
        obj.transform.localPosition = center;
        obj.transform.localRotation = rotation;
        obj.transform.localScale = Vector3.one;

        var box = obj.GetComponent<BoxCollider>();

        box.center = Vector3.zero;
        box.size = size + Vector3.one * (2 * Padding);
        RemoveVisualization(obj);
    }

    private static void RemoveVisualization(GameObject obj)
    {
        var renderer = obj.GetComponent<MeshRenderer>();

        if (renderer)
        {
            Object.DestroyImmediate(renderer);
        }

        var filter = obj.GetComponent<MeshFilter>();

        if (filter)
        {
            Object.DestroyImmediate(filter);
        }
    }

    /// <summary>Calculates local collision bounds even when the prefab is inactive.</summary>
    internal static Bounds GetBounds(Transform root)
    {
        var bounds = new Bounds();
        bool first = true;

        foreach (var box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (box.isTrigger || !box.enabled)
            {
                continue;
            }

            for (int corner = 0; corner < 8; corner++)
            {
                var point = GetCorner(root, box, corner);

                if (first)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        if (first)
        {
            throw new InvalidOperationException("No solid colliders on the valuable prefab.");
        }

        return bounds;
    }

    private static Vector3 GetCorner(Transform root, BoxCollider box, int corner)
    {
        var signs = new Vector3(
            (corner & 1) == 0 ? -1 : 1,
            (corner & 2) == 0 ? -1 : 1,
            (corner & 4) == 0 ? -1 : 1);
        var offset = Vector3.Scale(box.size * 0.5f, signs);

        return root.InverseTransformPoint(box.transform.TransformPoint(box.center + offset));
    }
}
