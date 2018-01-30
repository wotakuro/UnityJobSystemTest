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
    // Z座標
    private const float ZPositin = 10.0f;

    // execute mode
    public enum ExecuteMethod : int
    {
        DirectTransform = 0,
        TransformAndRotation = 1,
        TransformAccessorMain = 2,
        WithIJobParallelForTransform = 3,
        WithIJobParallelFor = 4,
    }

    // 動かすprefab
    public GameObject prefab;
    // オブジェクト数
    public int objectNum = 10000;
    // 実行モード
    private ExecuteMethod executeMethod = ExecuteMethod.DirectTransform;


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

    struct MyParallelForUpdate : Unity.Jobs.IJobParallelFor
    {
        public Unity.Collections.NativeArray<MyStruct> accessor;
        public int objNum;
        public float time;

        public void Execute(int index)
        {
            accessor[index] = new MyStruct(CalculatePosition(index, objNum, time), CalcRotation(index, objNum, time));
        }
    }

    private UnityEngine.Jobs.TransformAccessArray transformAccessArray;
    private Transform[] transformArray;
    private Unity.Collections.NativeArray<MyStruct> myStructNativeArray;
    private MyTransformUpdateJob myTransformUpdateJob;
    private MyParallelForUpdate myParallelJob;
    private JobHandle jobHandle;

    // Use this for initialization
    void Start()
    {
        transformArray = new Transform[objectNum];
        transformAccessArray = new UnityEngine.Jobs.TransformAccessArray(objectNum, 0);
        myStructNativeArray = new Unity.Collections.NativeArray<MyStruct>(objectNum, Unity.Collections.Allocator.Persistent);
        for (int i = 0; i < objectNum; ++i)
        {
            GameObject gmo = Object.Instantiate<GameObject>(prefab);
            transformArray[i] = gmo.transform;
            transformAccessArray.Add(transformArray[i]);
        }
    }


    // Update is called once per frame
    void Update()
    {
        float tm = Time.timeSinceLevelLoad;
        switch (executeMethod)
        {
            // direct access to transform
            case ExecuteMethod.DirectTransform:
                {
                    for (int i = 0; i < objectNum; ++i)
                    {
                        transformArray[i].position = CalculatePosition(i, objectNum, tm);
                        transformArray[i].rotation = CalcRotation(i, objectNum, tm);
                    }
                }
                break;
            case ExecuteMethod.TransformAndRotation:
                {
                    for (int i = 0; i < objectNum; ++i)
                    {
                        transformArray[i].SetPositionAndRotation( CalculatePosition(i, objectNum, tm), CalcRotation(i, objectNum, tm) );
                    }
                }
                break;
            // access with transformAcessor
            case ExecuteMethod.TransformAccessorMain:
                {
                    for (int i = 0; i < objectNum; ++i)
                    {
                        transformAccessArray[i].position = CalculatePosition(i, objectNum, tm);
                        transformAccessArray[i].rotation = CalcRotation(i, objectNum, tm);
                    }
                }
                break;
            // access with transformAcessor( Job System )
            case ExecuteMethod.WithIJobParallelForTransform:
                {
                    myTransformUpdateJob = new MyTransformUpdateJob()
                    {
                        time = tm,
                        objNum = this.objectNum
                    };
                    jobHandle = myTransformUpdateJob.Schedule(this.transformAccessArray);
                    jobHandle.Complete();
                }
                break;
            // access with transformAcessor( Job System )
            case ExecuteMethod.WithIJobParallelFor:
                {
                    myParallelJob = new MyParallelForUpdate()
                    {
                        accessor = this.myStructNativeArray,
                        time = tm,
                        objNum = this.objectNum
                    };
                    jobHandle = myParallelJob.Schedule(objectNum, PreferJobNum);
                    jobHandle.Complete();
                    // 最後にセット
                    for (int i = 0; i < objectNum; ++i)
                    {
                        transformArray[i].SetPositionAndRotation(myStructNativeArray[i].position, myStructNativeArray[i].rotation);
                    }
                }
                break;
        }
    }

    // 座標計算
    private static Vector3 CalculatePosition(int idx, int num, float time)
    {
        Vector3 positon = new Vector3(idx - num / 2 + Mathf.Sin(time * 2.0f) , Mathf.Cos(time ) * 3.0f, ZPositin + Mathf.Repeat(time,4.0f) );
        return positon;
    }
    // 回転計算
    public static Quaternion CalcRotation(int idx, int num, float time)
    {
        Quaternion rot = Quaternion.AngleAxis(idx + time, Vector3.up);
        return rot;
    }



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
        Debug.Log("Change Value " + executeMethod);
    }
}