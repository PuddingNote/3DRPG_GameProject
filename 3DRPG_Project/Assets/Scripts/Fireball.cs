using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 10;
    public Transform target; // 유도 기능용
    public float homingSensitivity = 5f; // 유도 성능 (회전 속도)

    private Vector3 direction;

    // 기존 초기화 함수 (직진용)
    public void Initialize(Vector3 dir, int dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        target = null;
        
        Destroy(gameObject, 3f); // 임시 수명 3초
    }

    // 유도 초기화 함수 (타겟 추적용)
    public void Initialize(Transform targetTransform, int dmg)
    {
        target = targetTransform;
        damage = dmg;
        
        // 초기 방향은 타겟 방향
        if (target != null)
        {
            direction = (target.position + Vector3.up - transform.position).normalized;
        }
        else
        {
            direction = transform.forward;
        }

        Destroy(gameObject, 3f);
    }

    private void Update()
    {
        if (target != null)
        {
            Vector3 targetPos = target.position;

            // Collider가 있다면 그 중심(Center)을 타겟으로 설정
            Collider targetCollider = target.GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetPos = targetCollider.bounds.center;
            }

            // 타겟 방향 계산
            Vector3 targetDir = (targetPos - transform.position).normalized;
            
            // 현재 방향에서 타겟 방향으로 부드럽게 회전 (유도)
            direction = Vector3.Slerp(direction, targetDir, Time.deltaTime * homingSensitivity);
        }

        // 이동
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
        
        // 투사체 자체 회전 (진행 방향 보기)
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어 자신과의 충돌 무시
        if (other.CompareTag("Player")) 
        {
            return;
        }
        
        // 2. 타겟 지정 공격인 경우, 타겟이 아니면 무시 (관통)
        if (target != null && other.transform != target && other.transform.root != target)
        {
            // 충돌한 오브젝트가 타겟 본체도 아니고, 타겟의 자식/부모 관계도 아니라면 무시
            return;
        }

        // 3. 데미지 처리
        IDamageable targetEntity = other.GetComponent<IDamageable>();
        if (targetEntity != null)
        {
            targetEntity.TakeDamage(damage);
            
            // 적중 이펙트 생성 (추후 구현)

            // 투사체 파괴
            Destroy(gameObject);
        }
    }
}
