using System.Collections;
using UnityEngine;

// 아이템 실물이 아닌 등급 VFX만 가지고, Drop(포물선 낙하) → Idle(대기) → Absorb(플레이어로 가속 흡수) 연출을 수행
public class DroppedItemVFX : MonoBehaviour
{
    [Header("Drop (Item Drop 단계)")]
    [Min(0.01f)] public float dropDuration = 0.55f;
    [Min(0f)] public float spawnUpOffset = 1.2f;
    [Min(0f)] public float dropRadius = 1.8f;
    [Min(0f)] public float arcHeight = 1.0f;
    public LayerMask groundMask = ~0;



    [Header("Landing (착지 높이)")]
    [Tooltip("true면 레이캐스트 결과와 무관하게 '착지 y'를 고정")]
    public bool useFixedLandingY = true;

    [Tooltip("useFixedLandingY가 true일 때 착지할 월드 y값")]
    public float fixedLandingY = 0.25f;



    [Header("Idle (Item Idle 단계)")]
    [Min(0f)] public float idleMinSeconds = 1.0f;
    [Min(0f)] public float idleMaxSeconds = 2.0f;



    [Header("Absorb (Item Absorb 단계)")]
    [Min(0f)] public float absorbStartSpeed = 2.0f;
    [Min(0f)] public float absorbAcceleration = 18.0f;
    [Min(0.01f)] public float absorbStopDistance = 0.35f;
    public Vector3 absorbTargetOffset = new Vector3(0f, 1.2f, 0f);

    private Transform absorbTarget;
    private Coroutine routine;

    public void Play(Vector3 originPosition, Transform target)
    {
        absorbTarget = target;

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(PlayRoutine(originPosition));
    }

    private IEnumerator PlayRoutine(Vector3 originPosition)
    {
        Vector3 startPos = originPosition + Vector3.up * spawnUpOffset;
        Vector3 desiredEndPos = originPosition + GetRandomCircleOffset(dropRadius);

        Vector3 endPos = FindGroundPosition(desiredEndPos);
        if (useFixedLandingY)
        {
            endPos.y = fixedLandingY;
        }

        // Drop: 포물선 느낌(중간이 가장 높게)
        float dropTimer = 0f;
        while (dropTimer < dropDuration)
        {
            dropTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dropTimer / dropDuration);

            Vector3 linear = Vector3.Lerp(startPos, endPos, t);
            float arc = Mathf.Sin(Mathf.PI * t) * arcHeight;
            transform.position = linear + Vector3.up * arc;

            yield return null;
        }

        transform.position = endPos;

        // Idle: 1~2초간 정지
        float idleSeconds = Random.Range(idleMinSeconds, idleMaxSeconds);
        if (idleSeconds > 0f)
        {
            yield return new WaitForSeconds(idleSeconds);
        }

        // Absorb: 거리 무관, 점점 빨라지며 플레이어로 이동
        if (absorbTarget == null)
        {
            Destroy(gameObject);
            yield break;
        }

        float speed = absorbStartSpeed;
        while (absorbTarget != null)
        {
            Vector3 targetPos = absorbTarget.position + absorbTargetOffset;
            Vector3 toTarget = targetPos - transform.position;

            float distance = toTarget.magnitude;
            if (distance <= absorbStopDistance)
            {
                break;
            }

            speed += absorbAcceleration * Time.deltaTime;

            Vector3 dir = toTarget / Mathf.Max(distance, 0.0001f);
            transform.position += dir * speed * Time.deltaTime;

            yield return null;
        }

        Destroy(gameObject);
    }

    private static Vector3 GetRandomCircleOffset(float radius)
    {
        if (radius <= 0f)
        {
            return Vector3.zero;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r = Mathf.Sqrt(Random.Range(0f, 1f)) * radius; // 균일 분포
        return new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
    }

    private Vector3 FindGroundPosition(Vector3 worldPos)
    {
        Vector3 rayStart = worldPos + Vector3.up * 5f;
        Ray ray = new Ray(rayStart, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, groundMask))
        {
            return hit.point;
        }

        return worldPos;
    }
}
