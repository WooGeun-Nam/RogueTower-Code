using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; // List를 사용하기 위해 추가

// [추가] 타일셋을 관리하기 위한 새로운 구조체를 정의합니다.
[System.Serializable]
public struct TileSet
{
    public string name; // 타일셋의 이름
    private string bgmClipName; // 이 타일셋(맵)에서 재생할 BGM 오디오 클립의 이름
    public string BgmClipName { get; private set; }
    
    [Header("Path Tiles")]
    public TileBase horizontalPathTile;    // 가로 길
    public TileBase verticalPathTile;      // 세로 길
    public TileBase cornerTopRightTile;    // ┗ 
    public TileBase cornerTopLeftTile;     // ┛
    public TileBase cornerBottomRightTile; // ┏
    public TileBase cornerBottomLeftTile;  // ┓
    
    [Header("Building Tiles")]
    public TileBase startCastleTile; // 시작 성 타일
    public TileBase endCastleTile;   // 도착 성 타일
    
    [Header("Wall Sprites")]
    public Sprite baseWallSprite; // 기본 벽 (단일)
    public Sprite[] decorativeWallSprites; // 장식 벽 (배열)
    
    // 구조체의 필드 값을 초기화하고 BGM 이름을 생성
    public void Initialize()
    {
        // 이름 필드가 비어있지 않다면, "BGM_" 접두사와 이름을 조합하여 BGM 클립 이름을 만듭니다.
        if (!string.IsNullOrEmpty(name))
        {
            BgmClipName = "BGM_Game_" + name;
        }
        else
        {
            BgmClipName = string.Empty;
        }
    }
}

