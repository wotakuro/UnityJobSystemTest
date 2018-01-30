# プロジェクトについて
このプロジェクトは Unity 2018.1から導入された JobSystemのテスト用プログラムです。

# 内容について
沢山のGameObject ( 5万個のSphere )を毎フレーム動かすテストです。<br />
5パターンのやり方で、どれが一番早そうかをテスト出来ます。

左上の選択画面で動的に変更が出来ます。また実行速度については deltaTimeを 左上に表示しています。
が、Profiler上で確認するのが良いでしょう


## DirectTransform
計算した結果を素直に transform.position / transform.rotationに代入していくやり方です。

## TransformAndRotation
計算した結果を素直に transform.SetPositionAndRotationで代入していくやり方です

## TransformAccessorMain
MainThreadでTransformAccessArray 越しにポジションをセットする方法です。
→ 結局 Transform経由でセットとなり、ダメでした。

## WithJobParallelForTransform
IJobParallelForTransformを継承したstructのExecuteで素直にposition/rotationをセットする方法です。

## WithJobParallelFor
IJobParallelFor側でposition/rotationの計算を行い、最終結果をMainThread側でセットする方法です。
