using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Map Canvas UI를 패널 단위로 제어하는 단일 스크립트.
public class MapCanvasUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private MinimapSystem minimapSystem;
    [SerializeField] private GameObject minimapPanel;
    [SerializeField] private RawImage minimapRawImage;
    [SerializeField] private GameObject fullmapPanel;
    [SerializeField] private RawImage fullmapRawImage;
    [SerializeField] private Button closeButton;

    // [Header("Auto Find Settings")]
    // [SerializeField] private bool autoFindChildrenByName = true;
    // [SerializeField] private string minimapRawImageName = "MinimapRawImage";
    // [SerializeField] private string fullmapRawImageName = "FullmapRawImage";
    // [SerializeField] private string closeButtonName = "Close Button";

    private EventTrigger minimapTrigger;
    private EventTrigger fullmapTrigger;
    private bool isDragging;

    private void Awake()
    {
        if (minimapSystem == null)
        {
            minimapSystem = FindFirstObjectByType<MinimapSystem>();
        }

        //if (autoFindChildrenByName)
        //{
        //    AutoFindReferences();
        //}

        WireEvents();
    }

    //private void AutoFindReferences()
    //{
    //    if (minimapRawImage == null)
    //    {
    //        minimapRawImage = FindInChildrenByName<RawImage>(minimapRawImageName);
    //    }
    //
    //    if (fullmapRawImage == null)
    //    {
    //        fullmapRawImage = FindInChildrenByName<RawImage>(fullmapRawImageName);
    //    }
    //
    //    if (closeButton == null)
    //    {
    //        closeButton = FindInChildrenByName<Button>(closeButtonName);
    //    }
    //
    //    if (minimapPanel == null)
    //    {
    //        // RawImage의 상위 패널을 대략적으로 찾음(원하면 인스펙터에서 지정)
    //        if (minimapRawImage != null)
    //        {
    //            minimapPanel = minimapRawImage.transform.root != null ? minimapRawImage.transform.root.gameObject : null;
    //        }
    //    }
    //
    //    if (fullmapPanel == null)
    //    {
    //        if (fullmapRawImage != null)
    //        {
    //            fullmapPanel = fullmapRawImage.transform.root != null ? fullmapRawImage.transform.root.gameObject : null;
    //        }
    //    }
    //}

    // 이벤트 연결
    private void WireEvents()
    {
        if (minimapSystem == null)
        {
            return;
        }

        // Close 버튼
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        // 미니맵 클릭 -> 풀맵 토글
        if (minimapRawImage != null)
        {
            minimapTrigger = EnsureEventTrigger(minimapRawImage.gameObject);
            ReplaceTrigger(minimapTrigger, EventTriggerType.PointerClick, HandleMinimapClick);
        }

        // 풀맵 드래그 이벤트 연결
        if (fullmapRawImage != null)
        {
            fullmapTrigger = EnsureEventTrigger(fullmapRawImage.gameObject);
            ReplaceTrigger(fullmapTrigger, EventTriggerType.PointerDown, HandleFullmapPointerDown);
            ReplaceTrigger(fullmapTrigger, EventTriggerType.Drag, HandleFullmapDrag);
            ReplaceTrigger(fullmapTrigger, EventTriggerType.PointerUp, HandleFullmapPointerUp);
        }
    }

    // 이벤트 핸들러
    private void HandleCloseClicked()
    {
        minimapSystem.SetFullMapVisible(false);
    }

    // 미니맵 클릭 시 풀맵 토글
    private void HandleMinimapClick(BaseEventData data)
    {
        minimapSystem.ToggleFullMap();
    }

    // 풀맵 드래그 시작
    private void HandleFullmapPointerDown(BaseEventData data)
    {
        if (!minimapSystem.IsFullMapOpen())
        {
            return;
        }

        PointerEventData ped = data as PointerEventData;
        if (ped == null)
        {
            return;
        }

        if (ped.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isDragging = true;
    }

    // 풀맵 드래그 중
    private void HandleFullmapDrag(BaseEventData data)
    {
        if (!isDragging)
        {
            return;
        }

        if (!minimapSystem.IsFullMapOpen())
        {
            isDragging = false;
            return;
        }

        PointerEventData ped = data as PointerEventData;
        if (ped == null)
        {
            return;
        }

        minimapSystem.PanFullMapByScreenDelta(ped.delta);
    }

    // 풀맵 드래그 종료
    private void HandleFullmapPointerUp(BaseEventData data)
    {
        PointerEventData ped = data as PointerEventData;
        if (ped != null && ped.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        isDragging = false;
    }

    // EventTrigger 확인 및 추가
    private static EventTrigger EnsureEventTrigger(GameObject go)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = go.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
        }

        return trigger;
    }

    // EventTrigger 교체
    private static void ReplaceTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> callback)
    {
        if (trigger == null)
        {
            return;
        }

        // 같은 타입 엔트리 제거
        for (int i = trigger.triggers.Count - 1; i >= 0; i--)
        {
            if (trigger.triggers[i] != null && trigger.triggers[i].eventID == type)
            {
                trigger.triggers.RemoveAt(i);
            }
        }

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback.AddListener((e) => callback(e));
        trigger.triggers.Add(entry);
    }

    // 이름으로 찾기
    //private T FindInChildrenByName<T>(string name) where T : Component
    //{
    //    if (string.IsNullOrEmpty(name))
    //    {
    //        return null;
    //    }
    //
    //    Transform[] children = GetComponentsInChildren<Transform>(true);
    //    foreach (Transform t in children)
    //    {
    //        if (t != null && t.name == name)
    //        {
    //            return t.GetComponent<T>();
    //        }
    //    }
    //
    //    return null;
    //}
}
