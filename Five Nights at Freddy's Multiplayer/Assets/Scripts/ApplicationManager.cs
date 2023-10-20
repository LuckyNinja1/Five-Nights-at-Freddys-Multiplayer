using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello World!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void LogToConsole(string text_, bool newLine_ = true)
    {
        QuantumConsole.Instance.LogToConsole(text_, newLine_);
    }
}
