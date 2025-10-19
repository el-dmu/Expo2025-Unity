using UnityEngine;
using System.Collections;
using Leap;
using Leap.Unity;
using System;

public class FireExtinguisherController : MonoBehaviour
{
    // --- 이벤트 선언 ---
    public static event Action OnGrabbed;
    public static event Action OnPinPulled;
    public static event Action OnHoseGrabbed;
    public static event Action OnLeverSqueezed;

    // --- Inspector 연결 변수 ---
    [Header("잡기(Grab) 설정")]
    public Transform rightHandPalm;
    public Transform handleAnchor;
    public string rightHandTag = "RightHand";
    [Range(0.7f, 1.0f)]
    public float grabThreshold = 0.9f;

    [Header("오브젝트 연결")]
    public ParticleSystem sprayParticles;
    public InteractionTargetSeal safetyPinTarget;
    public Transform pin;
    public Transform lever;
    public InteractionTargetLever leverTarget;
    public GrabbableHorn hoseTarget;

    [Header("분사 설정")]
    public float sprayGraceTime = 0.3f;

    // --- 내부 상태 변수 ---
    private Rigidbody rb;
    private bool isHeld = false;
    private bool isPinPulled = false;
    private bool isHoseGrabbed = false;
    private bool leverSqueezedEventFired = false;
    private float releaseTimer = 0f;
    private Quaternion initialLeverRotation;
    private bool isHandInside = false;
    private Hand currentHand;
    private bool wasGrabbingLastFrame = false;

    // '물리적 잠금' 변수
    private bool isGrabbingLocked = false;
    private bool isPinPullLocked = false;
    private bool isHoseGrabLocked = false;
    private bool isLeverSqueezeLocked = false;

    public bool IsHeld => isHeld;
    public bool IsPinPulled => isPinPulled;
    public bool IsHoseGrabbed => isHoseGrabbed;

    // 각 행동을 잠그거나 해제할 함수들
    public void LockGrabbing(bool lockState)
    {
        isGrabbingLocked = lockState;
        Debug.Log($"<color={(lockState ? "red" : "green")}>[Controller] 잡기(Grab) 잠금: {lockState}</color>");
    }
    public void LockPinPull(bool lockState)
    {
        isPinPullLocked = lockState;
        Debug.Log($"<color={(lockState ? "red" : "green")}>[Controller] 안전핀(Pin) 잠금: {lockState}</color>");
    }

    // [★★★ 핵심 수정 ★★★]
    // 호스 잠금이 'isGrabbable' 상태를 직접 제어하도록 수정
    public void LockHoseGrab(bool lockState)
    {
        isHoseGrabLocked = lockState;
        if (hoseTarget != null)
        {
            // 잠금(true)이면 isGrabbable = false
            // 해제(false)이면 isGrabbable = true
            hoseTarget.isGrabbable = !lockState;
        }
        Debug.Log($"<color={(lockState ? "red" : "green")}>[Controller] 호스(Hose) 잠금: {lockState} (isGrabbable: {!lockState})</color>");
    }
    public void LockLeverSqueeze(bool lockState)
    {
        isLeverSqueezeLocked = lockState;
        Debug.Log($"<color={(lockState ? "red" : "green")}>[Controller] 레버(Lever) 잠금: {lockState}</color>");
    }
    /// <summary>
    /// 소화기를 강제로 손에서 놓습니다. (훈련 종료 시 사용)
    /// </summary>
    public void ForceRelease()
    {
        if (!isHeld) return; // 이미 놓은 상태면 무시

        isHeld = false;

        // Rigidbody를 다시 물리 오브젝트로 되돌립니다.
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true; // 중력 다시 적용
        }

        // 활성화된 인터랙션 타겟들을 비활성화합니다.
        safetyPinTarget.enabled = false;
        leverTarget.enabled = false;

        // 파티클이 켜져있다면 강제로 끕니다.
        ControlParticles(false);

