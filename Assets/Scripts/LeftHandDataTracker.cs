using UnityEngine;
using Leap;
using Leap.Unity;
using System.Text;
using System.Collections.Generic; // Dictionary를 사용하기 위해 추가

public class LeftHandDataTracker : MonoBehaviour
{
    [Header("왼손 모델 연결")]
    [Tooltip("Hierarchy 뷰에 있는 왼손 모델(예: CapsuleHand_L)을 여기에 연결해주세요.")]
    public HandModelBase leftHandModel;

    [Header("출력 설정")]
    [Tooltip("콘솔에 데이터를 출력하는 주기 (초)")]
    public float printInterval = 0.5f; // 0.5초마다 출력

    private float timer; // 다음 출력까지 남은 시간을 저장할 타이머
    private StringBuilder handDataStringBuilder = new StringBuilder();
    // 손가락 구부림 값을 순서대로 저장하기 위한 Dictionary
    private Dictionary<Finger.FingerType, float> fingerCurls = new Dictionary<Finger.FingerType, float>();

    void Update()
    {
        // 타이머를 매 프레임 감소시킴
        timer -= Time.deltaTime;

        // 타이머가 0 이하일 때만 데이터 출력 로직을 실행
        if (timer <= 0f)
        {
            // 타이머를 다시 설정
            timer = printInterval;

            // leftHandModel이 없거나 손이 추적되고 있지 않으면 아무것도 하지 않음
            if (leftHandModel == null || !leftHandModel.IsTracked)
            {
                return;
            }

            // HandModelBase로부터 Leap.Hand 데이터를 직접 가져옴
            Leap.Hand hand = leftHandModel.GetLeapHand();

            // --- 데이터 계산 ---
            // 1. 손가락 구부림 정도를 미리 계산하여 Dictionary에 저장
            foreach (Finger finger in hand.fingers)
            {
                Bone proximal = finger.Proximal;
                Bone intermediate = finger.Intermediate;
                Bone distal = finger.Distal;

                float angle1 = Vector3.Angle(proximal.Direction, intermediate.Direction);
                float angle2 = Vector3.Angle(intermediate.Direction, distal.Direction);
                fingerCurls[finger.Type] = angle1 + angle2;
            }

            // --- 한 줄로 출력할 문자열 생성 ---
            handDataStringBuilder.Clear(); // StringBuilder 초기화

            // 1. 회전 값 추가
            Vector3 rot = hand.Rotation.eulerAngles;
            handDataStringBuilder.AppendFormat("왼손 회전(X,Y,Z): ({0:F1}, {1:F1}, {2:F1}) | ", rot.x, rot.y, rot.z);

            // 2. 손가락 구부림 값을 '엄지, 검지, 중지, 약지, 소지' 순서로 추가
            handDataStringBuilder.AppendFormat("왼손 구부림(엄,검,중,약,소): ({0:F1}, {1:F1}, {2:F1}, {3:F1}, {4:F1})",
                fingerCurls[Finger.FingerType.THUMB],
                fingerCurls[Finger.FingerType.INDEX],
                fingerCurls[Finger.FingerType.MIDDLE],
                fingerCurls[Finger.FingerType.RING],
                fingerCurls[Finger.FingerType.PINKY]);

            // 최종적으로 완성된 한 줄의 문자열을 콘솔에 출력
            Debug.Log(handDataStringBuilder.ToString());
        }
    }
}