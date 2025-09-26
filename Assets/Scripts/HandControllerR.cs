using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class HandControllerR : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("장치 관리자에서 확인한 오른손 COM 포트 번호")]
    public string portName = "COM11";

    [Header("3D 모델 관절 연결")]
    [Tooltip("오른손 손목 또는 손바닥에 해당하는 루트 오브젝트")]
    public Transform handRoot;
    [Tooltip("제어할 5개의 손가락 관절 (엄지부터 새끼 순서로)")]
    public Transform[] fingerJoints = new Transform[5];

    [Header("움직임 캘리브레이션")]
    [Tooltip("각 손가락별로 플렉스 센서가 펴졌을 때의 값 (엄지부터)")]
    public int[] flexMin = new int[5];
    [Tooltip("각 손가락별로 플렉스 센서가 완전히 굽혀졌을 때의 값 (엄지부터)")]
    public int[] flexMax = new int[5];
    [Tooltip("손가락이 최대로 굽혀질 각도")]
    public float maxFingerAngle = 90.0f;


    [Header("데이터 외부 공개")]
    [Tooltip("DataManager가 현재 손의 회전값을 가져가기 위한 변수")]
    public Quaternion currentRotation;

    [Tooltip("DataManager가 현재 손가락 굽힘 값을 가져가기 위한 변수 (0~1)")]
    public float[] fingerValues = new float[5];


    // --- 내부 변수들 ---
    private SerialPort serialPort;
    private Thread dataReadThread;
    private bool isRunning = false;
    private string receivedString;
    private readonly object lockObject = new object();

    private int[] flexVals = new int[5];
    private float pitch = 0, roll = 0, yaw = 0;

    public bool buttonPressed = false; // 버튼을 눌렀는지 여부
    public bool isGripping = false; // 주먹을 쥐었는지 여부
    private bool _isInDebugMode = false; // ★ 내부용 디버그 상태 변수 추가

    // ★★★ 외부에서 호출할 함수 추가 ★★★
    public void ActivateDebugMode()
    {
        _isInDebugMode = true;
        Debug.Log($"[오른손] 디버그 모드가 활성화되어 포트 연결을 건너뜁니다.");
    }

    void Start()
    {   
         if (_isInDebugMode)
        {
            isRunning = false;
            return;
        }
        
        try
        {
            serialPort = new SerialPort(portName, 115200) { ReadTimeout = 200 };
            serialPort.Open();
            isRunning = true;
            dataReadThread = new Thread(ReadDataThread);
            dataReadThread.IsBackground = true;
            dataReadThread.Start();
            Debug.Log($"✅ [오른손] Bluetooth-Serial 포트 연결 성공: {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [오른손] Bluetooth-Serial 포트 연결 실패: {e.Message}");
        }
    }

    void Update()
    {
        string dataToProcess = null;
        lock (lockObject)
        {
            if (receivedString != null)
            {
                dataToProcess = receivedString;
                receivedString = null;
            }
        }

        if (dataToProcess != null)
        {
            ParseData(dataToProcess);
        }

        UpdateHandModel();
    }

    private void ReadDataThread()
    {
        while (isRunning && serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string line = serialPort.ReadLine();
                lock (lockObject)
                {
                    receivedString = line;
                }
            }
            catch (TimeoutException) { /* 무시 */ }
            catch (Exception e)
            {
                if (isRunning) Debug.LogWarning($"⚠ [오른손] 데이터 수신 오류: {e.Message}");
            }
        }
    }

    private void ParseData(string data)
    {
        try
        {
            data = data.TrimStart(',');
            
            string[] parts = data.Split(',');
            if (parts.Length >= 8)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (i < parts.Length)
                    {
                        flexVals[i] = int.Parse(parts[i]);
                    }
                }

                pitch = float.Parse(parts[5]);
                roll = float.Parse(parts[6]);
                yaw = float.Parse(parts[7]);

                if (parts[11] == "1")
                {
                    buttonPressed = true;
                }
                else
                {
                    buttonPressed = false;
                }

                if (flexVals[0] > 2950 && flexVals[1] > 3750 &&
                    flexVals[2] > 3850 && flexVals[3] > 3700 &&
                    flexVals[4] > 150)
                {
                    isGripping = true;
                }
                else
                {
                    isGripping = false;
                }
                // Debug.Log($"주먹을 쥐었나요?: {isGripping}\n flex1: {flexVals[0] > 2980}, flex2: {flexVals[1] > 3870}, flex3: {flexVals[2] > 3930}, flex4: {flexVals[3] > 3770}, flex5: {flexVals[4] > 150}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠ [오른손] 데이터 파싱 실패: {data} | 오류: {e.Message}");
        }
    }

    private void UpdateHandModel()
    {
        if (handRoot != null)
            {
                
                // float finalWristX = 57.645f - pitch;
                // float finalWristY = 135.26f - yaw;
                // float finalWristZ = -69.727f + roll; 

                Quaternion baseRotation = Quaternion.Euler(57.645f, -135.26f, 69.727f);

                // 2. 센서 값으로 실시간 회전값을 만듭니다. (축 순서나 부호는 모델에 맞게 조절해야 할 수 있습니다)
                //Quaternion sensorRotation = Quaternion.Euler(-pitch * Mathf.Rad2Deg, -yaw * Mathf.Rad2Deg, roll * Mathf.Rad2Deg);
                Quaternion sensorRotation = Quaternion.Euler(-pitch, -yaw, roll);

                // 3. 초기 회전값에 센서 회전값을 곱해서 최종 회전값을 계산합니다.
                handRoot.localRotation = baseRotation * sensorRotation;
                currentRotation = handRoot.localRotation;
            }

        for (int i = 0; i < fingerJoints.Length; i++)
        {
            if (fingerJoints[i] != null && i < flexMin.Length && i < flexMax.Length)
            {
                float bendAmount = Mathf.InverseLerp(flexMin[i], flexMax[i], flexVals[i]);
                fingerValues[i] = bendAmount;
                float targetAngle = bendAmount * maxFingerAngle;

                Quaternion initialRotation = Quaternion.identity;

                switch (i)
                {
                    case 0: // 엄지 (ThumbB_R)
                        // 오른손 모델에 맞게 Y축 부호만 반전했습니다.
                        initialRotation = Quaternion.Euler(-0.539f, 0.005f, -17.365f);
                        fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, -targetAngle);
                        break;
                    case 1: // 검지 (IndexA_R)
                        initialRotation = Quaternion.Euler(14.478f, -85.578f, -11.701f);
                        fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, -targetAngle);
                        break;
                    case 2: // 중지 (MiddleA_R)
                        initialRotation = Quaternion.Euler(3.078f, -99.098f, -9.556f);
                        fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, -targetAngle);
                        break;
                    case 3: // 약지 (RingA_R)
                        initialRotation = Quaternion.Euler(-7.489f, -109.398f, -6.735f);
                        fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, -targetAngle);
                        break;
                    case 4: // 소지 (PinkyA_R)
                        initialRotation = Quaternion.Euler(-15.24f, -123.18f, -9.379f);
                        fingerJoints[i].localRotation = initialRotation * Quaternion.Euler(0f, 0f, -targetAngle);
                        break;
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (dataReadThread != null && dataReadThread.IsAlive) dataReadThread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}