/// <summary>
/// 랜덤 경로 데이터를 기반으로 타일맵에 맵을 생성하는 클래스
/// </summary>
public class MapGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Transform groundParent;   // 타일 벽(장애물)이 생성될 부모 Transform
    [SerializeField] private Tilemap pathTilemap;      // 경로 타일맵
    [SerializeField] private Tilemap buildingTilemap;  // 건물(성) 타일맵

    [Header("Prefab References")]
    [SerializeField] private GameObject tileWallPrefab; // 타일 벽 프리팹
    
    [Header("Tile Sets")]
    [SerializeField] private TileSet[] tileSets; // 사용할 모든 타일셋 테마 배열
    
    [Range(0, 1)]
    [SerializeField] private float decorativeTileChance = 0.05f; // 장식 타일이 나타날 확률 (5%)
    
    private TileSet selectedTileSet; // 이번 게임에서 선택된 타일셋
    
    // 맵의 경계를 정의하는 상수
    private readonly int startX = -(GameConstants.MAP_WIDTH / 2);
    private readonly int endX = (GameConstants.MAP_WIDTH / 2) - 1;
    private readonly int startY = -(GameConstants.MAP_HEIGHT / 2);
    private readonly int endY = (GameConstants.MAP_HEIGHT / 2) - 1;
    
    /// <summary>
    /// 맵 생성을 시작하는 메인 메소드
    /// </summary>
    /// <param name="waypoints">생성할 경로의 웨이포인트 배열</param>
    /// <param name="isLeftToRight">경로의 방향 (왼쪽에서 오른쪽으로)</param>
    public void GenerateMap(Vector2Int[] waypoints, bool isLeftToRight)
    {
        if (tileSets == null || tileSets.Length == 0)
        {
            Debug.LogError("MapGenerator에 타일셋이 설정되지 않았습니다!");
            return;
        }

        // 게임 시작 시 사용할 타일셋을 무작위로 하나 선택하여 저장합니다.
        if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 0)
        {
            // 튜토리얼 중에는 항상 첫 번째 타일셋을 사용합니다.
            selectedTileSet = tileSets[0];
        }
        else
        {
            // 1. 현재 게임 난이도를 가져옵니다.
            int difficultyLevel = DifficultyManager.Instance.CurrentDifficultyLevel;

            // 2. 난이도에 따라 잠금 해제된 맵의 개수를 결정합니다.
            int unlockedMapCount;
            if (difficultyLevel < 25)
            {
                unlockedMapCount = 1;
            }
            else if (difficultyLevel < 50)
            {
                unlockedMapCount = 2;
            }
            else if (difficultyLevel < 75)
            {
                unlockedMapCount = 3;
            }
            else // 75 이상
            {
                unlockedMapCount = 4;
            }

            // 3. (안전 장치) 실제 설정된 타일셋 배열의 크기를 넘지 않도록 합니다.
            // 예를 들어, 타일셋을 3개만 설정했다면 4개가 선택되는 오류를 방지합니다.
            unlockedMapCount = Mathf.Min(unlockedMapCount, tileSets.Length);

            // 4. 잠금 해제된 맵 중에서만 무작위로 하나를 선택합니다.
            int randomIndex = Random.Range(0, unlockedMapCount);
            selectedTileSet = tileSets[randomIndex];
        }
        
        selectedTileSet.Initialize();
        
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(selectedTileSet.BgmClipName))
        {
            SoundManager.Instance.PlayBGM(selectedTileSet.BgmClipName, true);
        }

        ClearAllTilemaps();
        ClearTileWalls();
        PlacePathTiles(waypoints); // 직선 및 코너 경로 타일 배치
        PlaceCastles(waypoints[0], waypoints[waypoints.Length - 1]);
        FillWithTileWalls();
    }

    /// <summary>
    /// 모든 타일맵을 초기화합니다.
    /// </summary>
    private void ClearAllTilemaps()
    {
        pathTilemap.ClearAllTiles();
        buildingTilemap.ClearAllTiles();
    }

    /// <summary>
    /// 기존에 생성된 타일 벽을 모두 제거합니다.
    /// </summary>
    private void ClearTileWalls()
    {
        foreach (Transform child in groundParent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 웨이포인트를 따라 직선 및 코너 경로 타일을 배치합니다.
    /// </summary>
    /// <param name="waypoints">경로 웨이포인트 배열</param>
    private void PlacePathTiles(Vector2Int[] waypoints)
    {
        // 1. Fill straight paths first
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2Int currentPoint = waypoints[i];
            Vector2Int nextPoint = waypoints[i + 1];

            if (currentPoint.x == nextPoint.x) // Vertical
            {
                int startY = Mathf.Min(currentPoint.y, nextPoint.y);
                int endY = Mathf.Max(currentPoint.y, nextPoint.y);
                for (int y = startY; y <= endY; y++)
                {
                    pathTilemap.SetTile(new Vector3Int(currentPoint.x, y, 0), selectedTileSet.verticalPathTile);
                }
            }
            else if (currentPoint.y == nextPoint.y) // Horizontal
            {
                int startX = Mathf.Min(currentPoint.x, nextPoint.x);
                int endX = Mathf.Max(currentPoint.x, nextPoint.x);
                for (int x = startX; x <= endX; x++)
                {
                    pathTilemap.SetTile(new Vector3Int(x, currentPoint.y, 0), selectedTileSet.horizontalPathTile);
                }
            }
        }

        // 2. Place corner tiles, overwriting the straight tiles at the corner point
        for (int i = 1; i < waypoints.Length - 1; i++)
        {
            Vector2Int previousPoint = waypoints[i - 1];
            Vector2Int currentPoint = waypoints[i];
            Vector2Int nextPoint = waypoints[i + 1];

            Vector2Int dirTo = currentPoint - previousPoint;
            Vector2Int dirFrom = nextPoint - currentPoint;

            TileBase cornerTile = null;

            // ┏ (cornerBottomRightTile): Below -> Right or Right -> Below
            if ((dirTo.y > 0 && dirFrom.x > 0) || (dirTo.x < 0 && dirFrom.y < 0))
            {
                cornerTile = selectedTileSet.cornerBottomRightTile;
            }
            // ┓ (cornerBottomLeftTile): Below -> Left or Left -> Below
            else if ((dirTo.y > 0 && dirFrom.x < 0) || (dirTo.x > 0 && dirFrom.y < 0))
            {
                cornerTile = selectedTileSet.cornerBottomLeftTile;
            }
            // ┗ (cornerTopRightTile): Above -> Right or Right -> Above
            else if ((dirTo.y < 0 && dirFrom.x > 0) || (dirTo.x < 0 && dirFrom.y > 0))
            {
                cornerTile = selectedTileSet.cornerTopRightTile;
            }
            // ┛ (cornerTopLeftTile): Above -> Left or Left -> Above
            else if ((dirTo.y < 0 && dirFrom.x < 0) || (dirTo.x > 0 && dirFrom.y > 0))
            {
                cornerTile = selectedTileSet.cornerTopLeftTile;
            }

            if (cornerTile != null)
            {
                pathTilemap.SetTile(new Vector3Int(currentPoint.x, currentPoint.y, 0), cornerTile);
            }
        }
    }
    
    /// <summary>
    /// 시작점과 도착점에 성 타일을 배치합니다.
    /// </summary>
    private void PlaceCastles(Vector2Int start, Vector2Int end)
    {
        Vector3Int startCell = new Vector3Int(start.x, start.y, 0);
        Vector3Int endCell = new Vector3Int(end.x, end.y, 0);
        
        buildingTilemap.SetTile(new Vector3Int(start.x, start.y, 0), selectedTileSet.startCastleTile);
        buildingTilemap.SetTile(new Vector3Int(end.x, end.y, 0), selectedTileSet.endCastleTile);
        
        // [추가] 타일의 월드 좌표를 GameManager에 저장합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.startCastlePosition = buildingTilemap.GetCellCenterWorld(startCell);
            GameManager.Instance.endCastlePosition = buildingTilemap.GetCellCenterWorld(endCell);
        }
    }

    /// <summary>
    /// 경로가 아닌 모든 공간을 타일 벽으로 채웁니다.
    /// </summary>
    private void FillWithTileWalls()
    {
        int border = 2;
        
        for (int x = startX-border; x <= endX+border; x++)
        {
            for (int y = startY-border; y <= endY+border; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                if (!pathTilemap.HasTile(cellPosition) && !buildingTilemap.HasTile(cellPosition))
                {
                    Vector3 position = new Vector3(x + 0.5f, y + 0.5f, 0);
                    GameObject tileWall = Instantiate(tileWallPrefab, position, Quaternion.identity, groundParent);
                
                    SpriteRenderer sr = tileWall.GetComponent<SpriteRenderer>();
                    
                    if (sr != null)
                    {
                        // 1. 현재 선택된 타일셋에서 벽 스프라이트 정보를 가져옵니다.
                        Sprite[] decoratives = selectedTileSet.decorativeWallSprites;
                        Sprite baseSprite = selectedTileSet.baseWallSprite; // 배열이 아닌 단일 스프라이트

                        // 2. 장식 타일 배열이 비어있지 않고, 설정된 확률을 통과했는지 확인합니다.
                        if (decoratives != null && decoratives.Length > 0 && Random.value < decorativeTileChance)
                        {
                            // 장식 타일 중 하나를 무작위로 선택하여 적용합니다.
                            sr.sprite = decoratives[Random.Range(0, decoratives.Length)];
                        }
                        // 3. 그렇지 않고, 기본 벽 스프라이트가 설정되어 있다면 그것을 적용합니다.
                        else if (baseSprite != null)
                        {
                            // 단일 기본 스프라이트를 직접 적용합니다.
                            sr.sprite = baseSprite;
                        }
                    }
                }
            }
        }
    }
}
