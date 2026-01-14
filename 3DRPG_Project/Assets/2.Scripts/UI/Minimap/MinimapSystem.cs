using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// RenderTexture 기반 미니맵/풀맵 시스템.
/// - 맵은 북쪽 고정 (카메라 회전 고정)
/// - 중앙 UI 오버레이로 플레이어 방향(화살표)과 메인 카메라 방향(시야 콘) 표시
/// - 미니맵 클릭 시 풀맵 토글(별도 카메라/RT 사용 가능)
/// </summary>
public class MinimapSystem : MonoBehaviour
{
    [Header("UI - Minimap")]
    [Tooltip("미니맵 출력용 RawImage(RenderTexture가 연결)")]
    [SerializeField] private RawImage minimapImage;



    [Header("UI - Full Map")]
    [Tooltip("풀맵 패널 Root(활성/비활성 토글 대상)")]
    [SerializeField] private GameObject fullMapRoot;
    [Tooltip("풀맵 출력용 RawImage(RenderTexture가 연결)")]
    [SerializeField] private RawImage fullMapImage;



    [Header("UI - Full Map Aspect")]
    [Tooltip("RawImage는 preserveAspect를 지원하지 않으므로, AspectRatioFitter로 비율을 유지할지 여부")]
    [SerializeField] private bool fullMapUseAspectRatioFitter = true;

    [Tooltip("AspectRatioFitter의 Mode. 일반적으로 FitInParent 권장(부모 영역 안에 비율 유지)")]
    [SerializeField] private AspectRatioFitter.AspectMode fullMapAspectMode = AspectRatioFitter.AspectMode.FitInParent;



    [Header("Camera Settings")]
    [Tooltip("미니맵 카메라 (없으면 런타임에 자동 생성)")]
    [SerializeField] private Camera minimapCamera;

    [Tooltip("풀맵 카메라 (없으면 런타임에 자동 생성, 풀맵이 필요 없다면 비워도 됨)")]
    [SerializeField] private Camera fullMapCamera;

    [Tooltip("미니맵 카메라 높이")]
    [SerializeField] private float cameraHeight = 100f;



    [Header("Camera Height Behavior")]
    [Tooltip("카메라 Y를 followTarget의 Y를 기준으로 둘지 여부. 끄면 baseY + cameraHeight로 고정(권장: 끔)")]
    [SerializeField] private bool useTargetYForCameraHeight = false;

    [Tooltip("useTargetYForCameraHeight=false일 때 사용할 기준 Y. autoBaseYFromBounds가 켜져있으면 bounds center.y를 사용")]
    [SerializeField] private float cameraBaseY = 0f;

    [Tooltip("useTargetYForCameraHeight=false일 때, 씬 bounds 중심 Y를 자동 기준으로 사용할지 여부(권장: 켬)")]
    [SerializeField] private bool autoBaseYFromBounds = true;

    [Tooltip("미니맵 Ortho Size (화면에 보여줄 반경)")]
    [SerializeField] private float minimapOrthoSize = 30f;

    [Tooltip("풀맵 Ortho Size (화면에 보여줄 반경)")]
    [SerializeField] private float fullMapOrthoSize = 45f;

    [Tooltip("미니맵 카메라가 따라갈 대상(플레이어). 비워두면 GameManager.currentPlayer를 자동 사용")]
    [SerializeField] private Transform followTarget;

    [Tooltip("미니맵 전용 레이어 마스크 (Minimap, Interaction 레이어 등에 사용)")]
    [SerializeField] private LayerMask minimapCullingMask = ~0;



    [Header("Main Camera Exclusion")]
    [Tooltip("메인 카메라(Camera.main)에서 Minimap 레이어를 자동으로 제외하여, 마커 스프라이트가 일반 화면에 보이지 않게 함")]
    [SerializeField] private bool autoExcludeMinimapLayerFromMainCamera = true;

    [Tooltip("메인 카메라에서 제외할 레이어 이름(기본: Minimap).")]
    [SerializeField] private string minimapLayerName = "Minimap";



    [Header("Background Colors")]
    [Tooltip("미니맵 카메라 배경색")]
    [SerializeField] private Color minimapBackgroundColor = new Color(0f, 0f, 0f, 1f);

