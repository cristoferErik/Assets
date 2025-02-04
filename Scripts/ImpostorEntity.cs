using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ImpostorEntity : MonoBehaviour
{
    public int depth;
    public float ratio = 1.25f;
    public int sizeAtlasTexture = 3000;
    public LayerMask layerMask;
    private List<Vector3> vertices;

    private List<Vector3> pyramidVertices;

    private Vector2[] uvs;

    struct Square
    {
        public Vector3 A, B, C, D;
        public Square(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { A = a; B = b; C = c; D = d; }

        public String getVertex()
        {
            return " Vector A: " + A + " Vector B: " + B + " Vector C: " + C + " Vector D: " + D;
        }
    }

    public void Initialize()
    {
        this.vertices = new List<Vector3>();
        this.pyramidVertices = new List<Vector3>();
    }
    public void createImpostorHemiOctahedron()
    {
        createMaterialEmission();
        createPyramid();
        generateTextures();
    }
    public void createPyramid()
    {
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 top = new Vector3(0, 0, 1);
        Vector3 right = new Vector3(1, 0, 0);
        Vector3 bottom = new Vector3(0, 0, -1);

        Square quad = new Square(left, top, right, bottom);

        List<Square> quads = new List<Square>();
        quads.AddRange(SubDivideQuad(quad, depth));

        foreach (Square v in quads)
        {
            Vector3 normalizedA = NormalizeVertex(v.A);
            Vector3 normalizedB = NormalizeVertex(v.B);
            Vector3 normalizedC = NormalizeVertex(v.C);
            Vector3 normalizedD = NormalizeVertex(v.D);

            if (!vertices.Contains(normalizedA))
            {
                vertices.Add(normalizedA);
            }
            if (!vertices.Contains(normalizedB))
            {
                vertices.Add(normalizedB);
            }
            if (!vertices.Contains(normalizedC))
            {
                vertices.Add(normalizedC);
            }
            if (!vertices.Contains(normalizedD))
            {
                vertices.Add(normalizedD);
            }
        }

        //Debug.Log(vertices.Count);
        convertToHemiOcto();
        moveObjectToCenter();
    }
    private Vector3 NormalizeVertex(Vector3 vertex, float precision = 1000f)
    {
        // Redondea las coordenadas a la precisión especificada
        return new Vector3(
            Mathf.Round(vertex.x * precision) / precision,
            Mathf.Round(vertex.y * precision) / precision,
            Mathf.Round(vertex.z * precision) / precision
        );
    }
    public void convertToHemiOcto()
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 temp = vertices[i];
            temp.y = 1 - (MathF.Abs(temp.x) + Mathf.Abs(temp.z));
            vertices[i] = temp.normalized * ratio;
        }
    }
    public void moveObjectToCenter()
    {

        transform.position = Vector3.zero;

        ScaleObjectProportionally();
    }
    void ScaleObjectProportionally()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // Obtener el tamaño del mesh (bounds.size)
            Vector3 meshSize = meshRenderer.bounds.size;

            // Calcular el factor de escala (escalar para que el tamaño más grande sea 1)
            float maxDimension = Mathf.Max(meshSize.x, meshSize.y, meshSize.z);

            // Si el tamaño máximo es mayor que 1, escalamos proporcionalmente
            if (maxDimension > 1f)
            {
                // Factor de escala para ajustar el tamaño máximo a 1
                float scaleFactor = 1f / maxDimension;

                // Escalar el objeto proporcionalmente
                transform.localScale = transform.localScale * scaleFactor;
            }
        }
    }
    private List<Square> SubDivideQuad(Square square, int depth)
    {
        if (depth == 1)
        {
            return new List<Square> { square };
        }
        List<Square> result = new List<Square>();

        Vector3 m = square.A;
        Vector3 n = (square.B - square.A) / depth + square.A;
        Vector3 q = (square.D - square.A) / depth + square.A;
        Vector3 p = ((square.B - square.A) / depth + (square.D - square.A) / depth) + square.A;

        Square sq = new Square(m, n, p, q);
        result.Add(sq);

        Vector3 a = p;
        Vector3 b = (square.C - square.B) / depth + square.B;
        Vector3 c = square.C;
        Vector3 d = (square.C - square.D) / depth + square.D; ;

        Square sq1 = new Square(a, b, c, d);

        //Esta porqueria hace lo que tiene que hacer, no tocar
        for (int i = 0; i < depth - 1; i++)
        {
            Vector3 rg = n + ((square.B - n) * (i) / (depth - 1));
            Vector3 tp = n + ((square.B - n) * (i + 1) / (depth - 1));

            Vector3 lt = p + ((b - p) * i / (depth - 1));
            Vector3 bt = p + ((b - p) * (i + 1) / (depth - 1));

            Square sq2 = new Square(rg, tp, lt, bt);
            result.Add(sq2);
        }
        for (int i = 0; i < depth - 1; i++)
        {
            Vector3 rg = q + (square.D - q) * (i) / (depth - 1);
            Vector3 tp = q + (square.D - q) * (i + 1) / (depth - 1);
            Vector3 lt = p + (d - p) * (i) / (depth - 1);
            Vector3 bt = p + (d - p) * (i + 1) / (depth - 1);

            Square sq2 = new Square(rg, tp, lt, bt);
            result.Add(sq2);
        }

        result.AddRange(SubDivideQuad(sq1, depth - 1));

        return result;
    }

    public Camera createCamera()
    {
        GameObject cameraObject = new GameObject("Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 0.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = layerMask;

        return camera;
    }

    public void generateTextures()
    {
        Camera camera = createCamera();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Vector3 currentObjectPosition = meshRenderer.bounds.center;

        // Tamaño de la textura por vértice 
        int sizeTexture = (int)(sizeAtlasTexture / math.sqrt(vertices.Count));

        RenderTexture renderTexture = new RenderTexture(sizeTexture, sizeTexture, 24);
        camera.targetTexture = renderTexture;

        //Pintamos de negro la textura
        Texture2D atlasTexture = new Texture2D(sizeAtlasTexture, sizeAtlasTexture, TextureFormat.RGB24, false);
        Color[] pixels = new Color[sizeAtlasTexture * sizeAtlasTexture];
        System.Array.Fill(pixels, Color.black);
        atlasTexture.SetPixels(pixels);

        float angle = 45 * Mathf.Deg2Rad;
        for (int i = 0; i < vertices.Count; i++)
        {
            camera.transform.position = vertices[i];
            Texture2D texture = new Texture2D(sizeTexture, sizeTexture, TextureFormat.RGB24, false);
            camera.transform.LookAt(currentObjectPosition);
            camera.Render();
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, sizeTexture, sizeTexture), 0, 0);
            texture.Apply();

            //Convertirlo a 2D rotado en un cuadrado y no rombo
            float t = 1 / (Mathf.Abs(vertices[i].x) + Mathf.Abs(vertices[i].y) + Mathf.Abs(vertices[i].z));
            Vector3 coordOct = vertices[i] * t;

            float Vx = (coordOct.x * Mathf.Cos(angle) - coordOct.z * Mathf.Sin(angle))*Mathf.Sqrt(2);
            float Vy = (coordOct.x * Mathf.Sin(angle) + coordOct.z * Mathf.Cos(angle))*Mathf.Sqrt(2);

            Vx= Mathf.Round(Vx*10000f)/10000f;
            Vy= Mathf.Round(Vy*10000f)/10000f;
            //Convertirlo a uv
            float u = (Vx + 1) * 0.5f;
            float v = (Vy + 1) * 0.5f;
            

            int x = Mathf.FloorToInt(u * (sizeAtlasTexture - sizeTexture));
            int y = Mathf.FloorToInt(v * (sizeAtlasTexture - sizeTexture));
            // Establece los píxeles en el atlas de texturas
            atlasTexture.SetPixels(x, y, sizeTexture, sizeTexture, texture.GetPixels());

            RenderTexture.active = null;

        }


        byte[] bytes = atlasTexture.EncodeToPNG(); // Codificar la textura a PNG
        string path = Path.Combine("/home/cristofer/img", "AtlasTexture.png");
        File.WriteAllBytes(path, bytes); // Escribir el archivo
        Debug.Log($"Imagen guardada en: {path}"); // Mensaje de confirmación

        atlasTexture.Apply();
        camera.targetTexture = null;
        renderTexture.Release();
        DestroyImmediate(camera.gameObject);
    }
    void createMaterialEmission()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            Material originalMaterial;
            Material emissiveMaterial;

            originalMaterial = meshRenderer.sharedMaterial;
            emissiveMaterial = new Material(Shader.Find("Unlit/emitter"));
            emissiveMaterial.mainTexture = originalMaterial.mainTexture;

            meshRenderer.sharedMaterial = emissiveMaterial;
        }
        else
        {
            Debug.Log("It is not found a MeshRenderer in your object!");
        }
    }
    void OnDrawGizmosSelected()
    {
        if (vertices == null || vertices.Count == 0)
            return;

        Gizmos.color = Color.green; // Cambia el color a tu preferencia
        for (int i = 0; i < vertices.Count; i++)
        {
            Gizmos.DrawSphere(vertices[i], 0.05f); // Dibuja una esfera en cada vértice
        }
    }

}
