using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


/// <summary>
/// バッテリー消費量をファイルに書き出しておきます
/// ログファイルはcsv形式で保存されます
/// 
/// ログの形式は下記です。
/// "実行ID" , "実行したモード" , "起動からの時間" , "バッテリーレベル" , "その時のDeltaTime" , "その時の実行時間"
/// </summary>
public class BatteryLogger {

    /// <summary>
    /// ファイルにログするインターバル(秒)
    /// </summary>
    private const int fileInterval = 30;


    private static BatteryLogger instance;

    private int lastExecuteTime = -10000;
    private StringBuilder stringBuilder;

    // 同一ファイルに複数データが入ってしまう可能性があるので…
    private string executeId;

    //  書き出すファイル名
    public string LogFile = "battery.txt";

    // 書き出すファイルの実パス
    private string actualPath;


    public static BatteryLogger Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new BatteryLogger();
            }
            return instance;
        }
    }
    /// <summary>
    ///  コンストラクタ
    /// </summary>
    private BatteryLogger()
    {
        executeId = System.Guid.NewGuid().ToString();
        stringBuilder = new StringBuilder(256);
        actualPath = System.IO.Path.Combine(Application.persistentDataPath, LogFile);
        Debug.Log("savePath " + actualPath);


        stringBuilder.Length = 0;
        if ( !System.IO.File.Exists(actualPath))
        {
            stringBuilder.Append("executeId,").Append("executeMode,").Append("timeFromStartup,");
            stringBuilder.Append("GetBatteryLevel(),").Append("dt,").Append("exeTm,");

            stringBuilder.Append("\n");
            System.IO.File.AppendAllText(actualPath, stringBuilder.ToString());
        }
    }

    /// <summary>
    /// ログにデータを入れます。が、一定スパンでないとデータをはじきます
    /// </summary>
    public void AddToLog(int executeMode, float timeFromStartup, float dt , float exeTm )
    {
        // まだファイルに書き込むべきではない
        if (timeFromStartup - lastExecuteTime < fileInterval)
        {
            return;
        }
        // ログファイル名からなら何もしない
        if (string.IsNullOrEmpty(LogFile))
        {
            return;
        }
        stringBuilder.Length = 0;
        stringBuilder.Append(executeId).Append(",");
        stringBuilder.Append(executeMode).Append(",");
        stringBuilder.Append(timeFromStartup).Append(",");
        stringBuilder.Append( GetBatteryLevel() ).Append(",");
        stringBuilder.Append(dt).Append(",");
        stringBuilder.Append(exeTm).Append(",");


        stringBuilder.Append("\n");
        System.IO.File.AppendAllText( actualPath, stringBuilder.ToString());

        lastExecuteTime = ((int)timeFromStartup / fileInterval) * fileInterval;
    }


    private static float GetBatteryLevel()
    {

        return SystemInfo.batteryLevel;
    }

}