    [Tooltip("풀맵 카메라 배경색. 원형 맵 바깥 빈 공간을 가리는 용도(마비노기 모바일에서는 구름이미지로 설정)")]
    [SerializeField] private Color fullMapBackgroundColor = new Color(0f, 0f, 0f, 1f);



    [Header("Full Map Bounds")]
    [Tooltip("풀맵 드래그 이동 시 월드 경계를 자동 계산할지 여부(권장: 켬). Minimap 레이어의 Renderer bounds를 합산")]
    [SerializeField] private bool autoComputeFullMapBounds = true;

    [Tooltip("bounds 자동 계산 시 탐색 기준 루트(비우면 씬 전체). 마을/던전 프리팹 루트를 넣으면 더 안전")]
    [SerializeField] private Transform boundsSearchRoot;

    [Tooltip("autoComputeFullMapBounds=false일 때 사용하는 수동 bounds(월드 좌표 기준)")]
    [SerializeField] private Bounds manualFullMapWorldBounds = new Bounds(Vector3.zero, new Vector3(100f, 10f, 100f));

    [Tooltip("풀맵 드래그 경계(bounds)에 여유 공간을 추가. (x=minX, y=maxX, z=minZ, w=maxZ) 단위: 월드 유닛")]
    [SerializeField] private Vector4 fullMapBoundsPaddingWorld = Vector4.zero;



    [Header("Full Map Default Fit")]
    [Tooltip("풀맵을 열 때, 현재 씬 bounds를 기준으로 '전체가 화면에 들어오는' 기본 줌을 자동 계산할지 여부")]
    [SerializeField] private bool autoFitFullMapOrthoSizeToBounds = true;

    [Tooltip("자동 Fit 시 여백 비율(1.0이면 딱 맞춤, 1.05면 5% 여백)")]
    [SerializeField] private float fullMapFitPadding = 1.05f;



    [Header("Debug")]
    [Tooltip("풀맵 bounds/권장 orthoSize/적용 orthoSize 로그 출력")]
    [SerializeField] private bool debugLogFullMapFit = false;



    [Header("RenderTexture")]
    [Tooltip("미니맵 RenderTexture 해상도(정사각). 높을수록 선명하지만 비용 증가")]
    [SerializeField] private int minimapTextureSize = 512;

    [Tooltip("풀맵 RenderTexture 해상도(정사각). autoFullMapRenderTextureToScreenSize가 꺼져있을 때 사용")]
    [SerializeField] private int fullMapTextureSize = 1024;



    [Header("RenderTexture - Quality")]
    [Tooltip("풀맵 RenderTexture를 화면 해상도(또는 그에 근접)로 자동 생성해서 퍼져 보이는 현상을 줄일지 여부(권장: 켬)")]
    [SerializeField] private bool autoFullMapRenderTextureToScreenSize = true;

    [Tooltip("autoFullMapRenderTextureToScreenSize 사용 시, RenderTexture 최대 가로/세로 픽셀(성능 보호용)")]
    [SerializeField] private int fullMapRenderTextureMaxSize = 2048;

    [Tooltip("미니맵 RenderTexture 필터 모드. Bilinear는 부드럽고, Point는 선명하지만 픽셀 느낌이 납니다.")]
    [SerializeField] private FilterMode minimapRenderTextureFilterMode = FilterMode.Bilinear;

    [Tooltip("풀맵 RenderTexture 필터 모드. Bilinear는 확대 시 퍼져 보일 수 있어 해상도 증가와 함께 쓰는 것을 권장합니다.")]
    [SerializeField] private FilterMode fullMapRenderTextureFilterMode = FilterMode.Bilinear;



    [Header("Direction Overlays")]
    [Tooltip("월드 북쪽(+Z)을 미니맵 위쪽으로 가정. 프로젝트 기준이 다르면 보정각을 사용")]
    [SerializeField] private float northUpYawOffsetDegrees = 0f;

    [Tooltip("카메라 시야 콘 각도(예: 30도). 스프라이트 방식이면 회전만 하고, Filled Image면 fillAmount도 설정")]
    [SerializeField] private float cameraViewAngleDegrees = 30f;



