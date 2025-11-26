using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : LivingEntity
{
    [Header("이동 설정")]
    [Tooltip("캐릭터 이동 속도")]
    public float moveSpeed = 5f;

    [Tooltip("캐릭터 회전 속도")]
    public float rotationSpeed = 5f;

    private CharacterController characterController;
    public Animator animator;

    [Header("전투 설정")]
    [Tooltip("현재 타겟")]
    public LivingEntity target;

    [Tooltip("공격 사거리 (원거리)")]
    public float attackRange = 10f;

    [Tooltip("공격 쿨타임")]
    public float attackCooldown = 1f;

    [Tooltip("마지막 공격 시간")]
    public float lastAttackCooldown;
    
    [Header("공격 설정")]
    [Tooltip("파이어볼 프리팹")]
    public GameObject fireballPrefab;

    [Tooltip("발사 위치 (지팡이 끝)")]
    public Transform firePoint;

    [Header("카메라 설정")]
    [Tooltip("카메라 타겟 오브젝트")]
    public GameObject cinemachineCameraTarget;  // PlayerCameraPivot 참조

    [Tooltip("카메라가 위로 올라갈 수 있는 최대 각도")]
    public float topClamp = 70f;

    [Tooltip("카메라가 아래로 내려갈 수 있는 최대 각도")]
    public float bottomClamp = -30f;

    [Tooltip("카메라 회전 속도")]
    public float cameraRotationSpeed = 3f;
    
    // 카메라 회전 관련 변수
    private float cinemachineTargetYaw;     // 좌우 회전
    private float cinemachineTargetPitch;   // 상하 회전

    // 입력 감도 하한선 (임계값)
    private const float threshold = 0.01f;

    [Header("카메라 줌인/줌아웃")]
    [Tooltip("Cinemachine Virtual Camera")]
    public CinemachineVirtualCamera virtualCamera;  // PlayerFollowCamera 참조

    [Tooltip("마우스 휠 줌 속도")]
    public float ZoomSpeed = 15f;

    [Tooltip("카메라 최소 거리")]
    public float MinCameraDistance = 5f;

    [Tooltip("카메라 최대 거리")]
    public float MaxCameraDistance = 20f;

    [Tooltip("카메라 기본 거리")]
    public float DefaultCameraDistance = 9f;

    [Tooltip("줌 거리 보간 속도")]
    public float zoomSmoothSpeed = 10f;


    [Tooltip("현재 카메라 거리 ")]
    private float currentCameraDistance;    // 보간을 통해 부드럽게 변경되는 실제 거리
    
    [Tooltip("목표 카메라 거리 ")]
    private float targetCameraDistance;     // 마우스 휠 입력으로 설정되는 목표값
    
    [Tooltip("PlayerFollowCamera의 cm")]
    [SerializeField] private Cinemachine3rdPersonFollow thirdPersonFollow;  // Cinemachine 3rd Person Follow 컴포넌트 참조 (카메라 거리 제어용)

    // FSM 관련
    private IState currentState;

    protected override void Awake()
    {
        base.Awake(); // LivingEntity의 Awake (체력 초기화) 실행
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 초기 상태 설정
        ChangeState(new PlayerIdleState(this));

        // Cinemachine 타겟 초기 각도 초기화
        cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;

        // 줌인/줌아웃 관련 초기화
        if (virtualCamera != null)
        {
            if (thirdPersonFollow != null)
            {
                currentCameraDistance = Mathf.Clamp(thirdPersonFollow.CameraDistance, MinCameraDistance, MaxCameraDistance);
            }
            else
            {
                currentCameraDistance = Mathf.Clamp(DefaultCameraDistance, MinCameraDistance, MaxCameraDistance);
            }
        }
        else
        {
            currentCameraDistance = Mathf.Clamp(DefaultCameraDistance, MinCameraDistance, MaxCameraDistance);
        }

        targetCameraDistance = currentCameraDistance;
        
        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.CameraDistance = currentCameraDistance;
        }
    }

    private void Update()
    {
        // 마우스 클릭 처리 (타겟팅만)
        if (Input.GetMouseButtonDown(0))
        {
            HandleTargetingInput();
        }

        // 스페이스바 입력 (공격 시도)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryAttack();
        }

        // 현재 상태의 로직 실행
        currentState?.Execute();
    }

    private void TryAttack()
    {
        // 타겟팅 체크
        if (target == null || target.IsDead)
        {
            Debug.Log("타겟이 없습니다.");
            return;
        }

        // 쿨타임 체크
        if (Time.time - lastAttackCooldown < attackCooldown)
        {
            Debug.Log("공격 쿨타임 중입니다.");
            return;
        }

        // 거리 체크
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        if (distance <= attackRange)
        {
            // 사거리 내: 즉시 공격
            ChangeState(new PlayerAttackState(this));
        }
        else
        {
            // 사거리 밖: 추적 시작
            ChangeState(new PlayerChaseState(this));
        }
    }

    // 외부(State)에서 특정 방향으로 이동시키기 위한 함수
    public void Move(Vector3 direction)
    {
        characterController.SimpleMove(direction * moveSpeed);
    }

    private void HandleTargetingInput()
    {
        // 마우스 위치로 레이 발사
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // 1. 태그 확인: "Monster" 태그인지 먼저 체크
            if (hit.collider.CompareTag("Monster"))
            {
                // 2. LivingEntity 컴포넌트 가져오기
                LivingEntity entity = hit.collider.GetComponent<LivingEntity>();
                
                // 살아있는 엔티티라면 타겟으로 설정
                if (entity != null && !entity.IsDead)
                {
                    target = entity;
                    Debug.Log($"타겟 선택: {target.name}");
                }
            }
            else
            {
                // 몬스터가 아닌 곳(빈 땅 등)을 클릭하면 타겟 해제
                target = null;
                Debug.Log("타겟 해제");
            }
        }
    }

    // 상태 변경 함수
    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void CharacterMove()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);

        // 이동 방향이 있을 때만 캐릭터 회전
        if (moveDirection.sqrMagnitude > threshold)
        {
            // 이동 방향으로 부드럽게 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        characterController.SimpleMove(moveDirection * moveSpeed);

        // 애니메이션 업데이트 (입력 크기가 임계값보다 크면 이동 중으로 판단)
        UpdateAnimation(inputDirection.magnitude > threshold);
    }

    // 애니메이션 처리 함수
    public void UpdateAnimation(bool isMove)
    {
        if (animator == null) 
        {
            return;
        }

        // isMove 파라미터 설정
        animator.SetBool("isMove", isMove);
    }

    // 카메라 기준 이동 방향 계산 함수 (카메라 회전에 따라 이동 방향이 조정되도록)
    private Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
    {
        if (cinemachineCameraTarget == null)
        {
            return inputDirection;
        }

        // Cinemachine 타겟의 회전을 기준으로 방향 벡터 계산
        Quaternion cameraRotation = Quaternion.Euler(0f, cinemachineTargetYaw, 0f);
        Vector3 forward = cameraRotation * Vector3.forward;
        Vector3 right = cameraRotation * Vector3.right;

        // 입력 방향을 카메라 기준으로 변환
        Vector3 direction = forward * inputDirection.z + right * inputDirection.x;
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        return direction;
    }

    // Update 이후에 한 번 더 실행
    // 월드 속성이 다 반영된 이후에, 카메라 방향과 같은 "연출 요소" 작업을 처리할때 유용하다.
    private void LateUpdate()
    {
        CameraRotation();
        CameraZoom();
        
        // 카메라 타겟의 회전을 항상 cinemachineTargetYaw와 cinemachineTargetPitch로 강제 설정 (캐릭터 회전에 영향을 받지 않도록)
        if (cinemachineCameraTarget != null)
        {
            cinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch, cinemachineTargetYaw, 0.0f);
        }
    }

    // 카메라 회전 함수
    private void CameraRotation()
    {
        // 마우스 우클릭이 눌려있을 때만 카메라 회전
        if (!Input.GetMouseButton(1))
        {
            return;
        }

        // 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 입력이 있을 때만 회전 (마우스가 아주 약간 움직였을때 불필요하게 카메라가 덜컥거리지 않게 하기 위한 하한선)
        if (Mathf.Abs(mouseX) > threshold || Mathf.Abs(mouseY) > threshold)
        {
            cinemachineTargetYaw += mouseX * cameraRotationSpeed;
            cinemachineTargetPitch += mouseY * cameraRotationSpeed;
        }

        // 각도 제한
        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, bottomClamp, topClamp);

        // Cinemachine 타겟 회전
        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch, cinemachineTargetYaw, 0.0f);
    }

    // 각도 제한 함수 (지면을 관통하거나 360도 이상 돌아버리는 현상 방지를 위해)
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f)
        {
            angle += 360f;
        }
        if (angle > 360f)
        {
            angle -= 360f;
        }
        
        return Mathf.Clamp(angle, min, max);
    }

    // 카메라 줌 함수
    private void CameraZoom()
    {
        if (thirdPersonFollow == null)
        {
            return;
        }

        // 마우스 휠 입력 받기
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > threshold)
        {
            // 목표 거리만 변경 (즉시 적용하지 않음)
            targetCameraDistance -= scroll * ZoomSpeed;
            targetCameraDistance = Mathf.Clamp(targetCameraDistance, MinCameraDistance, MaxCameraDistance);
        }

        // 현재 거리를 목표 거리로 부드럽게 보간
        currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetCameraDistance, Time.deltaTime * zoomSmoothSpeed);
        
        // 보간된 값을 Cinemachine에 적용
        thirdPersonFollow.CameraDistance = currentCameraDistance;
    }
}