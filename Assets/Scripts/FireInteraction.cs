using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FireInteraction : MonoBehaviour
{
    [Header("진화 설정")]
    public float timeToExtinguish = 5.0f; // 불을 끄는 데 필요한 시간 (초)
    public float sprayGracePeriod = 0.2f; // 분사가 끊겨도 타이머를 유지할 유예 시간 (초)

    private ParticleSystem fireParticleSystem;
    private bool isExtinguished = false;
    private float sprayTimer = 0.0f; // 지속적으로 분사된 시간을 측정하는 타이머
    private float lastParticleHitTime; // 마지막으로 파티클이 충돌한 시간을 기록

    void Start()
    {
        fireParticleSystem = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // 불이 아직 꺼지지 않았다면
        if (!isExtinguished)
        {
            // 마지막 충돌 시간으로부터 유예 시간이 지나지 않았다면 (즉, 계속 분사 중이라면)
            if (Time.time - lastParticleHitTime < sprayGracePeriod)
            {
                // 타이머 시간을 증가시킴
                sprayTimer += Time.deltaTime;
                Debug.Log("진화 중... 타이머: " + sprayTimer.ToString("F2"));

                // 타이머가 목표 시간에 도달했다면 불을 끄는 함수 호출
                if (sprayTimer >= timeToExtinguish)
                {
                    ExtinguishFire();
                }
            }
            else
            {
                // 유예 시간이 지났다면 (분사가 멈췄다면) 타이머를 초기화
                sprayTimer = 0f;
            }
        }
    }

    /// <summary>
    /// 다른 파티클이 내 '물리적 콜라이더'에 부딪혔을 때 호출됩니다.
    /// </summary>
    void OnParticleCollision(GameObject other)
    {
        if (isExtinguished)
        {
            return;
        }
        
        // 파티클이 충돌할 때마다 마지막 충돌 시간을 현재 시간으로 갱신
        lastParticleHitTime = Time.time;
    }

    // 불을 끄는 함수
    private void ExtinguishFire()
    {
        if (isExtinguished)
        {
            return;
        }

        Debug.Log(timeToExtinguish + "초 동안 분사 완료! 불을 끕니다.");
        isExtinguished = true;
        fireParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}