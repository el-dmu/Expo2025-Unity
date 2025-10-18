using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class ReceiveHandDataL : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("장치 관리자에서 확인한 왼손 COM 포트 번호")]
    public string portName = "COM8";

    [Tooltip("DataManager가 flex 센서 값을 가져가기 위한 배열")]
    public int[] flexVals = new int[5];

    [Tooltip("DataManager가 자이로 센서 값을 가져가기 위한 배열")]
    public float[] QuaterVals = new float[4];

    private SerialPort serialPort;
    private Thread dataReadThread;
    private bool isRunning = false;
    private string receivedString;
    private readonly object lockObject = new object();
        
    void Start()
    {
        try
            {
                serialPort = new SerialPort(portName, 115200) { ReadTimeout = 200 };
                serialPort.Open();
                isRunning = true;
                dataReadThread = new Thread(ReadDataThread);
                dataReadThread.IsBackground = true;
                dataReadThread.Start();
                Debug.Log($"✅ [왼손] Bluetooth-Serial 포트 연결 성공: {portName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [왼손] Bluetooth-Serial 포트 연결 실패: {e.Message}");
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
                if (isRunning) Debug.LogWarning($"⚠ [왼손] 데이터 수신 오류: {e.Message}");
            }
        }
    }

    private void ParseData(string data)
    {
        try
        {
            data = data.TrimStart(',');

            string[] parts = data.Split(',');
            if (parts.Length >= 12)
            {
                for (int i = 0; i < 5; i++)
                {
                    flexVals[i] = int.Parse(parts[i]);
                }

                for (int i = 0; i < 4; i++)
                {
                    QuaterVals[i] = float.Parse(parts[i + 8]);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠ [왼손] 데이터 파싱 실패: {data} | 오류: {e.Message}");
        } 
    }
    
    void OnApplicationQuit()
    {
        isRunning = false;
        if (dataReadThread != null && dataReadThread.IsAlive) dataReadThread.Join();
        if (serialPort != null && serialPort.IsOpen) serialPort.Close();
    }
}