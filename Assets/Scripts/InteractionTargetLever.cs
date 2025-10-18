using UnityEngine;
using Leap;
using Leap.Unity;

public class InteractionTargetLever : MonoBehaviour
{
    public enum Handedness { Any, Left, Right }
    public Handedness allowedHand = Handedness.Right;

    // 외부에서 '쥐었는지' 상태를 읽을 수 있는 변수 (프로퍼티)
    public bool IsGrabbed { get; private set; } = false;

    private LeapProvider leapProvider;
    private bool isHandInside = true; // 손이 트리거 안에 있는지 체크

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        leapProvider = FindObjectOfType<LeapProvider>();
        if (leapProvider == null)
        {
            Debug.LogError("씬에서 LeapProvider를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        // 손이 트리거 안에 있을 때만 쥐는 상태를 매 프레임 체크
        if (isHandInside)
        {
            CheckGrabState();
        }
    }

    // 현재 손이 쥐고 있는지 상태를 확인하고 IsGrabbed 값을 업데이트하는 함수
    private void CheckGrabState()
    {
        bool isCurrentlyGrabbing = false;
        if (leapProvider != null)
        {
            foreach (var hand in leapProvider.CurrentFrame.Hands)
            {
                if (hand.GrabStrength > 0.95f)
                {
                    if (!hand.IsRight)
                    {
                        continue; // 오른손 전용인데 왼손이 들어왔다면 무시
                    }

                    isCurrentlyGrabbing = true;
                    break; // 한 손이라도 쥐고 있으면 true
                }
            }
        }
        IsGrabbed = isCurrentlyGrabbing;
    }

    private void OnTriggerExit(Collider other)
    {
        // 나간 것이 Leap Motion '손'이 맞는지 확인
        if (other.GetComponentInParent<HandModelBase>() != null)
        {
            isHandInside = false;
            IsGrabbed = false; // 손이 나가면 '쥠' 상태도 자동으로 해제
        }
    }
}