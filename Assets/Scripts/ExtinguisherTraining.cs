using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExtinguisherTraining : MonoBehaviour
{
    // 훈련 단계를 구분하기 위한 열거형(enum)
    public enum TrainingState
    {
        Idle, // 시작 전
        ExtinguisherHeld, // 소화기 잡음
        PinPulled, // 안전핀 뽑음
        HoseHeld, // 호스 잡음
        Spraying // 분사 중
    }

    [Header("오브젝트 연결")]
    public HandControllerL leftHand;
    public HandControllerR rightHand;
    public Transform extinguisher; // 소화기 최상위 오브젝트
    public Transform pin; // 안전핀 오브젝트
    public Transform hose; // 호스 오브젝트 (회전시킬 부분)
    public Transform lever; // 레버 오브젝트
    public ParticleSystem sprayVFX; // 분사 파티클 이펙트
    public GameObject fireVFX; // 꺼야 할 불 이펙트

    private Vector3 holdingPositionOffset = new Vector3(0.002556f, 0.000843f, 0.002155f);
    private Vector3 holdingRotationOffset = new Vector3(49.231f, -188.648f, -70.264f);
    private Vector3 holdingScale = new Vector3(0.015f, 0.015f, 0.015f);

    [Header("설정값")]
    public float handMoveSpeed = 2f; // 손이 목표물로 움직이는 속도
    public float pinPullSpeed = 1f; // 핀이 빠지는 속도

    [Header("개발용 테스트 설정")]
    [Tooltip("체크하면 장갑 대신 키보드로 단계를 진행할 수 있습니다.")]
    public bool isDebugMode = false;

    // --- 내부 변수 ---
    private TrainingState currentState = TrainingState.Idle;
    private Quaternion initialLeverRotation;
    private Vector3 initialPinPosition;

    void Start()
    {
        // 시작 시 초기값 저장 및 상태 설정
        initialLeverRotation = lever.localRotation;
        initialPinPosition = pin.localPosition;
        sprayVFX.Stop();

        // 3. 소화기를 오른손 위치에 맞게 초기 배치
        extinguisher.position = new Vector3(0.125f, 1.16f, 1.468f);
        extinguisher.rotation = Quaternion.Euler(0f, -277.85f, 0f);
        extinguisher.localScale = new Vector3(1.5f, 1.5f, 1.5f);
    }

    void Update()
    {
        // ★ 디버그 모드가 켜져 있으면 키보드 입력으로 단계를 제어
        if (isDebugMode)
        {
            HandleDebugInput();
            return; // 디버그 모드일때는 아래의 실제 장갑 로직을 실행하지 않음
        }

        // 각 상태에 따라 실행할 로직을 관리
        switch (currentState)
        {
            case TrainingState.Idle:
                // 3. 오른손으로 주먹을 쥐면 소화기를 잡음
                if (rightHand.isGripping)
                {
                    extinguisher.SetParent(rightHand.transform);
                    extinguisher.localPosition = holdingPositionOffset;
                    extinguisher.localRotation = Quaternion.Euler(holdingRotationOffset);
                    extinguisher.localScale = holdingScale;
                    Debug.Log("소화기를 잡았습니다. 다음 단계로 이동합니다.");
                    currentState = TrainingState.ExtinguisherHeld;

                    // 4. 왼손을 안전핀 쪽으로 서서히 이동 시작
                    StartCoroutine(MoveObjectSmoothly(leftHand.transform, pin.position, handMoveSpeed));
                }
                break;

            case TrainingState.ExtinguisherHeld:
                // 4. 왼손이 주먹을 쥐고 손목을 돌리면 안전핀 뽑기
                if (leftHand.isGripping) // 실제로는 자이로 회전값도 조건에 추가해야 함
                {
                    Debug.Log("안전핀을 뽑았습니다. 다음 단계로 이동합니다.");
                    currentState = TrainingState.PinPulled;
                    StartCoroutine(PullPinAndMoveToHose());
                }
                break;

            case TrainingState.PinPulled:
                // 5. 왼손이 주먹을 쥐면 호스를 잡음
                if (leftHand.isGripping)
                {
                    Debug.Log("호스를 잡았습니다. 다음 단계로 이동합니다.");
                    currentState = TrainingState.HoseHeld;
                }
                break;

            case TrainingState.HoseHeld:
                // 5. 호스 방향을 왼손 손목 회전값에 따라 조절
                hose.rotation = leftHand.currentRotation;

                // 6. 오른손 버튼을 누르면 분사 (안전핀이 뽑혔을 때만)
                if (rightHand.buttonPressed)
                {
                    Debug.Log("분사를 시작합니다.");
                    currentState = TrainingState.Spraying;
                    lever.localRotation = initialLeverRotation * Quaternion.Euler(20, 0, 0); // 레버 누름
                    sprayVFX.Play();
                }
                break;

            case TrainingState.Spraying:
                // 호스 방향 조절은 계속
                hose.rotation = leftHand.currentRotation;

                // 오른손 버튼을 떼면 분사 중지
                if (!rightHand.buttonPressed)
                {
                    Debug.Log("분사를 중지합니다.");
                    currentState = TrainingState.HoseHeld;
                    lever.localRotation = initialLeverRotation; // 레버 원위치
                    sprayVFX.Stop();
                }
                break;
        }
    }

    // 오브젝트를 목표 위치로 부드럽게 이동시키는 코루틴
    IEnumerator MoveObjectSmoothly(Transform obj, Vector3 targetPosition, float speed)
    {
        while (Vector3.Distance(obj.position, targetPosition) > 0.01f)
        {
            obj.position = Vector3.Lerp(obj.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        obj.position = targetPosition; // 정확한 위치로 보정
    }

    // 핀을 뽑고 손을 호스로 이동시키는 코루틴
    IEnumerator PullPinAndMoveToHose()
    {
        Vector3 pinPullTarget = pin.position + pin.right * -0.2f; // 핀을 왼쪽으로 20cm 빼냄

        // 핀이 서서히 빠지는 모션
        while (Vector3.Distance(pin.position, pinPullTarget) > 0.01f)
        {
            pin.position = Vector3.Lerp(pin.position, pinPullTarget, pinPullSpeed * Time.deltaTime);
            yield return null;
        }
        pin.gameObject.SetActive(false); // 핀 비활성화

        // 5. 핀이 다 빠지면 왼손을 호스 쪽으로 이동
        StartCoroutine(MoveObjectSmoothly(leftHand.transform, hose.position, handMoveSpeed));
    }
    
    // ★★★ 디버그용 키보드 입력을 처리하는 함수 ★★★
    private void HandleDebugInput()
    {
        // Keyboard.current가 null이 아닌지 확인 (키보드가 연결되어 있는지)
        if (Keyboard.current == null) return;

        // 숫자 키 1: 소화기 잡기
        if (Keyboard.current.digit1Key.wasPressedThisFrame && currentState == TrainingState.Idle)
        {
            extinguisher.SetParent(rightHand.transform);
            extinguisher.localPosition = holdingPositionOffset;
            extinguisher.localRotation = Quaternion.Euler(holdingRotationOffset);
            Debug.Log(" [디버그] 소화기를 잡았습니다.");
            currentState = TrainingState.ExtinguisherHeld;
            StartCoroutine(MoveObjectSmoothly(leftHand.transform, pin.position, handMoveSpeed));
        }

        // 숫자 키 2: 안전핀 뽑기
        if (Keyboard.current.digit2Key.wasPressedThisFrame && currentState == TrainingState.ExtinguisherHeld)
        {
            Debug.Log(" [디버그] 안전핀을 뽑았습니다.");
            currentState = TrainingState.PinPulled;
            StartCoroutine(PullPinAndMoveToHose());
        }

        // 숫자 키 3: 호스 잡기
        if (Keyboard.current.digit3Key.wasPressedThisFrame && currentState == TrainingState.PinPulled)
        {
            Debug.Log(" [디버그] 호스를 잡았습니다.");
            currentState = TrainingState.HoseHeld;
        }

        // 숫자 키 4: 누르고 있으면 분사, 떼면 중지
        if (currentState == TrainingState.HoseHeld || currentState == TrainingState.Spraying)
        {
            // 키를 누르는 순간
            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                Debug.Log(" [디버그] 분사를 시작합니다.");
                currentState = TrainingState.Spraying;
                lever.localRotation = initialLeverRotation * Quaternion.Euler(20, 0, 0);
                sprayVFX.Play();
            }
            // 키를 떼는 순간
            else if (Keyboard.current.digit4Key.wasReleasedThisFrame)
            {
                Debug.Log(" [디버그] 분사를 중지합니다.");
                currentState = TrainingState.HoseHeld;
                lever.localRotation = initialLeverRotation;
                sprayVFX.Stop();
            }
        }

        // 호스를 잡고 있거나 분사 중일 때, 마우스 움직임으로 호스 방향 조절
        if ((currentState == TrainingState.HoseHeld || currentState == TrainingState.Spraying) && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float rotX = mouseDelta.y * -0.1f; // 감도 조절 및 Y축 반전
            float rotY = mouseDelta.x * 0.1f;  // 감도 조절
            hose.Rotate(Vector3.left, rotX, Space.Self);
            hose.Rotate(Vector3.up, rotY, Space.World);
        }
    }
}