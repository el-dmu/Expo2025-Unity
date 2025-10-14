using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExtinguisherTraining : MonoBehaviour
{
    public enum TrainingState
    {
        Idle, ExtinguisherHeld, PinPulled, HoseHeld, Spraying
    }

    [Header("오브젝트 연결")]
    public HandControllerL leftHand;
    public HandControllerR rightHand;
    public Transform extinguisher;
    public Transform pin;
    public Transform hose;
    public Transform lever;
    public ParticleSystem sprayVFX;
    public GameObject fireVFX;
    public Transform pinGrabTarget;
    public Transform hoseGrabTarget;

    [Header("시작 위치 설정")]
    [Tooltip("소화기가 생성될 고정된 위치를 지정하는 오브젝트입니다.")]
    public Transform extinguisherSpawnPoint; // XR Origin 변수 제거, 이 변수만 남김

    private Vector3 holdingPositionOffset = new Vector3(0.002556f, 0.000843f, 0.002155f);
    private Vector3 holdingRotationOffset = new Vector3(49.231f, -188.648f, -70.264f);
    private Vector3 holdingScale = new Vector3(0.015f, 0.015f, 0.015f);

    [Header("설정값")]
    public float handMoveSpeed = 2f;
    public float pinPullSpeed = 1f;

    [Header("개발용 테스트 설정")]
    public bool isDebugMode = false;

    private TrainingState currentState = TrainingState.Idle;
    private Quaternion initialLeverRotation;
    private Vector3 initialPinPosition;
    private bool isReadyToGrabHose = false;

    void Awake()
    {
        if (isDebugMode)
        {
            if (leftHand != null) leftHand.ActivateDebugMode();
            if (rightHand != null) rightHand.ActivateDebugMode();
        }
    }

    void Start()
    {
        initialLeverRotation = lever.localRotation;
        initialPinPosition = pin.localPosition;
        sprayVFX.Stop();

        // [수정] 고정된 Spawn Point의 월드 좌표를 기준으로 소화기 위치를 설정합니다.
        if (extinguisherSpawnPoint != null)
        {
            extinguisher.position = extinguisherSpawnPoint.position;
            extinguisher.rotation = extinguisherSpawnPoint.rotation;
        }
        else
        {
            Debug.LogError("Extinguisher Spawn Point가 설정되지 않았습니다! Inspector에서 연결해주세요.");
            this.enabled = false; // 스크립트 비활성화
        }
    }
    
    // --- Update() 및 나머지 함수들은 이전과 동일 ---
    void Update()
    {
        if (isDebugMode) { HandleDebugInput(); return; }
        switch (currentState)
        {
            case TrainingState.Idle:
                if (rightHand.isGripping) {
                    extinguisher.SetParent(rightHand.transform);
                    extinguisher.localPosition = holdingPositionOffset;
                    extinguisher.localRotation = Quaternion.Euler(holdingRotationOffset);
                    extinguisher.localScale = holdingScale;
                    Debug.Log("소화기를 잡았습니다. 다음 단계로 이동합니다.");
                    currentState = TrainingState.ExtinguisherHeld;
                    StartCoroutine(MoveObjectSmoothly(leftHand.transform, pinGrabTarget.position, pinGrabTarget.rotation, handMoveSpeed));
                }
                break;
            case TrainingState.ExtinguisherHeld:
                if (leftHand.isGripping) {
                    Debug.Log("안전핀을 뽑았습니다. 다음 단계로 이동합니다.");
                    currentState = TrainingState.PinPulled;
                    isReadyToGrabHose = false; 
                    StartCoroutine(PullPinAndMoveToHose());
                }
                break;
            case TrainingState.PinPulled:
                if (!leftHand.isGripping) { isReadyToGrabHose = true; }
                if (isReadyToGrabHose && leftHand.isGripping) {
                    Debug.Log("호스를 잡았습니다. 다음 단계로 이동합니다.");
                    currentState = TrainingState.HoseHeld;
                }
                break;
            case TrainingState.HoseHeld:
                hose.rotation = Quaternion.Euler(74.3f, 50f, -30.453f);
                if (rightHand.buttonPressed) {
                    Debug.Log("분사를 시작합니다.");
                    currentState = TrainingState.Spraying;
                    lever.localRotation = initialLeverRotation * Quaternion.Euler(20, 0, 0); 
                    sprayVFX.Play();
                }
                break;
            case TrainingState.Spraying:
                hose.rotation = Quaternion.Euler(74.3f, 50f, -30.453f);
                if (!rightHand.buttonPressed) {
                    Debug.Log("분사를 중지합니다.");
                    currentState = TrainingState.HoseHeld;
                    lever.localRotation = initialLeverRotation; 
                    sprayVFX.Stop();
                }
                break;
        }
    }
    
    IEnumerator MoveObjectSmoothly(Transform obj, Vector3 targetPosition, Quaternion targetRotation, float speed) {
        while (Vector3.Distance(obj.position, targetPosition) > 0.01f || Quaternion.Angle(obj.rotation, targetRotation) > 1.0f) {
            obj.position = Vector3.Lerp(obj.position, targetPosition, speed * Time.deltaTime);
            obj.rotation = Quaternion.Slerp(obj.rotation, targetRotation, speed * Time.deltaTime);
            yield return null;  
        }
        obj.position = targetPosition;
        obj.rotation = targetRotation;
    }

    IEnumerator PullPinAndMoveToHose() {
        Vector3 pinPullTarget = pin.position + pin.right * 0.2f;
        while (Vector3.Distance(pin.position, pinPullTarget) > 0.01f) {
            pin.position = Vector3.Lerp(pin.position, pinPullTarget, pinPullSpeed * Time.deltaTime);
            yield return null;
        }
        pin.gameObject.SetActive(false);
        StartCoroutine(MoveObjectSmoothly(leftHand.transform, hoseGrabTarget.position, hoseGrabTarget.rotation, handMoveSpeed));
    }
    
    private void HandleDebugInput() {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame && currentState == TrainingState.Idle) {
            extinguisher.SetParent(rightHand.transform);
            extinguisher.localPosition = holdingPositionOffset;
            extinguisher.localRotation = Quaternion.Euler(holdingRotationOffset);
            Debug.Log(" [디버그] 소화기를 잡았습니다.");
            currentState = TrainingState.ExtinguisherHeld;
            StartCoroutine(MoveObjectSmoothly(leftHand.transform, pinGrabTarget.position, pinGrabTarget.rotation, handMoveSpeed));
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame && currentState == TrainingState.ExtinguisherHeld) {
            Debug.Log(" [디버그] 안전핀을 뽑았습니다.");
            currentState = TrainingState.PinPulled;
            StartCoroutine(PullPinAndMoveToHose());
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame && currentState == TrainingState.PinPulled) {
            Debug.Log(" [디버그] 호스를 잡았습니다.");
            currentState = TrainingState.HoseHeld;
        }
        if (currentState == TrainingState.HoseHeld || currentState == TrainingState.Spraying) {
            if (Keyboard.current.digit4Key.wasPressedThisFrame) {
                Debug.Log(" [디버그] 분사를 시작합니다.");
                currentState = TrainingState.Spraying;
                lever.localRotation = initialLeverRotation * Quaternion.Euler(20, 0, 0);
                sprayVFX.Play();
            } else if (Keyboard.current.digit4Key.wasReleasedThisFrame) {
                Debug.Log(" [디버그] 분사를 중지합니다.");
                currentState = TrainingState.HoseHeld;
                lever.localRotation = initialLeverRotation;
                sprayVFX.Stop();
            }
        }
        if ((currentState == TrainingState.HoseHeld || currentState == TrainingState.Spraying) && Mouse.current != null) {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float rotX = mouseDelta.y * -0.1f;
            float rotY = mouseDelta.x * 0.1f;
            hose.Rotate(Vector3.left, rotX, Space.Self);
            hose.Rotate(Vector3.up, rotY, Space.World);
        }
    }
}

