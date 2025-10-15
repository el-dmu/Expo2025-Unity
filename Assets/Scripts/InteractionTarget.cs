using UnityEngine;
using Leap;
using Leap.Unity;   

public class InteractionTarget : MonoBehaviour
{
    // ★★★ 어떤 손의 입력을 받을지 선택하는 옵션 추가 ★★★
    public enum Handedness { Any, Left, Right }
    [Header("손 설정")]
    [Tooltip("상호작용을 허용할 손을 선택합니다. (Any = 양손 모두)")]
    public Handedness allowedHand = Handedness.Any;
    // ★★★ 여기까지 ★★★

    // 외부에서 '쥐었는지' 상태를 읽을 수 있는 변수 (프로퍼티)
    public bool IsGrabbed { get; private set; } = false;

     private LeapProvider leapProvider;
    private bool isHandInside = false; // 손이 트리거 안에 있는지 체크

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
                if (hand.GrabStrength > 0.8f)
                {
                    // ★★★ 조건 2: 허용된 손이 맞는가? ★★★
                    if (allowedHand == Handedness.Left && !hand.IsLeft)
                    {
                        continue; // 왼손 전용인데 오른손이 들어왔다면 무시
                    }
                    if (allowedHand == Handedness.Right && !hand.IsRight)
                    {
                        continue; // 오른손 전용인데 왼손이 들어왔다면 무시
                    }
                    // ★★★ 여기까지 ★★★
                    
                    isCurrentlyGrabbing = true;
                    Debug.Log("움켜쥔 정도" + hand.GrabStrength);
                    break; // 한 손이라도 쥐고 있으면 true
                }
            }
        }
        IsGrabbed = isCurrentlyGrabbing;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 들어온 것이 Leap Motion '손'이 맞는지 확인
        if (other.GetComponentInParent<HandModelBase>() != null)
        {
            isHandInside = true;
        }
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