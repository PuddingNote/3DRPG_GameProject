using UnityEngine;

// 월드 오브젝트 위에 미니맵 전용 아이콘(SpriteRenderer)을 생성해서 미니맵 카메라가 해당 레이어만 렌더링하도록 하는 방식.
public class MinimapIconWorld : MonoBehaviour
{
    public enum IconKind
    {
        Generic,
        Player,
        Boss,
        Portal,
        NPC
    }

    [Header("Icon")]
    [Tooltip("아이콘 종류(용도 구분). NPC일 때만 퀘스트 색상 변형 로직이 동작")]
    [SerializeField] private IconKind iconKind = IconKind.Generic;
    [Tooltip("미니맵/풀맵에 렌더링할 아이콘 스프라이트")]
    [SerializeField] private Sprite iconSprite;
    [Tooltip("기본 아이콘 색상")]
    [SerializeField] private Color iconColor = Color.white;
    [Tooltip("아이콘 스케일(월드 단위)")]
    [SerializeField] private float iconScale = 1.0f;
    [Tooltip("아이콘 높이 오프셋(월드 Y). 미니맵 카메라에만 보이도록 Minimap 레이어를 사용")]
    [SerializeField] private float heightOffset = 6.0f;

    [Header("Layer")]
    [Tooltip("미니맵 전용 레이어 이름(Minimap). 없으면 minimapLayerIndex를 사용")]
    [SerializeField] private string minimapLayerName = "Minimap";
    [Tooltip("minimapLayerName 레이어가 없을 때 사용할 레이어 인덱스")]
    [SerializeField] private int minimapLayerIndex = 0;

    [Header("Behavior")]
    [Tooltip("아이콘을 미니맵 카메라 기준으로 눕혀서 항상 잘 보이게 할지 여부(기본: 켬)")]
    [SerializeField] private bool faceMinimapCameras = true;
    [Tooltip("아이콘이 오브젝트 회전을 따라가게 할지 여부(예: 방향 표시). 켜면 faceMinimapCameras는 무시")]
    [SerializeField] private bool followOwnerRotation = false;

    [Header("NPC Quest Variant")]
    [Tooltip("NPC 아이콘일 때, 퀘스트 가능 상태면 색상을 변경할지 여부")]
    [SerializeField] private bool tintRedWhenNpcHasQuest = true;
    [Tooltip("NPC 퀘스트 가능 상태일 때의 아이콘 색상")]
    [SerializeField] private Color npcQuestColor = Color.red;
    [Tooltip("NPC 퀘스트 상태 폴링 주기(초). 너무 짧으면 비용이 증가할 수 있음")]
    [SerializeField] private float npcQuestPollInterval = 0.5f;

    private GameObject iconObject;
    private SpriteRenderer spriteRenderer;
    private NPCController cachedNpc;
    private float nextNpcPollTime;
    private bool lastNpcHasQuest;

    private void Awake()
    {
        cachedNpc = GetComponent<NPCController>();
        CreateIconObjectIfNeeded();
        ApplyStaticSettings();
        UpdateNpcQuestTint(force: true);
    }

    private void LateUpdate()
    {
        UpdateIconTransform();
        UpdateNpcQuestTint(force: false);
    }

    // 아이콘 오브젝트 생성
    private void CreateIconObjectIfNeeded()
    {
        if (iconObject != null)
        {
            return;
        }

        iconObject = new GameObject("MinimapIcon");
        iconObject.transform.SetParent(transform, false);
        spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
    }

    // 아이콘 오브젝트 설정
    private void ApplyStaticSettings()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = iconSprite;
        spriteRenderer.color = iconColor;
        spriteRenderer.sortingOrder = 1000;

        int layer = LayerMask.NameToLayer(minimapLayerName);
        if (layer < 0)
        {
            layer = minimapLayerIndex;
        }

        iconObject.layer = layer;
    }

    // 아이콘 오브젝트 위치 업데이트
    private void UpdateIconTransform()
    {
        if (iconObject == null)
        {
            return;
        }

        Vector3 pos = transform.position;
        iconObject.transform.position = new Vector3(pos.x, pos.y + heightOffset, pos.z);
        iconObject.transform.localScale = Vector3.one * iconScale;

        if (followOwnerRotation)
        {
            iconObject.transform.rotation = transform.rotation;
            return;
        }

        if (faceMinimapCameras)
        {
            // 미니맵 카메라는 아래를 보기 때문에, 아이콘은 카메라와 같은 회전으로 맞춰도 항상 정면이 된다.
            // 스프라이트가 위를 향하도록 기본적으로 X=90인 쿼터니언을 사용.
            iconObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    // NPC 퀘스트 색상 업데이트
    private void UpdateNpcQuestTint(bool force)
    {
        if (!tintRedWhenNpcHasQuest)
        {
            return;
        }

        if (iconKind != IconKind.NPC)
        {
            return;
        }

        if (cachedNpc == null)
        {
            return;
        }

        if (!force && Time.unscaledTime < nextNpcPollTime)
        {
            return;
        }

        nextNpcPollTime = Time.unscaledTime + npcQuestPollInterval;

        bool hasQuest = cachedNpc.HasAvailableQuestOnMinimap();
        if (!force && hasQuest == lastNpcHasQuest)
        {
            return;
        }

        lastNpcHasQuest = hasQuest;

        if (spriteRenderer == null)
        {
            return;
        }

        if (hasQuest)
        {
            spriteRenderer.color = npcQuestColor;
        }
        else
        {
            spriteRenderer.color = iconColor;
        }
    }
}
