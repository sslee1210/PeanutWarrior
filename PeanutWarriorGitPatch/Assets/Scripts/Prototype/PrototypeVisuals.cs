using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public static class PrototypeVisuals
    {
        private static Sprite whiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite != null) return whiteSprite;
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                return whiteSprite;
            }
        }
    }
}
