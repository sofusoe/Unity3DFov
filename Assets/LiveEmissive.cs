using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiveEmissive : MonoBehaviour {

    public Material material;

    void Start() {
        StartCoroutine(Flicker());
    }

    IEnumerator Flicker() {
        while(true) {
            material.DisableKeyword("_EMISSION");
            yield return new WaitForSeconds(0.5f);
            material.EnableKeyword("_EMISSION");
            yield return new WaitForSeconds(0.5f);
        }
    }

}
