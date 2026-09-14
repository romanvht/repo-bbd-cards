using System;
using System.Collections.Generic;

namespace BbdCards;

internal static class CardGeometry
{
    public const float Side = 0.34f;

    public const float Thickness = 0.018f;

    public const float Radius = 0.018f;

    public const int CornerSegments = 6;

    internal readonly struct Vertex
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float U;
        public readonly float V;

        /// <summary>Stores a vertex position and texture coordinates.</summary>
        public Vertex(float x, float y, float z, float u, float v)
        {
            X = x;
            Y = y;
            Z = z;
            U = u;
            V = v;
        }
    }

    internal sealed class Data
    {
        public readonly List<Vertex> Vertices = new();

        public readonly List<int> Triangles = new();

        /// <summary>Appends a triangle in the supplied winding order.</summary>
        public void Triangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }
    }

    /// <summary>Builds the two printed faces and the rounded rim.</summary>
    public static Data Create()
    {
        var data = new Data();
        var ring = CreateRing();

        AddFace(data, ring, false);
        AddFace(data, ring, true);
        AddRim(data, ring);

        return data;
    }

    /// <summary>Samples the perimeter of the rounded square.</summary>
    private static List<(float x, float y)> CreateRing()
    {
        var ring = new List<(float x, float y)>();
        float halfSide = Side / 2;

        for (int corner = 0; corner < 4; corner++)
        {
            float centerX = corner == 0 || corner == 3 ? halfSide - Radius : -halfSide + Radius;
            float centerY = corner < 2 ? Side - Radius : Radius;

            for (int step = 0; step <= CornerSegments; step++)
            {
                double angle = (corner * 90.0 + step * 90.0 / CornerSegments) * Math.PI / 180;

                ring.Add((centerX + Radius * (float)Math.Cos(angle), centerY + Radius * (float)Math.Sin(angle)));
            }
        }

        return ring;
    }

    /// <summary>Adds one face with outward winding and readable image orientation.</summary>
    private static void AddFace(Data data, List<(float x, float y)> ring, bool back)
    {
        int center = data.Vertices.Count;
        float z = back ? Thickness / 2 : -Thickness / 2;

        data.Vertices.Add(new Vertex(0, Side / 2, z, 0.5f, 0.5f));

        foreach (var point in ring)
        {
            float u = Math.Max(0, Math.Min(1, point.x / Side + 0.5f));
            float v = Math.Max(0, Math.Min(1, point.y / Side));

            data.Vertices.Add(new Vertex(point.x, point.y, z, back ? 1 - u : u, v));
        }

        for (int i = 0; i < ring.Count; i++)
        {
            int a = center + 1 + i;
            int b = center + 1 + (i + 1) % ring.Count;

            if (back)
            {
                data.Triangle(center, a, b);
            }
            else
            {
                data.Triangle(center, b, a);
            }
        }
    }

    /// <summary>Adds a rim with separate normals and colors sampled from the image corner.</summary>
    private static void AddRim(Data data, List<(float x, float y)> ring)
    {
        float halfThickness = Thickness / 2;

        for (int i = 0; i < ring.Count; i++)
        {
            var a = ring[i];
            var b = ring[(i + 1) % ring.Count];
            int start = data.Vertices.Count;

            data.Vertices.Add(new Vertex(a.x, a.y, -halfThickness, 0.001f, 0.001f));
            data.Vertices.Add(new Vertex(b.x, b.y, -halfThickness, 0.002f, 0.001f));
            data.Vertices.Add(new Vertex(b.x, b.y, halfThickness, 0.002f, 0.002f));
            data.Vertices.Add(new Vertex(a.x, a.y, halfThickness, 0.001f, 0.002f));
            data.Triangle(start, start + 1, start + 2);
            data.Triangle(start, start + 2, start + 3);
        }
    }
}
