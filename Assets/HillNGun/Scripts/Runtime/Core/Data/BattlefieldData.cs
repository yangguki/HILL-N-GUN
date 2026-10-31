using UnityEngine;

namespace HillNGun.Data
{
    [CreateAssetMenu(fileName = "BattlefieldData", menuName = "HillNGun/Data/Battlefield")]
    public class BattlefieldData : ScriptableObject
    {
        [Header("Field Size")]
        [Tooltip("전장 크기 (타일)")]
        public Vector2 size = new Vector2(24f, 18f);

        [Header("Row Positions (Y coordinates in tiles)")]
        [Tooltip("우리 팀 엄폐 줄 Y 좌표 배열")]
        public float[] ourRowsY = { 2f, 4.5f, 7f };

        [Tooltip("상대 팀 엄폐 줄 Y 좌표 배열")]
        public float[] enemyRowsY = { 11f, 13.5f, 16f };

        [Header("Cover Placement")]
        [Tooltip("줄에서 상대 쪽으로의 최소 오프셋 (타일)")]
        public float coverOffsetMin = 0.35f;

        [Tooltip("줄에서 상대 쪽으로의 최대 오프셋 (타일)")]
        public float coverOffsetMax = 0.85f;

        [Header("Starting Positions")]
        [Tooltip("시작 줄 인덱스 (0=뒤, 1=가운데, 2=앞)")]
        public int startRowIndex = 1;

        [Tooltip("시작 X 좌표 배열 (3인 기준)")]
        public float[] startX = { 6f, 12f, 18f };

        [Header("Rendering")]
        [Tooltip("기준 해상도 (픽셀)")]
        public Vector2Int referenceResolution = new Vector2Int(1920, 1080);

        [Tooltip("X축 타일당 픽셀")]
        public float pxPerTileX = 72f;

        [Tooltip("Y축 타일당 픽셀")]
        public float pxPerTileY = 50f;

        [Tooltip("화면 아래쪽 원근 스케일")]
        public float perspectiveScaleBottom = 1.0f;

        [Tooltip("화면 위쪽 원근 스케일")]
        public float perspectiveScaleTop = 0.8f;

        [Tooltip("HUD 상단 높이 (픽셀)")]
        public int hudTopPx = 100;

        [Header("Boundaries")]
        [Tooltip("X축 이동 가능 최소값")]
        public float boundaryMinX = 0.5f;

        [Tooltip("X축 이동 가능 최대값")]
        public float boundaryMaxX = 23.5f;

        [Header("Cover Layout")]
        public CoverPlacement[] covers;

        [Tooltip("PvE에서 아군 쪽 엄폐물 생성 여부")]
        public bool spawnCoversInPvE = true;

        [Tooltip("PvP에서 엄폐물 배치를 상대 진영에 대칭 복제할지 여부")]
        public bool mirrorCoversToEnemySideInPvP = true;

        public int OurRowCount => ourRowsY?.Length ?? 0;
        public int EnemyRowCount => enemyRowsY?.Length ?? 0;

        public float GetOurRowY(int rowIndex)
        {
            if (ourRowsY == null || ourRowsY.Length == 0) return 4.5f;
            rowIndex = Mathf.Clamp(rowIndex, 0, ourRowsY.Length - 1);
            return ourRowsY[rowIndex];
        }

        public float GetEnemyRowY(int rowIndex)
        {
            if (enemyRowsY == null || enemyRowsY.Length == 0) return 13.5f;
            rowIndex = Mathf.Clamp(rowIndex, 0, enemyRowsY.Length - 1);
            return enemyRowsY[rowIndex];
        }

        public float GetPerspectiveScale(float yTile)
        {
            float t = Mathf.InverseLerp(0f, size.y, yTile);
            return Mathf.Lerp(perspectiveScaleBottom, perspectiveScaleTop, t);
        }

        public Vector2 TileToScreen(Vector2 tilePos)
        {
            float screenX = tilePos.x * pxPerTileX;
            float screenY = (size.y - tilePos.y) * pxPerTileY + hudTopPx;
            return new Vector2(screenX, screenY);
        }

        public Vector2 ScreenToTile(Vector2 screenPos)
        {
            float tileX = screenPos.x / pxPerTileX;
            float tileY = size.y - (screenPos.y - hudTopPx) / pxPerTileY;
            return new Vector2(tileX, tileY);
        }
    }

    [System.Serializable]
    public class CoverPlacement
    {
        public string coverId;
        public int rowIndex;
        public float xPosition;
    }
}
