using System;
using System.Collections.Generic;
using System.Linq;
using REPOLib.Modules;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BbdCards.Helpers;

internal sealed class ValuablePrefab : IDisposable
{
    private readonly List<Object> resources = new();

    private bool registered;

    public GameObject Root { get; }

    /// <summary>Clones an inactive donor and removes its original model and colliders.</summary>
    public ValuablePrefab(GameObject donor, Transform storage, string name)
    {
        if (storage.gameObject.activeInHierarchy)
        {
            throw new InvalidOperationException("Prefab storage must be inactive.");
        }

        Root = Object.Instantiate(donor, storage, false);
        Root.name = name;
        Root.transform.localPosition = Vector3.zero;
        Root.transform.localRotation = Quaternion.identity;
        Root.transform.localScale = Vector3.one;

        foreach (Transform child in Root.transform.Cast<Transform>().ToArray())
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    /// <summary>Loads the stock HDD used as the base prefab.</summary>
    public static GameObject LoadDonor()
    {
        var donor = Resources.Load<GameObject>("Valuables/02 Small/Valuable Arctic HDD");

        if (!donor)
        {
            throw new InvalidOperationException("Base prefab Valuable Arctic HDD is missing; check the game version.");
        }

        return donor;
    }

    /// <summary>Creates persistent inactive storage for registered prefabs.</summary>
    public static GameObject CreateStorage(string name)
    {
        var storage = new GameObject(name);

        storage.SetActive(false);
        Object.DontDestroyOnLoad(storage);

        return storage;
    }

    /// <summary>Tracks an allocated resource for cleanup if registration fails.</summary>
    public T Own<T>(T resource) where T : Object
    {
        resources.Add(resource);

        return resource;
    }

    /// <summary>Attaches a visible mesh and its material to the prefab.</summary>
    public void AddVisual(string name, Mesh mesh, Material material)
    {
        var visual = new GameObject(name);

        visual.transform.SetParent(Root.transform, false);
        visual.layer = Root.layer;
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    /// <summary>Sets value, durability, mass and room bounds for the finished shape.</summary>
    public void Configure(float min, float max, float mass, float fragility, Vector3 center)
    {
        var valuable = Root.GetComponent<ValuableObject>();

        valuable.debugVolume = false;
        valuable.volumeType = ValuableVolume.Type.Small;

        var value = Own(ScriptableObject.CreateInstance<Value>());

        value.valueMin = min;
        value.valueMax = Mathf.Max(min, max);
        valuable.valuePreset = value;

        var attributes = Own(ScriptableObject.CreateInstance<PhysAttribute>());

        attributes.mass = mass;
        valuable.physAttributePreset = attributes;

        var durability = Own(ScriptableObject.CreateInstance<Durability>());

        durability.fragility = fragility;
        durability.durability = 100;
        valuable.durabilityPreset = durability;

        var body = Root.GetComponent<Rigidbody>();

        body.mass = mass;
        body.centerOfMass = center;
        Root.GetComponent<PhysGrabObject>().massOriginal = mass;

        var massCenter = new GameObject("Center of Mass");

        massCenter.transform.SetParent(Root.transform, false);
        massCenter.transform.localPosition = center;

        var bounds = StockCollider.GetBounds(Root.transform);
        var room = Root.GetComponent<RoomVolumeCheck>();

        room.CheckPosition = bounds.center;
        room.currentSize = bounds.size;
    }

    /// <summary>Registers the prefab and transfers its resources to the game.</summary>
    public PrefabRef Register()
    {
        var reference = Valuables.RegisterValuable(Root);

        if (reference == null)
        {
            throw new InvalidOperationException("REPOLib rejected registration.");
        }

        registered = true;

        return reference;
    }

    /// <summary>Releases only resources belonging to an unregistered prefab.</summary>
    public void Dispose()
    {
        if (registered)
        {
            return;
        }

        if (Root)
        {
            Object.Destroy(Root);
        }

        foreach (var resource in resources)
        {
            if (resource)
            {
                Object.Destroy(resource);
            }
        }

        resources.Clear();
    }
}
