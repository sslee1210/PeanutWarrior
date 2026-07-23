using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public static class SkillIconFactoryV6
    {
        private const int Size = 128;

        public static Sprite Create(int index)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "PeanutSpectacularSkillIcon" + index;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++) texture.SetPixel(x, y, clear);

            Ring(texture, 64, 64, 55, 3, 0.38f);
            switch (Mathf.Clamp(index, 0, 7))
            {
                case 0: ShellCyclone(texture); break;
                case 1: FallingFlowerRain(texture); break;
                case 2: LeylinePods(texture); break;
                case 3: RoyalPodArmory(texture); break;
                case 4: CarapaceRelease(texture); break;
                case 5: PeanutChainSword(texture); break;
                case 6: FallenFlowerRoot(texture); break;
                default: GoldenCoreHeavenSever(texture); break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 128f);
        }

        public static Color ColorFor(int index)
        {
            Color[] colors =
            {
                new Color(0.88f, 0.64f, 0.12f),
                new Color(0.96f, 0.48f, 0.25f),
                new Color(0.30f, 0.72f, 0.32f),
                new Color(0.96f, 0.72f, 0.10f),
                new Color(0.82f, 0.38f, 0.13f),
                new Color(0.88f, 0.20f, 0.18f),
                new Color(0.56f, 0.30f, 0.82f),
                new Color(0.98f, 0.78f, 0.16f)
            };
            return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
        }

        private static void ShellCyclone(Texture2D texture)
        {
            Ring(texture, 64, 64, 29, 4, 1f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector2 inner = Point(64, 64, 24f, angle);
                Vector2 outer = Point(64, 64, 47f, angle + 0.16f);
                Sword(texture, inner, outer, 4, 1f);
            }
            FilledCircle(texture, 64, 64, 7, 1f);
        }

        private static void FallingFlowerRain(Texture2D texture)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector2 center = Point(64, 42, 18f, angle);
                FilledCircle(texture, Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y), 9, 0.92f);
            }
            FilledCircle(texture, 64, 42, 8, 1f);
            for (int i = 0; i < 7; i++)
            {
                float x = 27f + i * 12f;
                Sword(texture, new Vector2(x, 60f + (i % 2) * 7f), new Vector2(x - 8f, 108f), 3, 1f);
            }
        }

        private static void LeylinePods(Texture2D texture)
        {
            FilledCircle(texture, 64, 64, 7, 1f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector2 pod = Point(64, 64, 39f, angle);
                Line(texture, new Vector2(64f, 64f), pod, 2, 0.82f);
                FilledCircle(texture, Mathf.RoundToInt(pod.x - 5f), Mathf.RoundToInt(pod.y), 8, 1f);
                FilledCircle(texture, Mathf.RoundToInt(pod.x + 5f), Mathf.RoundToInt(pod.y), 8, 1f);
                Sword(texture, pod + Vector2.down * 10f, pod + Vector2.up * 17f, 3, 0.90f);
            }
        }

        private static void RoyalPodArmory(Texture2D texture)
        {
            FilledCircle(texture, 53, 34, 20, 0.78f);
            FilledCircle(texture, 75, 34, 20, 0.78f);
            Line(texture, new Vector2(39f, 37f), new Vector2(26f, 21f), 4, 1f);
            Line(texture, new Vector2(53f, 28f), new Vector2(53f, 14f), 4, 1f);
            Line(texture, new Vector2(75f, 28f), new Vector2(75f, 14f), 4, 1f);
            Line(texture, new Vector2(89f, 37f), new Vector2(102f, 21f), 4, 1f);
            for (int i = 0; i < 9; i++)
            {
                float x = 24f + i * 10f;
                float tilt = (i - 4) * 2.4f;
                Sword(texture, new Vector2(x, 58f), new Vector2(x + tilt, 108f), 3, 1f);
            }
            Sword(texture, new Vector2(64f, 48f), new Vector2(64f, 116f), 6, 1f);
        }

        private static void CarapaceRelease(Texture2D texture)
        {
            FilledCircle(texture, 64, 68, 9, 1f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f + Mathf.PI / 6f;
                Vector2 inner = Point(64, 68, 16f, angle);
                Vector2 outer = Point(64, 68, 48f, angle);
                Sword(texture, inner, outer, 5, 1f);
            }
            Ring(texture, 64, 68, 24, 2, 0.72f);
        }

        private static void PeanutChainSword(Texture2D texture)
        {
            Ring(texture, 64, 64, 25, 3, 0.78f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector2 outer = Point(64, 64, 49f, angle);
                Vector2 inner = Point(64, 64, 12f, angle + 0.12f);
                Sword(texture, outer, inner, 4, 1f);
            }
            Line(texture, new Vector2(35f, 93f), new Vector2(94f, 34f), 5, 1f);
        }

        private static void FallenFlowerRoot(Texture2D texture)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector2 petal = Point(64, 35, 17f, angle);
                FilledCircle(texture, Mathf.RoundToInt(petal.x), Mathf.RoundToInt(petal.y), 8, 0.92f);
            }
            FilledCircle(texture, 64, 35, 7, 1f);
            Line(texture, new Vector2(64f, 48f), new Vector2(64f, 82f), 4, 0.78f);
            for (int i = 0; i < 7; i++)
            {
                float x = 28f + i * 12f;
                Sword(texture, new Vector2(64f, 82f), new Vector2(x, 110f), 3, 1f);
            }
            Ring(texture, 64, 84, 28, 2, 0.64f);
        }

        private static void GoldenCoreHeavenSever(Texture2D texture)
        {
            FilledCircle(texture, 64, 46, 23, 0.90f);
            Ring(texture, 64, 46, 28, 3, 1f);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI / 5f;
                Line(texture, Point(64, 46, 30f, angle), Point(64, 46, 45f, angle), 2, 0.82f);
            }
            Sword(texture, new Vector2(64f, 18f), new Vector2(64f, 112f), 7, 1f);
            Line(texture, new Vector2(46f, 87f), new Vector2(82f, 87f), 5, 1f);
        }

        private static void Sword(Texture2D texture, Vector2 from, Vector2 to, int thickness, float alpha)
        {
            Line(texture, from, to, thickness, alpha);
            Vector2 direction = (to - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Line(texture, to, to - direction * 10f + side * 6f, Mathf.Max(2, thickness - 1), alpha);
            Line(texture, to, to - direction * 10f - side * 6f, Mathf.Max(2, thickness - 1), alpha);
        }

        private static Vector2 Point(float centerX, float centerY, float radius, float angle)
        {
            return new Vector2(centerX + Mathf.Cos(angle) * radius, centerY + Mathf.Sin(angle) * radius);
        }

        private static void Ring(Texture2D texture, int centerX, int centerY, int radius, int thickness, float alpha)
        {
            int min = radius - thickness;
            int max = radius + thickness;
            for (int y = centerY - max; y <= centerY + max; y++)
                for (int x = centerX - max; x <= centerX + max; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance >= min && distance <= max) Pixel(texture, x, y, alpha);
                }
        }

        private static void FilledCircle(Texture2D texture, int centerX, int centerY, int radius, float alpha)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
                for (int x = centerX - radius; x <= centerX + radius; x++)
                    if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radiusSquared)
                        Pixel(texture, x, y, alpha);
        }

        private static void Line(Texture2D texture, Vector2 from, Vector2 to, int thickness, float alpha)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) * 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)steps);
                FilledCircle(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), thickness, alpha);
            }
        }

        private static void Pixel(Texture2D texture, int x, int y, float alpha)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height) return;
            Color current = texture.GetPixel(x, y);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(current.a, alpha)));
        }
    }
}
