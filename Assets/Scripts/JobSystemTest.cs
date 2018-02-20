using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// この辺がjobsytem関連の namespace
using Unity.Jobs;
using Unity.Jobs.LowLevel;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Jobs;

// job system test program
public class JobSystemTest : MonoBehaviour
{
    // 目標Job数
    private const int PreferJobNum = 7;
    // Z座標に追加する
    private const float ZPositionAdd = 10.0f;

    // execute mode
    public enum ExecuteMethod : int
    {
        DirectTransform = 0,
        TransformAndRotation = 1,
        WithIJobParallelForTransform = 2,
        WithIJobParallelForTransformByNextFrame = 3,
        WithIJobParallelFor = 4,
    }

    // 動かすprefab
    public GameObject prefab;
    // オブジェクト数
    public int objectNum = 10000;
    // ターゲットのFPSを指定
    public int targetFps = 30;
    // デフォルトの挙動
    public ExecuteMethod defaultMode = ExecuteMethod.WithIJobParallelForTransform;
    // ログファイル名
    public string logFileName = "battery.log";
    // 解像度縮小モード
    public bool isLowResolution = false;

    // 実行モード
    private ExecuteMethod executeMethod = ExecuteMethod.DirectTransform;
    // Jobの終了待ち等を行うHandleです
    private JobHandle jobHandle;
    // 動かす対象となるTransformの配列です
    private Transform[] transformArray;

    #region TRANSFORM_JOB
    // IParallelForTransfromで実行するための TransformAccessArrayです
    private UnityEngine.Jobs.TransformAccessArray transformAccessArray;
    #endregion TRANSFORM_JOB

    #region ARRAY_JOB
    // 配列に対する処理を行うJob部分です
    private Unity.Collections.NativeArray<MyStruct> myStructNativeArray;
    #endregion ARRAY_JOB

    #region TRANSFORM_JOB
    // transform更新用のjob
    struct MyTransformUpdateJob : UnityEngine.Jobs.IJobParallelForTransform
    {
        public int objNum;
        public float time;
        public void Execute(int index, TransformAccess transform)
        {
            transform.position = CalculatePosition(index, objNum, time);
            transform.rotation = CalcRotation(index, objNum, time);
        }
    }
    #endregion TRANSFORM_JOB



    #region ARRAY_JOB
    // 配列計算用にデータを用意しておきます
    struct MyStruct
    {
        public Vector3 position;
        public Quaternion rotation;

        public MyStruct(Vector3 pos, Quaternion rot)
        {
            this.position = pos;
            this.rotation = rot;
        }
    }

    // 配列計算用のJobを定義します
    struct MyParallelForUpdate : Unity.Jobs.IJobParallelFor
    {
        public Unity.Collections.NativeArray<MyStruct> accessor;
        public int objNum;
        public float time;

        /// 実行部分の関数です
        /// indexに配列のindexが入ってきます
        public void Execute(int index)
        {
            // accessor[index + 1] などのように、index以外の範囲に対して書き込みをした場合 エラーが発生します( Editorのみ)
            // 予想外の実行時エラーを減らすために、Editor起動時ではチェックするようになっています。
            accessor[index] = new MyStruct(CalculatePosition(index, objNum, time), CalcRotation(index, objNum, time));
        }
    }

    #endregion ARRAY_JOB

    // 初期化処理など
    void Start()
    {
        // ターゲットのFPSを指定します
        Application.targetFrameRate = targetFps;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (isLowResolution)
        {
            Screen.SetResolution(Screen.width / 4, Screen.height / 4, true);
        }
        // デフォルト挙動セット
        this.executeMethod = this.defaultMode;
        Resources.FindObjectsOfTypeAll<UnityEngine.UI.Dropdown>()[0].value = (int) this.defaultMode;
        // ログファイル名指定
        BatteryLogger.Instance.LogFile = this.logFileName;

        // 操作対象のtransformの作成を行います
        transformArray = new Transform[objectNum];
        for (int i = 0; i < objectNum; ++i)
        {
            GameObject gmo = Object.Instantiate<GameObject>(prefab);
            transformArray[i] = gmo.transform;
        }


        // TransformJobの為に、TransformAccessArrayを事前に生成しておきます
        #region TRANSFORM_JOB
        transformAccessArray = new UnityEngine.Jobs.TransformAccessArray(objectNum, 0);
        for (int i = 0; i < objectNum; ++i)
        {
            transformAccessArray.Add(transformArray[i]);
        }
        #endregion TRANSFORM_JOB

        // 座標計算等を並行処理させるために、事前に計算用のワークを確保しておきます
        #region ARRAY_JOB
        myStructNativeArray = new Unity.Collections.NativeArray<MyStruct>(objectNum, Unity.Collections.Allocator.Persistent);
        #endregion ARRAY_JOB
    }


