using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateDt : MonoBehaviour {
    public Text dtText;
	// Update is called once per frame
	void Update () {
        dtText.text = Time.deltaTime.ToString("0.0000");
	}
}
