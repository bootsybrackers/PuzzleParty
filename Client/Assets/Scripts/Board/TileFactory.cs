using UnityEngine;

namespace PuzzleParty.Board
{
    public static class TileFactory
    {
        public const int TEXTURE_WIDTH = 768;
        public const int TEXTURE_HEIGHT = 1152; // 768 * 1.5 to match 1024x1536 (2:3) source images

        public static string GetTileKey(int row, int col) => $"{row}_{col}";

        public static GameObject CreateTile(Texture2D source, int tileCol, int tileRow, int displayCol, int displayRow,
            int pixelWidth, int pixelHeight, float desiredTileWorldWidth, float desiredTileWorldHeight,
            int totalCols, int totalRows, Transform parent)
        {
            int startX = (tileCol - 1) * pixelWidth;
            int startY = TEXTURE_HEIGHT - (tileRow * pixelHeight);

            float pixelsPerUnit = pixelWidth / desiredTileWorldWidth;

            // When TEXTURE_HEIGHT / rows is not an exact integer (e.g. 1152/5 = 230.4
            // truncated to 268), each tile is 1 pixel too short. The resulting
            // sub-pixel gap between rows renders as a visible black line in Unity 6000.4.
            // Fix: expand the tile texture by 1 row (edge-clamped copy of the top edge)
            // so the sprite rect can cover the full desiredTileWorldHeight. The resulting
            // ~0.2px overlap between adjacent tiles is imperceptible.
            float desiredRectHeight = desiredTileWorldHeight * pixelsPerUnit;
            int textureHeight = (desiredRectHeight > pixelHeight + 0.01f) ? pixelHeight + 1 : pixelHeight;

            Texture2D part = CopyRegion(source, startX, startY, pixelWidth, pixelHeight, textureHeight);
            part.wrapMode = TextureWrapMode.Clamp;

            Sprite sprite = Sprite.Create(
                part,
                new Rect(0, 0, pixelWidth, textureHeight),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);

            GameObject go = new GameObject($"Tile_{tileRow - 1}_{tileCol - 1}");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;

            go.transform.SetParent(parent);
            go.transform.localPosition = GetTileLocalPosition(
                displayCol, displayRow, totalCols, totalRows,
                desiredTileWorldWidth, desiredTileWorldHeight, parent.lossyScale);

            return go;
        }

        public static Vector3 GetTileLocalPosition(int col, int row, int totalCols, int totalRows,
            float desiredTileWorldWidth, float desiredTileWorldHeight, Vector3 parentLossyScale)
        {
            float boardWidth = totalCols * desiredTileWorldWidth;
            float boardHeight = totalRows * desiredTileWorldHeight;

            float startX = -boardWidth / 2f + desiredTileWorldWidth / 2f;
            float startY = boardHeight / 2f - desiredTileWorldHeight / 2f;

            float x = startX + ((col - 1) * desiredTileWorldWidth);
            float y = startY - ((row - 1) * desiredTileWorldHeight);

            return new Vector3(x / parentLossyScale.x, y / parentLossyScale.y, -10f);
        }

        public static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D newTex = new Texture2D(newWidth, newHeight, source.format, false);
            newTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            newTex.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return newTex;
        }

        public static Texture2D CopyRegion(Texture2D source, int x, int y, int width, int height, int textureHeight = -1)
        {
            if (textureHeight < 0) textureHeight = height;
            Texture2D newTex = new Texture2D(width, textureHeight, TextureFormat.RGBA32, false);
            Color[] pixels = source.GetPixels(x, y, width, height);
            newTex.SetPixels(0, 0, width, height, pixels);
            if (textureHeight > height)
            {
                // Fill extra rows with the top edge row of the copied region (edge clamp)
                Color[] topRow = source.GetPixels(x, y + height - 1, width, 1);
                for (int row = height; row < textureHeight; row++)
                    newTex.SetPixels(0, row, width, 1, topRow);
            }
            newTex.Apply();
            return newTex;
        }
    }
}
