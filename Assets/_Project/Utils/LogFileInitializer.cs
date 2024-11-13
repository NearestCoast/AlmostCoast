using System;
using UnityEngine;

using System.IO;
using Object = UnityEngine.Object;

public class CustomLogHandler : ILogHandler
{
    private ILogHandler defaultLogHandler = Debug.unityLogger.logHandler;
    private StreamWriter logFileWriter;
    private bool isDisposed = false;

    public CustomLogHandler(string logFilePath)
    {
        // 지정한 경로에 파일 스트림을 열어 로그를 기록
        logFileWriter = new StreamWriter(logFilePath, true) { AutoFlush = true };
    }

    // 기본 로그 메시지 처리
    public void LogFormat(LogType logType, Object context, string format, params object[] args)
    {
        if (isDisposed) return;  // 로그 파일이 닫힌 경우 로그를 기록하지 않음
        
        string message = string.Format(format, args);

        // 로그를 파일에 기록
        logFileWriter.WriteLine($"[{logType}] {message}");

        // Unity 기본 로그 핸들러에도 전달하여 Unity 콘솔에서도 볼 수 있도록
        defaultLogHandler.LogFormat(logType, context, format, args);
    }

    // 예외 로그 메시지 처리
    public void LogException(System.Exception exception, Object context)
    {
        if (isDisposed) return;  // 로그 파일이 닫힌 경우 로그를 기록하지 않음
        
        logFileWriter.WriteLine($"[Exception] {exception}");
        defaultLogHandler.LogException(exception, context);
    }

    // 로그 파일 닫기
    public void Close()
    {
        if (isDisposed) return;
        
        logFileWriter?.Close();
        isDisposed = true;
    }
}


namespace _Project.Utils
{
    public class LogFileInitializer : MonoBehaviour
    {
        [SerializeField] private string path;
        private CustomLogHandler customLogHandler;

        void Start()
        {
            // 원하는 로그 파일 경로 설정
            string logFilePath = Path.Combine(path == ""? Application.persistentDataPath : path, "CustomLog.txt");
        
            // 커스텀 로그 핸들러 초기화
            customLogHandler = new CustomLogHandler(logFilePath);

            // Unity Logger에 커스텀 로그 핸들러 등록
            Debug.unityLogger.logHandler = customLogHandler;
        }

        private void OnDisable()
        {
            customLogHandler?.Close();
        }

        void OnDestroy()
        {
            // 프로그램 종료 시 로그 파일 닫기
            customLogHandler?.Close();
        }
    }
}