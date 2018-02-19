# プロジェクトについて
このプロジェクトは Unity 2018.1から導入された JobSystemのテスト用プログラムです。

# 内容について
沢山のGameObject ( 5万個のSphere )を毎フレーム動かすテストです。<br />
5パターンのやり方で、どれが一番早そうかをテスト出来ます。

左上の選択画面で動的に変更が出来ます。また実行速度については deltaTimeを 左上に表示しています。
が、Profiler上で確認するのが良いでしょう


## 1.DirectTransform
計算した結果を素直に transform.position / transform.rotationに代入していくやり方です。
全てがMain Thread上で行われます。

## 2.TransformAndRotation
計算した結果を素直に transform.SetPositionAndRotationで代入していくやり方です
全てがMain Thread上で行われます。

## 3.WithJobParallelForTransform
IJobParallelForTransformを利用して並行して、transformを動かします。
Jobを活用した形の方法です。
Update関数内で全てのtransform更新処理が終わる前提でやっています。

※Editor実行時だと、Renderingの処理が膨らんでしまいます。
　Editorのみの負荷となり、実機では影響がありません。

## 4.WithJobParallelForTransformByNextFrame
IJobParallelForTransformを継承したstructで
Update関数内で全てのtransform更新処理が終わる前提でやっています。

※Editor実行時だと、Renderingの処理が膨らんでしまいます。
　Editorのみの負荷となり、実機では影響がありません。

## 5.WithJobParallelFor
IJobParallelFor側でposition/rotationの計算を行い、最終結果をMainThread側でセットする方法です。
座標計算計算部分だけをJob化して、最後の処理はMainThread側で動きます.


