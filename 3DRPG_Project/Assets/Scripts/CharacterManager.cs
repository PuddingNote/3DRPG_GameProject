using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class CharacterManager : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("캐릭터 이동 속도")]
    public float moveSpeed = 8f;

    private CharacterController characterController;

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
    public float ZoomSpeed = 7f;

    [Tooltip("카메라 최소 거리")]
    public float MinCameraDistance = 4f;

    [Tooltip("카메라 최대 거리")]
    public float MaxCameraDistance = 15f;

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

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
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
        CharacterMove();
    }

    private void CharacterMove()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        Vector3 moveDirection = GetCameraRelativeDirection(inputDirection);

        characterController.SimpleMove(moveDirection * moveSpeed);
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