using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DeltaTime 表示用
/// </summary>
public class UpdateDt : MonoBehaviour {
    public Text dtText;
    private StringBuilder sb = new StringBuilder(256);

    private static UpdateDt instance;
    public static UpdateDt Instance { get { return instance; } }

    void Awake()
    {
        instance = this;
    }
    void OnDestroy()
    {
        instance = null;
    }

	// Update is called once per frame
	public void SetText (float dt , float execTime) {
        sb.Length = 0;
        sb.Append( dt.ToString("0.0000") );
        sb.Append("\n");
        sb.Append(execTime.ToString("0.0000"));

        dtText.text = sb.ToString();
	}
}
