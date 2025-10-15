using UnityEngine;

public class SessionManager : MonoBehaviour
{
    // Instance를 static으로 선언하여 다른 스크립트에서 쉽게 접근 가능
    public static SessionManager Instance;

    // 씬을 넘나들며 저장할 데이터
    public string employeeID;

    void Awake()
    {
        // SessionManager가 아직 없다면, 이것을 유일한 인스턴스로 만들고 파괴되지 않게 함
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // 이미 SessionManager가 존재한다면, 새로 생긴 것은 파괴
        else
        {
            Destroy(gameObject);
        }
    }
}