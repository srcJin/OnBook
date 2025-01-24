using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DisplayVersion : MonoBehaviour
{
    [SerializeField] TMPro.TMP_Text productName;
    [SerializeField] TMPro.TMP_Text version;

    void Start()
    {
        if (version) version.text = Application.version;
        if (productName) productName.text = Application.productName;
    }
}
