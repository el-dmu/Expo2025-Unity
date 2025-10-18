// SwipeDetector.cs

using UnityEngine;
using Leap;
using Leap.Unity;
using UnityEngine.Events; // UnityEvent를 사용하기 위해 추가

public class SwipeDetector : MonoBehaviour
{
    [Tooltip("연결하지 않으면 스크립트가 자동으로 씬에서 LeapServiceProvider를 찾습니다.")]
    public LeapServiceProvider leapProvider;

    [Header("Swipe Settings")]
    [Tooltip("더 민감하게 반응하도록 값을 낮춥니다. (예: 0.05)")]
    public float swipeThreshold = 0.05f; // 값을 낮춰서 더 민감하게 설정
    [Tooltip("스와이프 방향으로 인정할 축의 비중")]
    public float swipeDirectionThreshold = 0.7f;
    [Tooltip("스와이프 후 다음 감지까지의 대기 시간")]
    public float swipeCooldown = 1.0f;

    [Header("Events")]
    [Tooltip("왼쪽 스와이프가 감지되면 호출될 이벤트입니다.")]
    public UnityEvent OnLeftSwipe; // UIManager와 연결할 이벤트

    private Vector3 handStartPosition;
    private bool isTracking = false;
    private float lastSwipeTime;

    void Start()
    {
        if (leapProvider == null)
        {
            leapProvider = FindObjectOfType<LeapServiceProvider>();
            if (leapProvider == null)
            {
                Debug.LogError("씬에 LeapServiceProvider가 없습니다!");
                this.enabled = false;
                return;
            }
        }
    }

    void Update()
    {
        if (Time.time < lastSwipeTime + swipeCooldown) return;

        Hand mainHand = null;
        // 현재 프레임에서 오른손을 찾습니다.
        foreach (Hand hand in leapProvider.CurrentFrame.Hands)
        {
            if (hand.IsRight)
            {
                mainHand = hand;
                break;
            }
        }

        if (mainHand != null)
        {
            if (!isTracking)
            {
                handStartPosition = mainHand.PalmPosition;
                isTracking = true;
            }
            else
            {
                Vector3 movement = mainHand.PalmPosition - handStartPosition;

                if (movement.magnitude > swipeThreshold)
                {
                    Vector3 direction = movement.normalized;

                    // 왼쪽 스와이프만 감지 (direction.x가 음수이고, x축 움직임이 지배적일 때)
                    if (direction.x > 0 && Mathf.Abs(direction.x) > swipeDirectionThreshold)
                    {
                        Debug.Log("왼쪽으로 스와이프 감지!");
                        OnLeftSwipe.Invoke(); // 등록된 이벤트를 호출!
                        ResetSwipe();
                    }
                    else
                    {
                        // 유효한 스와이프가 아니면 시작 위치를 현재 위치로 리셋
                        handStartPosition = mainHand.PalmPosition;
                    }
                }
            }
        }
        else
        {
            if (isTracking) isTracking = false;
        }
    }

    private void ResetSwipe()
    {
        isTracking = false;
        lastSwipeTime = Time.time;
    }
}