    [Header("Behavior")]
    [Tooltip("미니맵/풀맵 카메라가 없으면 런타임에 자동 생성할지 여부")]
    [SerializeField] private bool createCamerasAtRuntimeIfMissing = true;
    [Tooltip("RenderTexture를 런타임에 자동 생성할지 여부(Inspector에서 직접 할당하는 방식이면 끔)")]
    [SerializeField] private bool createRenderTexturesAtRuntime = true;
    [Tooltip("풀맵이 열려있는 동안 플레이어 '수동 조작 입력'을 잠글지 여부(GameManager.SetPlayerManualInputLocked 사용). 자동 전투/이동은 계속 진행.")]
    [SerializeField] private bool lockPlayerInputWhileFullMapOpen = true;



    [Header("Full Map Interaction")]
    [Tooltip("풀맵 드래그 방향 반전. 현재 체감이 반대라면 true/false를 바꿔서 즉시 조정 가능")]
    [SerializeField] private bool invertFullMapPanDirection = true;

    [Tooltip("풀맵 휠 줌 활성화 여부")]
    [SerializeField] private bool enableFullMapWheelZoom = true;

    [Tooltip("풀맵 휠 줌 속도(orthographicSize 변화량)")]
    [SerializeField] private float fullMapZoomSpeed = 80f;

    [Tooltip("풀맵 최소 줌(orthographicSize 최소값)")]
    [SerializeField] private float fullMapMinOrthoSize = 30f;

    [Tooltip("풀맵 최대 줌(orthographicSize 최대값)")]
    [SerializeField] private float fullMapMaxOrthoSize = 70f;

