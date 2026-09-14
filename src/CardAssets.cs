using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BbdCards.Helpers;
using UnityEngine;

namespace BbdCards;

internal static class CardAssets
{
    private const string Prefix = "BbdCards.Cards.";

    public static readonly string[] Ids = Assembly.GetExecutingAssembly().GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal) && n.EndsWith(".png", StringComparison.Ordinal))
            .Select(n => n.Substring(Prefix.Length, n.Length - Prefix.Length - 4))
            .OrderBy(n => n, StringComparer.Ordinal).ToArray();

    public static Mesh CreateMesh()
    {
        var data = CardGeometry.Create();
        var mesh = new Mesh { name = "BbdCards/Rounded card" };

        mesh.vertices = data.Vertices.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
        mesh.uv = data.Vertices.Select(v => new Vector2(v.U, v.V)).ToArray();
        mesh.triangles = data.Triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        mesh.UploadMeshData(true);

        return mesh;
    }

    public static Material CreateMaterial(string id, Material template)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Prefix + id + ".png")
            ?? throw new FileNotFoundException("Card texture missing: " + id);
        using var bytes = new MemoryStream();

        stream.CopyTo(bytes);

        var texture = new Texture2D(2, 2, TextureFormat.RGB24, true)
        {
            name = "BbdCards/" + id,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 2
        };
        Material? material = null;

        try
        {
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true))
            {
                throw new InvalidDataException("Cannot decode card: " + id);
            }

            material = MaterialFactory.Create(template, texture.name, Color.white, 0, 0.15f);
            material.mainTexture = texture;

            return material;
        }
        catch
        {
            UnityEngine.Object.Destroy(texture);

            if (material)
            {
                UnityEngine.Object.Destroy(material);
            }

            throw;
        }
    }
}
