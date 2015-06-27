using UnityEngine;
using System.Collections;

public class MaterialModifier : MonoBehaviour
{
    public Material material;

    IEnumerator Start()
    {
        var spray = GetComponent<Kvant.Spray>();
        while (true)
        {
            spray.material.color = Color.red;
            yield return new WaitForSeconds(1);

            spray.material.color = Color.blue;
            yield return new WaitForSeconds(1);

            spray.material = material;
            yield return new WaitForSeconds(1);
        }
    }
}