        Debug.Log("<color=orange>[Controller] 소화기를 강제로 놓았습니다.</color>");
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
        }

        if (sprayParticles != null) sprayParticles.Stop();

        // [수정] Start에서 hoseTarget.isGrabbable = false로 명시적 설정
        if (hoseTarget != null) hoseTarget.isGrabbable = false;

        initialLeverRotation = lever.localRotation;
        safetyPinTarget.enabled = false;
        leverTarget.enabled = false;
    }

    void Update()
    {
        HandleGrabbing();

        if (isHeld)
        {
            transform.position = rightHandPalm.position - (transform.rotation * handleAnchor.localPosition);
            HandleInteractions();

            // [수정] !isHoseGrabLocked 체크는 HandleInteractions로 이동할 필요 없이 여기서 계속 수행
            if (isPinPulled && !isHoseGrabbed && !isHoseGrabLocked && hoseTarget != null && hoseTarget.isGrabbed)
            {
                isHoseGrabbed = true;
                OnHoseGrabbed?.Invoke();
                Debug.Log("호스를 잡았습니다!");
            }
        }
    }

    void HandleGrabbing()
    {
        if (isHeld || isGrabbingLocked) return;

        if (!isHandInside)
        {
            wasGrabbingLastFrame = false;
            return;
        }
        bool isGrabbingNow = currentHand != null && currentHand.GrabStrength >= grabThreshold;
        if (isGrabbingNow && !wasGrabbingLastFrame)
        {
            Grab();
        }
        wasGrabbingLastFrame = isGrabbingNow;
    }

    private void Grab()
    {
        isHeld = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        safetyPinTarget.enabled = true;
        leverTarget.enabled = true;
        Debug.Log("소화기를 잡았습니다!");
        OnGrabbed?.Invoke();
    }

    void HandleInteractions()
    {
        if (!isPinPulled && !isPinPullLocked && safetyPinTarget.IsGrabbed)
        {
            PullPin();
        }

        if (isPinPulled && isHoseGrabbed)
        {
            if (!isLeverSqueezeLocked && leverTarget.IsGrabbed)
            {
                releaseTimer = sprayGraceTime;
            }
            else
            {
                releaseTimer -= Time.deltaTime;
            }
        }
        else
        {
            releaseTimer = 0f;
        }

        bool isLeverPressed = releaseTimer > 0;
        ControlParticles(isLeverPressed);

        if (isLeverPressed && !leverSqueezedEventFired)
        {
            leverSqueezedEventFired = true;
            OnLeverSqueezed?.Invoke();
        }
    }

    void ControlParticles(bool isActive)
    {
        if (isActive)
        {
            if (!sprayParticles.isPlaying)
            {
                lever.localRotation = initialLeverRotation * Quaternion.Euler(20, 0, 0);
                sprayParticles.Play();
            }
        }
        else
        {
            if (sprayParticles.isPlaying)
            {
                lever.localRotation = initialLeverRotation;
                sprayParticles.Stop();
            }
        }
    }

    void PullPin()
    {
        if (isPinPulled) return;
        isPinPulled = true;
        Debug.Log("안전핀을 뽑았습니다.");
        OnPinPulled?.Invoke();
        StartCoroutine(PullPinAnimation());
    }

    // [★★★ 핵심 수정 ★★★]
    // PullPinAnimation에서 hoseTarget.isGrabbable = true; 코드를 삭제
    IEnumerator PullPinAnimation()
    {
        Vector3 pinPullTarget = pin.position + pin.right * 0.2f;
        while (Vector3.Distance(pin.position, pinPullTarget) > 0.01f)
        {
            pin.position = Vector3.Lerp(pin.position, pinPullTarget, 1f * Time.deltaTime);
            yield return null;
        }
        pin.gameObject.SetActive(false);

        // [삭제] if (hoseTarget != null)
        // [삭제] {
        // [삭제]     hoseTarget.isGrabbable = true;
        // [삭제] }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(rightHandTag))
        {
            HandModelBase handModel = other.GetComponentInParent<HandModelBase>();
            if (handModel != null)
            {
                isHandInside = true;
                currentHand = handModel.GetLeapHand();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(rightHandTag))
        {
            isHandInside = false;
            currentHand = null;
        }
    }
}