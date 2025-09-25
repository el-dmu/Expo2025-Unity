using UnityEngine;

public class SmoothWalkingMover : MonoBehaviour {
    public Transform[] waypoints;
    public float moveSpeed = 2f;
    public float swayAmount = 0.3f; // 카메라 흔들림 정도
    public float swayFrequency = 4f; // 흔들림 속도

    private int currentIndex = 0;
    private Vector3 velocity = Vector3.zero;
    private Transform cameraTransform;

    void Start() {
        cameraTransform = Camera.main.transform;
    }

    void Update() {
        if (waypoints.Length == 0 || currentIndex >= waypoints.Length) return;

        Vector3 targetPos = waypoints[currentIndex].position;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, 0.3f, moveSpeed);

        // 다음 waypoint로 전환
        if (Vector3.Distance(transform.position, targetPos) < 0.1f) {
            currentIndex++;
        }

        // 걷는 듯한 카메라 흔들림
        if (cameraTransform != null) {
            float sway = Mathf.Sin(Time.time * swayFrequency) * swayAmount;
            Vector3 camLocalPos = cameraTransform.localPosition;
            camLocalPos.y = sway; // 기본 높이 + 흔들림
            cameraTransform.localPosition = camLocalPos;
        }
    }
}
