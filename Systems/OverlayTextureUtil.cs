using System;
using UnityEngine;

namespace Dread.Systems
{
    /// <summary>
    /// Creates small solid or procedural textures without relying on SupportsTextureFormat,
    /// which throws on some Proton/Linux GPU stacks.
    /// </summary>
    internal static class OverlayTextureUtil
    {
        public static Texture2D? CreateSolid(Color color)
        {
            foreach (var formatName in new[] { "ARGB32", "RGBA32", "RGB24", "Alpha8", "RGBA4444" })
            {
                var tex = TryCreateFilled(2, 2, formatName, (x, y) => color);
                if (tex != null)
                    return tex;
            }

            return null;
        }

        public static Texture2D? CreateVignette(int size)
        {
            foreach (var formatName in new[] { "ARGB32", "RGBA32", "RGB24", "Alpha8", "RGBA4444" })
            {
                var tex = TryCreateFilled(size, size, formatName, (x, y) =>
                {
                    float dx = (x / (float)(size - 1)) - 0.5f;
                    float dy = (y / (float)(size - 1)) - 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float alpha = Mathf.Clamp01((dist - 0.3f) / 0.7f);
                    return new Color(0, 0, 0, alpha);
                });
                if (tex != null)
                    return tex;
            }

            return null;
        }

        private static Texture2D? TryCreateFilled(
            int width,
            int height,
            string formatName,
            Func<int, int, Color> pixel)
        {
            try
            {
                var format = (TextureFormat)Enum.Parse(typeof(TextureFormat), formatName);
                if (!IsFormatUsable(format))
                    return null;

                var tex = new Texture2D(width, height, format, false);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                        tex.SetPixel(x, y, pixel(x, y));
                }

                tex.Apply();
                return tex;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsFormatUsable(TextureFormat format)
        {
            try
            {
                var supportsMethod = typeof(SystemInfo).GetMethod(
                    "SupportsTextureFormat",
                    new[] { typeof(TextureFormat) });
                if (supportsMethod == null)
                    return true;

                return (bool)supportsMethod.Invoke(null, new object[] { format })!;
            }
            catch
            {
                // Proton/Linux: SupportsTextureFormat can throw "format is not a valid TextureFormat"
                return true;
            }
        }
    }
}
