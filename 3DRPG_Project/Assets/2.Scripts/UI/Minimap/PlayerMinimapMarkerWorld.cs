using UnityEngine;

/// <summary>
/// 플레이어 전용 월드 미니맵 마커.
/// - RenderTexture 기반 미니맵/풀맵에서 동일하게 보이도록, 월드에 스프라이트 마커를 배치.
/// - 미니맵에서는 항상 중앙에 보이도록 설정.
/// </summary>
public class PlayerMinimapMarkerWorld : MonoBehaviour
{
    [Header("Sprites")]
    [Tooltip("플레이어 위치 표시용 스프라이트(원형). 회전/카메라 회전의 영향을 받지 않습니다.")]
    [SerializeField] private Sprite playerSprite;
    [Tooltip("플레이어 방향 표시용 스프라이트(화살표)")]
    [SerializeField] private Sprite playerArrowSprite;
    [Tooltip("카메라 시야 표시용 스프라이트(부채꼴)")]
    [SerializeField] private Sprite cameraConeSprite;

    [Header("Appearance")]
    [Tooltip("플레이어 위치 스프라이트 색상")]
    [SerializeField] private Color playerColor = Color.white;
    [Tooltip("플레이어 방향 스프라이트 색상")]
    [SerializeField] private Color playerArrowColor = Color.white;
    [Tooltip("카메라 시야 콘 스프라이트 색상(알파 포함)")]
    [SerializeField] private Color cameraConeColor = new Color(255f, 255f, 255f, 0.45f);
    [Tooltip("플레이어 위치 스프라이트 스케일(월드 단위)")]
    [SerializeField] private float playerScale = 1.0f;
    [Tooltip("플레이어 방향 스프라이트 스케일(월드 단위)")]
    [SerializeField] private float playerArrowScale = 1.0f;
    [Tooltip("카메라 시야 콘 스프라이트 스케일(월드 단위)")]
    [SerializeField] private float cameraConeScale = 1.0f;
    [Tooltip("마커 높이 오프셋(월드 Y). Minimap 레이어를 메인 카메라에서 제외하면 게임 화면에서는 보이지 않습니다.")]
    [SerializeField] private float heightOffset = 6.0f;

    [Header("Rotation")]
    [Tooltip("플레이어 방향 마커 회전 보정각(도). 스프라이트의 '정면'이 +Z(북쪽) 기준이 아닐 때 사용")]
    [SerializeField] private float playerYawOffsetDegrees = 0f;

    [Tooltip("카메라 콘 회전 보정각(도). 스프라이트의 '정면'이 +Z(북쪽) 기준이 아닐 때 사용")]
    [SerializeField] private float cameraYawOffsetDegrees = 0f;

    [Header("Layer")]
    [Tooltip("미니맵 전용 레이어 이름(권장: Minimap). 없으면 minimapLayerIndex를 사용")]
    [SerializeField] private string minimapLayerName = "Minimap";
    [Tooltip("minimapLayerName 레이어가 없을 때 사용할 레이어 인덱스")]
    [SerializeField] private int minimapLayerIndex = 0;

    [Header("Sorting")]
    [Tooltip("플레이어 위치(원형) 기본 정렬 순서(SpriteRenderer.sortingOrder)")]
    [SerializeField] private int playerBaseSortingOrder = 1000;
    [Tooltip("플레이어 화살표 기본 정렬 순서(SpriteRenderer.sortingOrder)")]
    [SerializeField] private int baseSortingOrder = 1000;
    [Tooltip("카메라 콘 정렬 순서 오프셋")]
    [SerializeField] private int coneSortingOffset = -1;

    private Transform baseTr;
    private SpriteRenderer baseSr;
    private Transform arrowTr;
    private SpriteRenderer arrowSr;
    private Transform coneTr;
    private SpriteRenderer coneSr;

    private void Awake()
    {
        EnsureMarkers();
        ApplyStaticSettings();
    }

    private void LateUpdate()
    {
        UpdateTransforms();
        UpdateRotations();
    }

    // 마커 오브젝트 생성
    private void EnsureMarkers()
    {
        if (baseTr == null)
        {
            GameObject go = new GameObject("PlayerMinimapBase");
            go.transform.SetParent(transform, false);
            baseTr = go.transform;
            baseSr = go.AddComponent<SpriteRenderer>();
        }

        if (arrowTr == null)
        {
            GameObject go = new GameObject("PlayerMinimapArrow");
            go.transform.SetParent(transform, false);
            arrowTr = go.transform;
            arrowSr = go.AddComponent<SpriteRenderer>();
        }

        if (coneTr == null)
        {
            GameObject go = new GameObject("PlayerMinimapCameraCone");
            go.transform.SetParent(transform, false);
            coneTr = go.transform;
            coneSr = go.AddComponent<SpriteRenderer>();
        }
    }

    // 마커 오브젝트 설정
    private void ApplyStaticSettings()
    {
        int layer = LayerMask.NameToLayer(minimapLayerName);
        if (layer < 0)
        {
            layer = minimapLayerIndex;
        }

        if (baseSr != null)
        {
            baseSr.sprite = playerSprite;
            baseSr.color = playerColor;
            baseSr.sortingOrder = playerBaseSortingOrder;
            baseSr.gameObject.layer = layer;
        }

        if (arrowSr != null)
        {
            arrowSr.sprite = playerArrowSprite;
            arrowSr.color = playerArrowColor;
            arrowSr.sortingOrder = baseSortingOrder;
            arrowSr.gameObject.layer = layer;
        }

        if (coneSr != null)
        {
            coneSr.sprite = cameraConeSprite;
            coneSr.color = cameraConeColor;
            coneSr.sortingOrder = baseSortingOrder + coneSortingOffset;
            coneSr.gameObject.layer = layer;
        }
    }

    // 마커 오브젝트 위치 업데이트
    private void UpdateTransforms()
    {
        Vector3 pos = transform.position;
        Vector3 markerPos = new Vector3(pos.x, pos.y + heightOffset, pos.z);

        if (baseTr != null)
        {
            baseTr.position = markerPos;
            baseTr.localScale = Vector3.one * playerScale;
        }

        if (arrowTr != null)
        {
            arrowTr.position = markerPos;
            arrowTr.localScale = Vector3.one * playerArrowScale;
        }

        if (coneTr != null)
        {
            coneTr.position = markerPos;
            coneTr.localScale = Vector3.one * cameraConeScale;
        }
    }

    // 마커 오브젝트 회전 업데이트
    private void UpdateRotations()
    {
        // 플레이어 위치(원형)는 회전 영향이 없지만, 미니맵 카메라가 위에서 보기 때문에 X=90으로 눕혀둡니다.
        if (baseTr != null)
        {
            baseTr.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // 스프라이트를 XZ 평면에 눕힌 뒤, Yaw로 방향을 표현
        float playerYaw = transform.eulerAngles.y + playerYawOffsetDegrees;
        if (arrowTr != null)
        {
            arrowTr.rotation = Quaternion.Euler(90f, playerYaw, 0f);
        }

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 forward = mainCam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.Normalize();
                float camYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + cameraYawOffsetDegrees;
                if (coneTr != null)
                {
                    coneTr.rotation = Quaternion.Euler(90f, camYaw, 0f);
                }
            }
        }
    }
}
