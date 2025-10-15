using UnityEngine;

public class FireExtinguisherController : MonoBehaviour
{
    [Header("오브젝트 연결")]
    public ParticleSystem sprayParticles;
    public InteractionTarget safetyPinTarget;
    public InteractionTarget leverTarget;

    [Header("분사 설정")]
    [Tooltip("손을 떼도 분사가 유지되는 유예 시간 (초)")]
    public float sprayGraceTime = 0.3f; // 0.3초의 유예 시간 설정

    // 내부 상태 변수
    private bool isPinPulled = false;
    private float releaseTimer = 0f; // 레버를 놓았을 때 카운트다운할 타이머

    void Start()
    {
        if (sprayParticles != null)
        {
            sprayParticles.Stop();
        }
    }

    void Update()
    {
        HandleInteractions();
    }

    void HandleInteractions()
    {
        // 1. 안전핀 뽑기 로직 (변경 없음)
        if (!isPinPulled && safetyPinTarget.IsGrabbed)
        {
            PullPin();
        }

        // 2. 레버 누르기 및 분사 로직 (수정됨)
        // 안전핀이 뽑힌 상태에서만 레버 로직을 처리
        if (isPinPulled)
        {
            // 조건: 레버를 '쥐고 있을' 때
            if (leverTarget.IsGrabbed)
            {
                // 유예 시간 타이머를 최대로 계속 재설정
                releaseTimer = sprayGraceTime;
            }
            // 조건: 레버를 '쥐고 있지 않을' 때
            else
            {
                // 타이머를 서서히 감소시킴
                releaseTimer -= Time.deltaTime;
            }
        }
        else
        {
            // 안전핀이 뽑히지 않았다면 타이머는 항상 0
            releaseTimer = 0f;
        }

        // 최종적으로 타이머가 0보다 클 때만 분사 상태로 간주
        bool isLeverPressed = releaseTimer > 0;
        
        ControlParticles(isLeverPressed);
    }

    void ControlParticles(bool isActive)
    {
        if (isActive)
        {
            if (!sprayParticles.isPlaying)
            {
                sprayParticles.Play();
            }
        }
        else
        {
            if (sprayParticles.isPlaying)
            {
                sprayParticles.Stop();
            }
        }
    }

    void PullPin()
    {
        isPinPulled = true;
        Debug.Log("안전핀을 뽑았습니다.");

        if (safetyPinTarget != null)
        {
            safetyPinTarget.gameObject.SetActive(false);
        }
    }
}