    // 更新処理
    void Update()
    {
        float tm = Time.realtimeSinceStartup;
        switch (executeMethod)
        {
            // transformに直接アクセスします
            case ExecuteMethod.DirectTransform:
                {
                    for (int i = 0; i < objectNum; ++i)
                    {
                        transformArray[i].position = CalculatePosition(i, objectNum, tm);
                        transformArray[i].rotation = CalcRotation(i, objectNum, tm);
                    }
                }
                break;
            // SetPositionAndRotationで設定します
            case ExecuteMethod.TransformAndRotation:
                {
                    for (int i = 0; i < objectNum; ++i)
                    {
                        transformArray[i].SetPositionAndRotation( CalculatePosition(i, objectNum, tm), CalcRotation(i, objectNum, tm) );
                    }
                }
                break;
            #region TRANSFORM_JOB
            // IJobParallelForTransformを利用して処理します( Job System )
            case ExecuteMethod.WithIJobParallelForTransform:
                {
                    UpdateWithIJobParallelForTransform(tm);
                }
                break;
            // IjobParallelForTransformを利用して処理しますが、更新処理をそのフレームで待たず、次のフレームで同期することを期待します
            case ExecuteMethod.WithIJobParallelForTransformByNextFrame:
                {
                    UpdateWithIJobParallelForTransformByNextFrame(tm);
                }
                break;
            #endregion TRANSFORM_JOB

            #region ARRAY_JOB
            // IJobParallelForを利用して座標計算を行った後に、最後にMainThreadでTransformに代入します( Job System )
            case ExecuteMethod.WithIJobParallelFor:
                {
                    UpdateWithIJobParalellFor(tm);
                }
                break;
            #endregion ARRAY_JOB
        }
        // 実行時間をセット
        float executeTime = Time.realtimeSinceStartup - tm;
        UpdateDt.Instance.SetText(Time.deltaTime, executeTime);
        // ログに書き出します
        BatteryLogger.Instance.AddToLog((int)this.executeMethod, tm, Time.deltaTime, executeTime);
    }

    #region TRANSFORM_JOB
    /// Transformに対する並列処理を行う部分です。
    private void UpdateWithIJobParallelForTransform(float tm)
    {
        // Transformに関する並列処理のJobを作成します
        MyTransformUpdateJob myTransformUpdateJob = new MyTransformUpdateJob()
        {
            // structの初期値を設定します
            time = tm,
            objNum = this.objectNum
        };
        // tranfromAccessArrayに対する処理を Jobに発行します
        jobHandle = myTransformUpdateJob.Schedule(this.transformAccessArray);
        // 上記で発行したJobが終わるまで、処理をブロックします
        jobHandle.Complete();
    }


    /// Transformに対する並列処理を行います。その場で同期せず、次のフレームに同期のタイミングを持ち越し、処理の短縮を図ります
    private void UpdateWithIJobParallelForTransformByNextFrame(float tm)
    {
        // 前のフレーム発行したJobが終わるまで、処理をブロックします
        jobHandle.Complete();
        // Transformに関する並列処理のJobを作成します
        MyTransformUpdateJob myTransformUpdateJob = new MyTransformUpdateJob()
        {
            // structの初期値を設定します
            time = tm,
            objNum = this.objectNum
        };
        // tranfromAccessArrayに対する処理を Jobに発行します
        jobHandle = myTransformUpdateJob.Schedule(this.transformAccessArray);
        // 発行したJobを直ちに実行するように促します
        JobHandle.ScheduleBatchedJobs();
    }
    #endregion TRANSFORM_JOB

    #region ARRAY_JOB
    // 座標計算のみ平行処理して行い、Transformに入れる部分だけは MainThreadにしたバージョンです
    private void UpdateWithIJobParalellFor(float tm)
    {
        // Transformに関する並列処理のJobを作成します
        MyParallelForUpdate myParallelJob = new MyParallelForUpdate()
        {
            // structの初期値を設定します
            accessor = this.myStructNativeArray,
            time = tm,
            objNum = this.objectNum
        };

        // 配列の要素数と、処理の分割数を指定して、Jobを発行します
        jobHandle = myParallelJob.Schedule(objectNum, PreferJobNum);
        // 上記で発行したJobが終わるまで処理をブロックします
        jobHandle.Complete();
        // 最後に MainThread上で positionとrotationのセットを行います
        for (int i = 0; i < objectNum; ++i)
        {
            transformArray[i].SetPositionAndRotation(myStructNativeArray[i].position, myStructNativeArray[i].rotation);
        }
    }
    #endregion ARRAY_JOB

    // 座標計算のコア部分
    private static Vector3 CalculatePosition(int idx, int num, float time)
    {
        float x = idx / 100.0f + Mathf.Sin(time * 2.0f) - num / 200;
        float y = Mathf.Cos(time) * -3.0f + ((idx / 10 ) % 10);
        float z = ZPositionAdd + Mathf.Cos(time) + 4.0f + (idx % 10);
        Vector3 positon = new Vector3( x  , y , z );
        return positon;
    }
    // 回転計算のコア部分
    public static Quaternion CalcRotation(int idx, int num, float time)
    {
        Quaternion rot = Quaternion.AngleAxis(idx + time, Vector3.up);
        return rot;
    }


    /// <summary>
    /// 破棄時に、色々Disposeします
    /// </summary>
    void OnDestroy()
    {
        this.jobHandle.Complete();
        this.transformAccessArray.Dispose();
        this.myStructNativeArray.Dispose();
    }

    // メニューで実行モードが変更された
    public void OnChangedSelection(int val)
    {
        executeMethod = (ExecuteMethod)val;
    }
}