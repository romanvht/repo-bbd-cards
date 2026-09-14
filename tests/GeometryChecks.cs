using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BbdCards;

static class GeometryChecks
{
    static void Require(bool ok, string message)
    {
        if (!ok)
        {
            throw new Exception(message);
        }
    }

    static Vector3 Position(CardGeometry.Vertex v) => new(v.X, v.Y, v.Z);

    static string Key(Vector3 p) => $"{Math.Round(p.X, 6)}/{Math.Round(p.Y, 6)}/{Math.Round(p.Z, 6)}";

    static void Main()
    {
        var data = CardGeometry.Create();

        Require(data.Triangles.Count / 3 == 112, "Triangle budget");

        var edges = new Dictionary<(string, string), (int count, int direction)>();
        double volume = 0;

        for (int i = 0; i < data.Triangles.Count; i += 3)
        {
            var indices = data.Triangles.Skip(i).Take(3).ToArray();

            Require(indices.All(j => j >= 0 && j < data.Vertices.Count), "Index bounds");

            var points = indices.Select(j => Position(data.Vertices[j])).ToArray();
            var normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]);

            Require(normal.Length() > 1e-9, "Degenerate triangle");

            var center = (points[0] + points[1] + points[2]) / 3 - new Vector3(0, CardGeometry.Side / 2, 0);

            Require(Vector3.Dot(normal, center) > 0, "Every triangle must face outward");
            volume += Vector3.Dot(points[0], Vector3.Cross(points[1], points[2])) / 6.0;

            for (int j = 0; j < 3; j++)
            {
                string a = Key(points[j]);
                string b = Key(points[(j + 1) % 3]);
                bool ordered = string.CompareOrdinal(a, b) < 0;
                var key = ordered ? (a, b) : (b, a);

                edges.TryGetValue(key, out var edge);
                edges[key] = (edge.count + 1, edge.direction + (ordered ? 1 : -1));
            }
        }

        Require(edges.Values.All(v => v.count == 2 && v.direction == 0), "Mesh must be watertight with consistent winding");

        double exactVolume = (Math.Pow(CardGeometry.Side, 2) - (4 - Math.PI) * Math.Pow(CardGeometry.Radius, 2)) * CardGeometry.Thickness;

        Require(Math.Abs(volume / exactVolume - 1) < .001, "Rounded card volume");

        var vertices = data.Vertices;

        Require(Math.Abs(vertices.Max(v => v.X) - vertices.Min(v => v.X) - CardGeometry.Side) < 1e-6, "Width");
        Require(Math.Abs(vertices.Max(v => v.Y) - vertices.Min(v => v.Y) - CardGeometry.Side) < 1e-6, "Height");
        Require(Math.Abs(vertices.Max(v => v.Z) - vertices.Min(v => v.Z) - CardGeometry.Thickness) < 1e-6, "Thickness");
        Require(Math.Abs(vertices.Max(v => v.Z) - vertices.Min(v => v.Z) - .018f) < 1e-6, "Requested 18 mm card thickness");
        Require(vertices.All(v => Math.Abs(v.X) <= CardGeometry.Side / 2 + 1e-6
            && v.Y >= -1e-6 && v.Y <= CardGeometry.Side + 1e-6
            && Math.Abs(v.Z) <= CardGeometry.Thickness / 2 + 1e-6), "Card lies inside the matching box collider");
        Require(vertices.All(v => v.U >= 0 && v.U <= 1 && v.V >= 0 && v.V <= 1), "UV bounds");

        int ringCount = 4 * (CardGeometry.CornerSegments + 1);

        for (int i = 1; i <= ringCount; i++)
        {
            var front = vertices[i];
            var back = vertices[ringCount + 1 + i];

            Require(Math.Abs(front.U + back.U - 1) < 1e-6 && Math.Abs(front.V - back.V) < 1e-6, "Back text must be unmirrored and upright");

            float h = CardGeometry.Side / 2;
            float dx = Math.Max(Math.Abs(front.X) - (h - CardGeometry.Radius), 0);
            float dy = Math.Max(Math.Abs(front.Y - h) - (h - CardGeometry.Radius), 0);

            Require(Math.Abs(Math.Sqrt(dx * dx + dy * dy) - CardGeometry.Radius) < 1e-6, "Rounded perimeter");
        }

        Console.WriteLine($"PASS: {vertices.Count} vertices, 112 triangles; mesh, 18 mm thickness, bounds and UVs.");
    }
}
