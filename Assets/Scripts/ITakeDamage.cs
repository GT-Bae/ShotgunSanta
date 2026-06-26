using UnityEngine;

// 피해를 입을 수 있는 모든 객체가 구현해야 할 인터페이스
public interface ITakeDamage {
    void TakeDamage(float damageAmount);
}