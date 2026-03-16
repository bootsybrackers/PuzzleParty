using UnityEngine;

namespace PuzzleParty.Board
{
    public static class TileFactory
    {
        public const int TEXTURE_WIDTH = 768;
        public const int TEXTURE_HEIGHT = 1344;

        public static string GetTileKey(int row, int col) => $"{row}_{col}";

        public static GameObject CreateTile(Texture2D source, int tileCol, int tileRow, int displayCol, int displayRow,
            int pixelWidth, int pixelHeight, float desiredTileWorldWidth, float desiredTileWorldHeight,
            int totalCols, int totalRows, Transform parent)
        {
            int startX = (tileCol - 1) * pixelWidth;
            int startY = TEXTURE_HEIGHT - (tileRow * pixelHeight);

            Texture2D part = CopyRegion(source, startX, startY, pixelWidth, pixelHeight);

            float pixelsPerUnit = pixelWidth / desiredTileWorldWidth;
            Sprite sprite = Sprite.Create(
                part,
                new Rect(0, 0, part.width, part.height),
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

        public static Texture2D CopyRegion(Texture2D source, int x, int y, int width, int height)
        {
            Texture2D newTex = new Texture2D(width, height, source.format, false);
            newTex.SetPixels(source.GetPixels(x, y, width, height));
            newTex.Apply();
            return newTex;
        }
    }
}
