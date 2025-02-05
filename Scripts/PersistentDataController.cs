using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentDataController : MonoBehaviour
{
    private Material material;
    private Vector3 prevViewDir = Vector3.zero;
    private Vector2 prevUv = Vector2.zero;
    // Start is called before the first frame update
    void Start()
    {
        material = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 viewDir = Camera.main.transform.position - transform.position;
        if ((viewDir - prevViewDir).sqrMagnitude > 0.001f)
        {
            // Actualiza la dirección previa
            prevViewDir = viewDir;

            // Aquí puedes calcular la nueva UV si es necesario (ejemplo simplificado)
            Vector2 uv = new Vector2(0.5f, 0.5f); // Suponiendo que obtienes una UV de alguna manera

            // Actualiza las UV previas
            prevUv = uv;

            // Envía los nuevos valores al shader
            material.SetVector("_PrevViewDir", prevViewDir);
            material.SetVector("_PrevUv", prevUv);
        }

    }
}
