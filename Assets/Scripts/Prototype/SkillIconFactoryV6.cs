using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public static class SkillIconFactoryV6
    {
        private const int Size = 128;

        public static Sprite Create(int index)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.name = "PeanutSkillIcon" + index;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    texture.SetPixel(x, y, clear);

            Ring(texture, 64, 64, 54, 3, 0.42f);
            switch (index)
            {
                case 0: Whirlwind(texture); break;
                case 1: Barrage(texture); break;
                case 2: TrackingDance(texture); break;
                case 3: HeavenEarthCut(texture); break;
                case 4: ComboSlash(texture); break;
                case 5: VitalCut(texture); break;
                case 6: ElementMark(texture); break;
                default: DimensionEnd(texture); break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 128f);
        }

        public static Color ColorFor(int index)
        {
            Color[] colors =
            {
                new Color(0.15f, 0.58f, 0.34f),
                new Color(0.14f, 0.48f, 0.72f),
                new Color(0.22f, 0.64f, 0.62f),
                new Color(0.74f, 0.56f, 0.12f),
                new Color(0.82f, 0.24f, 0.16f),
                new Color(0.72f, 0.18f, 0.24f),
                new Color(0.52f, 0.30f, 0.80f),
                new Color(0.28f, 0.18f, 0.48f)
            };
            return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
        }

        private static void Whirlwind(Texture2D texture)
        {
            for (int arm = 0; arm < 3; arm++)
            {
                float offset = arm * Mathf.PI * 2f / 3f;
                Vector2 previous = Vector2.zero;
                for (int step = 0; step <= 34; step++)
                {
                    float t = step / 34f;
                    float angle = offset + t * Mathf.PI * 1.55f;
                    float radius = 8f + t * 38f;
                    Vector2 point = new Vector2(64f + Mathf.Cos(angle) * radius, 64f + Mathf.Sin(angle) * radius);
                    if (step > 0) Line(texture, previous, point, 3, 1f);
                    previous = point;
                }
            }
            FilledCircle(texture, 64, 64, 6, 1f);
        }

        private static void Barrage(Texture2D texture)
        {
            for (int i = 0; i < 5; i++)
            {
                float shift = (i - 2) * 14f;
                Line(texture, new Vector2(30f + shift, 92f), new Vector2(76f + shift, 36f), 4, 1f);
                Line(texture, new Vector2(71f + shift, 36f), new Vector2(83f + shift, 31f), 2, 0.85f);
            }
        }

        private static void TrackingDance(Texture2D texture)
        {
            Ring(texture, 64, 64, 25, 3, 0.95f);
            Ring(texture, 64, 64, 10, 3, 0.95f);
            Line(texture, new Vector2(24f, 90f), new Vector2(91f, 34f), 5, 1f);
            Line(texture, new Vector2(91f, 34f), new Vector2(82f, 35f), 3, 1f);
            Line(texture, new Vector2(91f, 34f), new Vector2(89f, 44f), 3, 1f);
        }

        private static void HeavenEarthCut(Texture2D texture)
        {
            Line(texture, new Vector2(64f, 22f), new Vector2(64f, 106f), 6, 1f);
            Line(texture, new Vector2(24f, 64f), new Vector2(104f, 64f), 6, 1f);
            Line(texture, new Vector2(42f, 28f), new Vector2(86f, 100f), 2, 0.65f);
            Line(texture, new Vector2(86f, 28f), new Vector2(42f, 100f), 2, 0.65f);
        }

        private static void ComboSlash(Texture2D texture)
        {
            for (int i = 0; i < 4; i++)
            {
                float shift = i * 13f;
                Line(texture, new Vector2(26f + shift, 102f), new Vector2(66f + shift, 28f), 5, 1f);
            }
            Line(texture, new Vector2(30f, 44f), new Vector2(97f, 84f), 3, 0.72f);
        }

        private static void VitalCut(Texture2D texture)
        {
            Ring(texture, 64, 64, 30, 3, 0.95f);
            Ring(texture, 64, 64, 12, 3, 0.95f);
            Line(texture, new Vector2(64f, 20f), new Vector2(64f, 42f), 3, 0.9f);
            Line(texture, new Vector2(64f, 86f), new Vector2(64f, 108f), 3, 0.9f);
            Line(texture, new Vector2(20f, 64f), new Vector2(42f, 64f), 3, 0.9f);
            Line(texture, new Vector2(86f, 64f), new Vector2(108f, 64f), 3, 0.9f);
            Line(texture, new Vector2(31f, 96f), new Vector2(97f, 31f), 5, 1f);
        }

        private static void ElementMark(Texture2D texture)
        {
            Line(texture, new Vector2(64f, 22f), new Vector2(102f, 64f), 5, 1f);
            Line(texture, new Vector2(102f, 64f), new Vector2(64f, 106f), 5, 1f);
            Line(texture, new Vector2(64f, 106f), new Vector2(26f, 64f), 5, 1f);
            Line(texture, new Vector2(26f, 64f), new Vector2(64f, 22f), 5, 1f);
            FilledCircle(texture, 64, 38, 7, 1f);
            FilledCircle(texture, 88, 64, 7, 1f);
            FilledCircle(texture, 64, 90, 7, 1f);
            FilledCircle(texture, 40, 64, 7, 1f);
            Ring(texture, 64, 64, 13, 3, 1f);
        }

        private static void DimensionEnd(Texture2D texture)
        {
            Ring(texture, 64, 64, 37, 5, 1f);
            Ring(texture, 64, 64, 21, 3, 0.82f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector2 inner = new Vector2(64f + Mathf.Cos(angle) * 12f, 64f + Mathf.Sin(angle) * 12f);
                Vector2 outer = new Vector2(64f + Mathf.Cos(angle + 0.18f) * 48f, 64f + Mathf.Sin(angle + 0.18f) * 48f);
                Line(texture, inner, outer, 3, 1f);
            }
            FilledCircle(texture, 64, 64, 7, 1f);
        }

        private static void Ring(Texture2D texture, int centerX, int centerY, int radius, int thickness, float alpha)
        {
            int min = radius - thickness;
            int max = radius + thickness;
            for (int y = centerY - max; y <= centerY + max; y++)
            {
                for (int x = centerX - max; x <= centerX + max; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance >= min && distance <= max) Pixel(texture, x, y, alpha);
                }
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
