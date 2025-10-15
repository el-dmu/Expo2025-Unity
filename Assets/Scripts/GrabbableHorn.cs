using UnityEngine;
using Leap;
using Leap.Unity;

[RequireComponent(typeof(Collider))]
public class GrabbableHorn : MonoBehaviour
{
    [Header("하이라이트 설정")]
    [Tooltip("손이 가까이 왔을 때 적용할 빛나는 재질")]
    public Material highlightMaterial;

    private Material originalMaterial; // 원래 재질을 저장할 변수
    private Renderer hornRenderer;     // 오브젝트의 렌더러 컴포넌트

    private LeapProvider leapProvider;
    private Hand grabbingHand = null;
    private bool isGrabbed = false;
    private bool isHandInside = false; // 손이 트리거 안에 있는지 체크하는 변수

    private Quaternion rotationOffset;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        // 렌더러 컴포넌트와 원래 재질을 저장
        hornRenderer = GetComponent<Renderer>();
        if (hornRenderer != null)
        {
            originalMaterial = hornRenderer.material;
        }

        leapProvider = FindObjectOfType<LeapProvider>();
        if (leapProvider == null)
        {
            Debug.LogError("씬에 LeapProvider가 없습니다. LeapHandController rig을 확인해주세요.");
        }
    }

    void LateUpdate()
    {
        if (isGrabbed && grabbingHand != null)
        {
            if (grabbingHand.GrabStrength < 0.5f)
            {
                ReleaseHorn();
                return;
            }
            transform.rotation = grabbingHand.Rotation * rotationOffset;
        }
    }
    
    // 손이 콜라이더에 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        // 들어온 것이 Leap Motion 손인지 확인
        if (other.GetComponentInParent<HandModelBase>() != null)
        {
            isHandInside = true;
            // 아직 잡히지 않았다면 하이라이트 효과를 켭니다.
            if (!isGrabbed && highlightMaterial != null)
            {
                hornRenderer.material = highlightMaterial;
            }
        }
    }
    
    // 손이 콜라이더를 빠져나갔을 때
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<HandModelBase>() != null)
        {
            isHandInside = false;
            // 잡혀있지 않다면 무조건 원래 재질로 되돌립니다.
            if (!isGrabbed && originalMaterial != null)
            {
                hornRenderer.material = originalMaterial;
            }
        }
    }

    // 손이 콜라이더 안에 머무는 동안
    private void OnTriggerStay(Collider other)
    {
        if (isGrabbed || leapProvider == null || !isHandInside) return;
        
        if (other.GetComponentInParent<HandModelBase>() != null)
        {
            foreach (Hand hand in leapProvider.CurrentFrame.Hands)
            {
                if (hand.GrabStrength > 0.8f)
                {
                    GrabHorn(hand);
                    break;
                }
            }
        }
    }

    private void GrabHorn(Hand hand)
    {
        isGrabbed = true;
        grabbingHand = hand;

        // 잡는 데 성공하면 하이라이트를 끄고 원래 재질로 되돌립니다.
        if (originalMaterial != null)
        {
            hornRenderer.material = originalMaterial;
        }

        rotationOffset = Quaternion.Inverse(grabbingHand.Rotation) * transform.rotation;
        Debug.Log("분사구를 잡았습니다!");
    }

    private void ReleaseHorn()
    {
        if (isGrabbed)
        {
            isGrabbed = false;
            grabbingHand = null;

            // 손을 놓았을 때, 만약 손이 여전히 콜라이더 안에 있다면 다시 하이라이트를 켭니다.
            if (isHandInside && highlightMaterial != null)
            {
                hornRenderer.material = highlightMaterial;
            }
            Debug.Log("분사구를 놓았습니다.");
        }
    }
}