    private RenderTexture minimapRT;
    private RenderTexture fullMapRT;
    private bool isFullMapOpen;
    private Coroutine initializeCoroutine;
    private Bounds fullMapWorldBounds;
    private Vector3 fullMapCenter;
    private int fullMapRTCreatedWidth;
    private int fullMapRTCreatedHeight;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 초기 씬에서는 sceneLoaded 이벤트를 이미 놓쳤을 수 있으므로, "게임 내 모든 초기화가 끝난 뒤" 적용을 보장하기 위해 Start에서 지연 초기화.
        SetFullMapVisible(false);
    }

    private void Start()
    {
        BeginDeferredInitialize();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ReleaseRenderTargets();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 이동 시 플레이어/카메라/UI가 재구성될 수 있으므로 "지연 재초기화"
        BeginDeferredInitialize();
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            // 안전장치: 런타임 중에 타겟이 늦게 알려지는 경우 자동 바인딩
            TryAutoBindTarget();
        }

        UpdateCameraFollow();
        //UpdateDirectionOverlays();
    }

    private void Update()
    {
        if (isFullMapOpen)
        {
            HandleFullMapWheelZoom();
        }
    }

    // 풀맵 토글
    public void ToggleFullMap()
    {
        SetFullMapVisible(!isFullMapOpen);
    }

    // 풀맵 표시 여부 설정
    public void SetFullMapVisible(bool visible)
    {
        isFullMapOpen = visible;

        if (fullMapRoot != null)
        {
            fullMapRoot.SetActive(visible);
        }

        if (fullMapCamera != null)
        {
            fullMapCamera.enabled = visible;
        }

        if (visible)
        {
            // 열리는 순간 타겟/바운즈가 아직 준비 안 된 경우가 있으므로 즉시 재시도
            TryAutoBindTarget();
            RecalculateFullMapBounds();

            // 풀맵 활성화 시에는 시작 줌을 fullMapOrthoSize로 고정
            if (fullMapCamera != null)
            {
                fullMapCamera.orthographicSize = Mathf.Clamp(fullMapOrthoSize, fullMapMinOrthoSize, fullMapMaxOrthoSize);
            }

            // 풀맵을 열 때는 플레이어 중심으로 시작(이후 드래그로 이동)
            if (followTarget != null)
            {
                fullMapCenter = followTarget.position;
            }
            else
            {
                fullMapCenter = fullMapWorldBounds.center;
            }
            fullMapCenter = ClampFullMapCenter(fullMapCenter);
            ApplyFullMapCenterToCamera();
        }

        if (lockPlayerInputWhileFullMapOpen && GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerManualInputLocked(visible);
        }
    }

    // 풀맵 열림 여부 확인
    public bool IsFullMapOpen()
    {
        return isFullMapOpen;
    }

    // 타겟 바인딩
    public void BindTarget(Transform target)
    {
        followTarget = target;
    }

    // 지연 초기화 시작
    private void BeginDeferredInitialize()
    {
        if (initializeCoroutine != null)
        {
            StopCoroutine(initializeCoroutine);
            initializeCoroutine = null;
        }

        initializeCoroutine = StartCoroutine(DeferredInitializeRoutine());
    }

    // 지연 초기화 루틴
    private System.Collections.IEnumerator DeferredInitializeRoutine()
    {
        // "게임 내 모든 설정이 완료된 뒤"를 최대한 보장하기 위해 최소 1프레임(EndOfFrame) 대기.
        yield return new WaitForEndOfFrame();

        // GameManager/Player가 더 늦게 준비되는 씬도 있으므로, 짧게 폴링(타임아웃 포함)
        float timeout = 3.0f;
        float startTime = Time.unscaledTime;

        while (Time.unscaledTime - startTime < timeout)
        {
            TryAutoBindTarget();
            if (followTarget != null)
            {
                break;
            }

            yield return null;
        }

        EnsureCameras();                    // 카메라 생성
        EnsureRenderTargets();              // RenderTexture 생성
        ApplyCameraDefaults();              // 카메라 기본 설정
        ApplyMainCameraLayerExclusion();    // 메인 카메라에서 미니맵 레이어를 제외
        RecalculateFullMapBounds();         // 풀맵 bounds 재계산

        // 1회 즉시 반영 (첫 프레임에 빈 화면이 뜨는 것을 완화)
        UpdateCameraFollow();               // 카메라 팔로우 업데이트
        //UpdateDirectionOverlays();
    }

    // 타겟 자동 바인딩 시도
    private void TryAutoBindTarget()
    {
        if (followTarget != null)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
        {
            followTarget = GameManager.Instance.currentPlayer.transform;
        }
    }

    // 미니맵/풀맵 카메라 자동 생성
    private void EnsureCameras()
    {
        if (!createCamerasAtRuntimeIfMissing)
        {
            return;
        }

        if (minimapCamera == null)
        {
            GameObject camObj = new GameObject("MinimapCamera");
            camObj.transform.SetParent(transform, false);
            minimapCamera = camObj.AddComponent<Camera>();
        }

        if (fullMapCamera == null)
        {
            GameObject camObj = new GameObject("FullMapCamera");
            camObj.transform.SetParent(transform, false);
            fullMapCamera = camObj.AddComponent<Camera>();
        }
    }

    // 미니맵/풀맵 RenderTexture 자동 생성
    private void EnsureRenderTargets()
    {
        if (!createRenderTexturesAtRuntime)
        {
            return;
        }

        if (minimapRT == null)
        {
            minimapRT = new RenderTexture(minimapTextureSize, minimapTextureSize, 16, RenderTextureFormat.ARGB32);
            minimapRT.name = "RT_Minimap";
            minimapRT.filterMode = minimapRenderTextureFilterMode;
            minimapRT.Create();
        }

        EnsureFullMapRenderTexture();

        if (minimapCamera != null)
        {
            minimapCamera.targetTexture = minimapRT;
        }

        if (fullMapCamera != null)
        {
            fullMapCamera.targetTexture = fullMapRT;
        }

        if (minimapImage != null)
        {
            minimapImage.texture = minimapRT;
        }

        if (fullMapImage != null)
        {
            fullMapImage.texture = fullMapRT;
            ApplyFullMapAspectRatioFitter();
        }
    }

    // 풀맵 AspectRatioFitter 적용
    private void ApplyFullMapAspectRatioFitter()
    {
        if (fullMapImage == null)
        {
            return;
        }

        AspectRatioFitter fitter = fullMapImage.GetComponent<AspectRatioFitter>();
        if (!fullMapUseAspectRatioFitter)
        {
            if (fitter != null)
            {
                fitter.enabled = false;
            }
            return;
        }

        if (fitter == null)
        {
            fitter = fullMapImage.gameObject.AddComponent<AspectRatioFitter>();
        }

        fitter.enabled = true;
        fitter.aspectMode = fullMapAspectMode;

        if (fullMapRT != null && fullMapRT.height > 0)
        {
            fitter.aspectRatio = (float)fullMapRT.width / fullMapRT.height;
        }
    }

    // 풀맵 RenderTexture 자동 생성
    private void EnsureFullMapRenderTexture()
    {
        int desiredW;
        int desiredH;

        if (autoFullMapRenderTextureToScreenSize)
        {
            desiredW = Mathf.Clamp(Screen.width, 1, fullMapRenderTextureMaxSize);
            desiredH = Mathf.Clamp(Screen.height, 1, fullMapRenderTextureMaxSize);
        }
        else
        {
            desiredW = Mathf.Max(1, fullMapTextureSize);
            desiredH = Mathf.Max(1, fullMapTextureSize);
        }

        bool needRecreate = false;
        if (fullMapRT == null)
        {
            needRecreate = true;
        }
        else if (fullMapRT.width != desiredW || fullMapRT.height != desiredH)
        {
            needRecreate = true;
        }

        if (!needRecreate)
        {
            return;
        }

        if (fullMapRT != null)
        {
            fullMapRT.Release();
            Destroy(fullMapRT);
            fullMapRT = null;
        }

        fullMapRT = new RenderTexture(desiredW, desiredH, 16, RenderTextureFormat.ARGB32);
        fullMapRT.name = "RT_FullMap";
        fullMapRT.filterMode = fullMapRenderTextureFilterMode;
        fullMapRT.Create();

        fullMapRTCreatedWidth = desiredW;
        fullMapRTCreatedHeight = desiredH;
    }

    // RenderTexture 해제
    private void ReleaseRenderTargets()
    {
        if (minimapRT != null)
        {
            minimapRT.Release();
            Destroy(minimapRT);
            minimapRT = null;
        }

        if (fullMapRT != null)
        {
            fullMapRT.Release();
            Destroy(fullMapRT);
            fullMapRT = null;
        }
    }

    // 카메라 기본 설정 적용
    private void ApplyCameraDefaults()
    {
        ApplyDefaultsToCamera(minimapCamera, minimapOrthoSize);
        ApplyDefaultsToCamera(fullMapCamera, fullMapOrthoSize);

        if (minimapCamera != null)
        {
            minimapCamera.backgroundColor = minimapBackgroundColor;
        }

        if (fullMapCamera != null)
        {
            fullMapCamera.backgroundColor = fullMapBackgroundColor;
        }
    }

    // 메인 카메라에서 미니맵 레이어를 제외
    private void ApplyMainCameraLayerExclusion()
    {
        if (!autoExcludeMinimapLayerFromMainCamera)
        {
            return;
        }

        int layer = LayerMask.NameToLayer(minimapLayerName);
        if (layer < 0)
        {
            return;
        }

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return;
        }

        // 현재 카메라의 cullingMask에서, layer에 해당하는 비트만 0으로 만들고(끄고), 나머지 레이어 설정은 그대로 유지한다는 뜻.
        mainCam.cullingMask &= ~(1 << layer);
    }

    // 카메라 기본 설정 적용
    private void ApplyDefaultsToCamera(Camera cam, float orthoSize)
    {
        if (cam == null)
        {
            return;
        }

        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 기본값(개별 카메라 배경은 ApplyCameraDefaults에서 지정)
        cam.cullingMask = minimapCullingMask;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 500f;

        // 북쪽 고정: 항상 아래를 바라보고, Yaw는 0(+offset)
        cam.transform.rotation = Quaternion.Euler(90f, northUpYawOffsetDegrees, 0f);
    }

    // 카메라 팔로우 업데이트
    private void UpdateCameraFollow()
    {
        if (followTarget == null)
        {
            return;
        }

        UpdateSingleCameraFollow(minimapCamera);

        if (!isFullMapOpen)
        {
            // 풀맵이 닫혀있으면 풀맵 카메라도 플레이어를 따라가도록 유지(열릴 때 플레이어 중심 시작을 보장)
            fullMapCenter = followTarget.position;
            UpdateSingleCameraFollow(fullMapCamera);
        }
        else
        {
            ApplyFullMapCenterToCamera();
        }
    }

    // 단일 카메라 팔로우 업데이트
    private void UpdateSingleCameraFollow(Camera cam)
    {
        if (cam == null)
        {
            return;
        }

        Vector3 targetPos = followTarget.position;
        float y = GetMapCameraY(targetPos.y);
        cam.transform.position = new Vector3(targetPos.x, y, targetPos.z);
    }

    // 풀맵 패닝
    public void PanFullMapByScreenDelta(Vector2 screenDeltaPixels)
    {
        if (!isFullMapOpen)
        {
            return;
        }

        if (fullMapCamera == null)
        {
            return;
        }

        // Orthographic 기준: 화면 픽셀 이동량을 월드 이동량으로 변환
        float unitsPerPixel = (fullMapCamera.orthographicSize * 2f) / Mathf.Max(1f, (float)Screen.height);
        float sign = invertFullMapPanDirection ? 1f : -1f;
        float dx = sign * screenDeltaPixels.x * unitsPerPixel;
        float dz = sign * screenDeltaPixels.y * unitsPerPixel;

        fullMapCenter += new Vector3(dx, 0f, dz);
        fullMapCenter = ClampFullMapCenter(fullMapCenter);
        ApplyFullMapCenterToCamera();
    }

    // 풀맵 월드 바운즈 반환
    public Bounds GetFullMapWorldBounds()
    {
        return fullMapWorldBounds;
    }

    // 풀맵 월드 바운즈 재계산
    public void RecalculateFullMapBounds()
    {
        if (!autoComputeFullMapBounds)
        {
            fullMapWorldBounds = manualFullMapWorldBounds;
            ApplyFullMapBoundsPadding();
            ApplyFullMapDefaultFitIfNeeded();
            return;
        }

        int targetLayer = LayerMaskToSingleLayerIndex(minimapCullingMask);
        if (targetLayer < 0)
        {
            // 마스크가 단일 레이어가 아니면, Minimap 레이어를 우선 사용
            targetLayer = LayerMask.NameToLayer("Minimap");
        }

        Bounds? bounds = ComputeRendererBoundsForLayer(targetLayer, boundsSearchRoot);
        if (bounds.HasValue)
        {
            fullMapWorldBounds = bounds.Value;
        }
        else
        {
            // fallback: 플레이어 주변 임시 bounds
            Vector3 center = (followTarget != null) ? followTarget.position : Vector3.zero;
            fullMapWorldBounds = new Bounds(center, new Vector3(200f, 10f, 200f));
        }

        ApplyFullMapBoundsPadding();
        ApplyFullMapDefaultFitIfNeeded();
    }

    // 풀맵 bounds에 여유 공간(패딩) 적용
    private void ApplyFullMapBoundsPadding()
    {
        if (fullMapBoundsPaddingWorld == Vector4.zero)
        {
            return;
        }

        Bounds b = fullMapWorldBounds;
        Vector3 min = b.min;
        Vector3 max = b.max;

        min.x -= Mathf.Max(0f, fullMapBoundsPaddingWorld.x);
        max.x += Mathf.Max(0f, fullMapBoundsPaddingWorld.y);
        min.z -= Mathf.Max(0f, fullMapBoundsPaddingWorld.z);
        max.z += Mathf.Max(0f, fullMapBoundsPaddingWorld.w);

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = (max - min);
        
        size.y = b.size.y;  // y 크기는 드래그 경계에 영향이 없으므로 기존 값 유지

        fullMapWorldBounds = new Bounds(center, size);
    }

    // 풀맵 기본 피트 적용
    private void ApplyFullMapDefaultFitIfNeeded()
    {
        if (!autoFitFullMapOrthoSizeToBounds)
        {
            return;
        }

        if (fullMapCamera == null)
        {
            return;
        }

        // orthographicSize는 "세로 반높이"이므로, bounds가 화면에 들어오려면
        // 세로 기준: extents.z
        // 가로 기준: extents.x / aspect
        float aspect = (float)Screen.width / Mathf.Max(1f, (float)Screen.height);
        Bounds b = fullMapWorldBounds;

        float requiredHalfHeight = Mathf.Max(b.extents.z, b.extents.x / Mathf.Max(0.0001f, aspect));
        requiredHalfHeight *= Mathf.Max(1.0f, fullMapFitPadding);

        float clamped = Mathf.Clamp(requiredHalfHeight, fullMapMinOrthoSize, fullMapMaxOrthoSize);
        fullMapCamera.orthographicSize = clamped;

        if (debugLogFullMapFit)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log(
                $"[MinimapSystem] FullMap Fit ({sceneName}) " +
                $"BoundsCenter={b.center}, BoundsSize={b.size}, " +
                $"Aspect={aspect:0.###}, RequiredOrtho={requiredHalfHeight:0.###}, " +
                $"AppliedOrtho={clamped:0.###} (ClampRange={fullMapMinOrthoSize:0.###}~{fullMapMaxOrthoSize:0.###})"
            );
        }

        // 줌 값이 바뀌면 화면 크기(halfW/halfH)가 변하므로, 중심도 다시 클램프
        fullMapCenter = ClampFullMapCenter(fullMapCenter);
        ApplyFullMapCenterToCamera();
    }

    // 풀맵 중심 카메라 위치 업데이트
    private void ApplyFullMapCenterToCamera()
    {
        if (fullMapCamera == null)
        {
            return;
        }

        float y = GetMapCameraY(fullMapCenter.y);
        fullMapCamera.transform.position = new Vector3(fullMapCenter.x, y, fullMapCenter.z);
    }

    // 맵 카메라 Y 계산
    private float GetMapCameraY(float targetY)
    {
        if (useTargetYForCameraHeight)
        {
            return targetY + cameraHeight;
        }

        float baseY = cameraBaseY;
        if (autoBaseYFromBounds && fullMapWorldBounds.size.sqrMagnitude > 0.0001f)
        {
            baseY = fullMapWorldBounds.center.y;
        }

        return baseY + cameraHeight;
    }

    // 풀맵 중심 클램프
    private Vector3 ClampFullMapCenter(Vector3 desiredCenter)
    {
        Bounds b = fullMapWorldBounds;
        if (b.size.x <= 0.0001f || b.size.z <= 0.0001f || fullMapCamera == null)
        {
            return desiredCenter;
        }

        float aspect = (float)Screen.width / Mathf.Max(1f, (float)Screen.height);
        float halfH = fullMapCamera.orthographicSize;
        float halfW = halfH * aspect;

        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minZ = b.min.z + halfH;
        float maxZ = b.max.z - halfH;

        // 맵이 화면보다 작은 경우(=min > max)에는 중앙 고정
        float clampedX;
        if (minX > maxX)
        {
            clampedX = b.center.x;
        }
        else
        {
            clampedX = Mathf.Clamp(desiredCenter.x, minX, maxX);
        }

        float clampedZ;
        if (minZ > maxZ)
        {
            clampedZ = b.center.z;
        }
        else
        {
            clampedZ = Mathf.Clamp(desiredCenter.z, minZ, maxZ);
        }

        return new Vector3(clampedX, desiredCenter.y, clampedZ);
    }

    // 레이어 마스크를 단일 레이어 인덱스로 변환
    private static int LayerMaskToSingleLayerIndex(LayerMask mask)
    {
        int value = mask.value;
        if (value == 0)
        {
            return -1;
        }

        // 단일 비트인지 확인
        if ((value & (value - 1)) != 0)
        {
            return -1;
        }

        int idx = 0;
        while (value > 1)
        {
            value >>= 1;
            idx++;
        }

        return idx;
    }

    // 레이어 렌더러 바운즈 계산
    private static Bounds? ComputeRendererBoundsForLayer(int layerIndex, Transform root)
    {
        Renderer[] renderers;
        if (root != null)
        {
            renderers = root.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        Bounds? bounds = null;
        foreach (Renderer r in renderers)
        {
            if (r == null)
            {
                continue;
            }

            if (r.gameObject.layer != layerIndex)
            {
                continue;
            }

            if (!bounds.HasValue)
            {
                bounds = r.bounds;
            }
            else
            {
                Bounds b = bounds.Value;
                b.Encapsulate(r.bounds);
                bounds = b;
            }
        }

        return bounds;
    }

    /*
    private void UpdateDirectionOverlays()
    {
        UpdateOverlayForTarget(followTarget, playerDirectionArrow, fullMapPlayerDirectionArrow);
        UpdateOverlayForCamera(cameraViewCone, fullMapCameraViewCone);
        UpdateOverlayFill(cameraViewConeImage, fullMapCameraViewConeImage);
    }

    private void UpdateOverlayForTarget(Transform target, RectTransform minimapArrow, RectTransform fullMapArrow)
    {
        if (target == null)
        {
            return;
        }

        float yaw = target.eulerAngles.y + northUpYawOffsetDegrees;
        float zRot = -yaw;

        if (minimapArrow != null)
        {
            // 오버레이는 항상 화면 중앙 고정(줌/드래그에 영향을 받지 않게)
            minimapArrow.anchoredPosition = Vector2.zero;
            minimapArrow.localEulerAngles = new Vector3(0f, 0f, zRot);
        }

        if (fullMapArrow != null)
        {
            // 풀맵에서는 "플레이어가 지도 위에서 어디 있는지"가 보여야 하므로
            // 현재 풀맵 카메라 뷰포트 기준으로 UI 위치를 계산해 따라다니게 합니다.
            fullMapArrow.anchoredPosition = GetFullMapAnchoredPositionForWorldPoint(target.position);
            fullMapArrow.localEulerAngles = new Vector3(0f, 0f, zRot);
        }
    }

    private void UpdateOverlayForCamera(RectTransform minimapCone, RectTransform fullMapCone)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            return;
        }

        Vector3 forward = mainCam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        forward.Normalize();
        float camYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + northUpYawOffsetDegrees;
        float zRot = -camYaw;

        if (minimapCone != null)
        {
            // 오버레이는 항상 화면 중앙 고정
            minimapCone.anchoredPosition = Vector2.zero;
            minimapCone.localEulerAngles = new Vector3(0f, 0f, zRot);
        }

        if (fullMapCone != null)
        {
            // 풀맵에서는 플레이어 위치를 기준으로 카메라 시야 콘을 붙이는 것이 자연스러움
            if (followTarget != null)
            {
                fullMapCone.anchoredPosition = GetFullMapAnchoredPositionForWorldPoint(followTarget.position);
            }
            else
            {
                fullMapCone.anchoredPosition = Vector2.zero;
            }
            fullMapCone.localEulerAngles = new Vector3(0f, 0f, zRot);
        }
    }

    private Vector2 GetFullMapAnchoredPositionForWorldPoint(Vector3 worldPoint)
    {
        if (!isFullMapOpen)
        {
            return Vector2.zero;
        }

        if (fullMapCamera == null || fullMapImage == null)
        {
            return Vector2.zero;
        }

        RectTransform rect = fullMapImage.rectTransform;
        Rect r = rect.rect;
        if (r.width <= 0.0001f || r.height <= 0.0001f)
        {
            return Vector2.zero;
        }

        // 풀맵 카메라 뷰포트(0~1) -> RawImage 로컬 좌표로 변환
        Vector3 vp = fullMapCamera.WorldToViewportPoint(worldPoint);
        float x = (vp.x - 0.5f) * r.width;
        float y = (vp.y - 0.5f) * r.height;
        return new Vector2(x, y);
    }

    private void UpdateOverlayFill(Image minimapFillImage, Image fullMapFillImage)
    {
        // Filled Image(방사형 부채꼴)로 쓰는 경우에만 fillAmount 세팅.
        // (스프라이트 방식이면 해당 Image는 비워두세요)
        float fill = Mathf.Clamp01(cameraViewAngleDegrees / 360f);

        if (minimapFillImage != null)
        {
            minimapFillImage.fillAmount = fill;
        }

        if (fullMapFillImage != null)
        {
            fullMapFillImage.fillAmount = fill;
        }
    }
    */

    // 풀맵 휠 줌 처리
    private void HandleFullMapWheelZoom()
    {
        if (!enableFullMapWheelZoom)
        {
            return;
        }

        if (fullMapCamera == null)
        {
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) <= 0.0001f)
        {
            return;
        }

        float nextSize = fullMapCamera.orthographicSize - scroll * fullMapZoomSpeed;
        nextSize = Mathf.Clamp(nextSize, fullMapMinOrthoSize, fullMapMaxOrthoSize);
        fullMapCamera.orthographicSize = nextSize;

        // 줌 값이 바뀌면 화면 크기(halfW/halfH)가 변하므로, 중심도 다시 클램프
        fullMapCenter = ClampFullMapCenter(fullMapCenter);
        ApplyFullMapCenterToCamera();
    }
}
