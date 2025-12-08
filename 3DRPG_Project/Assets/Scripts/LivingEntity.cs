using UnityEngine;
using System;

public abstract class LivingEntity : MonoBehaviour, IDamageable
{
    [Header("Status")]
    public int maxHp = 100;
    public int currentHp;
    
    public bool IsDead { get; protected set; }

    // 사망 시 발동할 이벤트
    public event Action OnDeath;

    protected virtual void Awake()
    {
        currentHp = maxHp;
    }

    protected virtual void OnEnable()
    {
        IsDead = false;
        currentHp = maxHp;
    }

    public virtual void TakeDamage(int damage)
    {
        if (IsDead) 
        {
            return;
        }

        currentHp -= damage;
        //Debug.Log($"{name}이 {damage} 데미지 입음. 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    public virtual void Die()
    {
        if (IsDead) 
        {
            return;
        }

        IsDead = true;
        OnDeath?.Invoke();
        //Debug.Log($"{name} 사망");
    }
}
