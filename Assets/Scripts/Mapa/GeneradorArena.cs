using UnityEngine;
using System.Collections.Generic;

namespace RealmBrawl
{
    public class GeneradorArena : MonoBehaviour
    {
        [Header("Terreno")]
        [SerializeField] float tamano = 80f;
        [SerializeField] Material materialSuelo;

        [Header("Decoracion")]
        [SerializeField] GameObject[] prefabsArboles;
        [SerializeField] GameObject[] prefabsRocas;
        [SerializeField] GameObject[] prefabsArbustos;
        [SerializeField] GameObject[] prefabsVallas;

        [Header("Spawn Points")]
        [SerializeField] int cantidadSpawns = 4;
        [SerializeField] float distanciaSpawns = 25f;

        [Header("Semilla")]
        [SerializeField] int semilla = 42;

        List<Transform> puntosSpawn = new List<Transform>();
        public List<Transform> PuntosSpawn => puntosSpawn;

        public void Generar()
        {
            Random.InitState(semilla);
            LimpiarHijos();

            CrearSuelo();
            CrearDecoPerimetro();
            CrearAntorchas();
            CrearPuntosSpawn();
        }

        void LimpiarHijos()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }
            puntosSpawn.Clear();
        }

        void CrearSuelo()
        {
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "Suelo";
            suelo.transform.SetParent(transform);
            suelo.transform.localPosition = Vector3.zero;
            suelo.transform.localScale = new Vector3(tamano / 10f, 1f, tamano / 10f);
            suelo.tag = "Ground";
            suelo.layer = LayerMask.NameToLayer("Default");

            if (materialSuelo != null)
                suelo.GetComponent<Renderer>().sharedMaterial = materialSuelo;
        }

        void CrearDecoPerimetro()
        {
            var contenedor = new GameObject("Decoracion");
            contenedor.transform.SetParent(transform);

            float mitad = tamano * 0.45f;
            int cantidadPorLado = 12;

            for (int lado = 0; lado < 4; lado++)
            {
                for (int i = 0; i < cantidadPorLado; i++)
                {
                    float t = (float)i / cantidadPorLado;
                    Vector3 pos = Vector3.zero;

                    switch (lado)
                    {
                        case 0: pos = new Vector3(Mathf.Lerp(-mitad, mitad, t), 0, mitad); break;
                        case 1: pos = new Vector3(Mathf.Lerp(-mitad, mitad, t), 0, -mitad); break;
                        case 2: pos = new Vector3(mitad, 0, Mathf.Lerp(-mitad, mitad, t)); break;
                        case 3: pos = new Vector3(-mitad, 0, Mathf.Lerp(-mitad, mitad, t)); break;
                    }

                    pos += new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

                    // No colocar decoraciones en el centro de la arena (radio 20) para dar espacio de combate
                    if (new Vector2(pos.x, pos.z).magnitude < 20f)
                        continue;

                    // Alternar entre arboles, rocas y arbustos
                    GameObject prefab = null;
                    float r = Random.value;
                    if (r < 0.4f && prefabsArboles != null && prefabsArboles.Length > 0)
                        prefab = prefabsArboles[Random.Range(0, prefabsArboles.Length)];
                    else if (r < 0.7f && prefabsRocas != null && prefabsRocas.Length > 0)
                        prefab = prefabsRocas[Random.Range(0, prefabsRocas.Length)];
                    else if (prefabsArbustos != null && prefabsArbustos.Length > 0)
                        prefab = prefabsArbustos[Random.Range(0, prefabsArbustos.Length)];

                    if (prefab != null)
                    {
                        var obj = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), contenedor.transform);
                        float escala = Random.Range(0.8f, 1.3f);
                        obj.transform.localScale *= escala;
                    }
                }
            }
        }

        void CrearAntorchas()
        {
            var contenedor = new GameObject("Antorchas");
            contenedor.transform.SetParent(transform);

            float d = tamano * 0.35f;
            Vector3[] posiciones = {
                new Vector3(d, 0, d),
                new Vector3(-d, 0, d),
                new Vector3(d, 0, -d),
                new Vector3(-d, 0, -d)
            };

            foreach (var pos in posiciones)
            {
                var antorcha = new GameObject("Antorcha");
                antorcha.transform.SetParent(contenedor.transform);
                antorcha.transform.position = pos;

                // Palo
                var palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                palo.transform.SetParent(antorcha.transform);
                palo.transform.localPosition = new Vector3(0, 1.5f, 0);
                palo.transform.localScale = new Vector3(0.15f, 1.5f, 0.15f);

                var matPalo = new Material(Shader.Find("Standard"));
                matPalo.color = new Color(0.4f, 0.25f, 0.1f);
                palo.GetComponent<Renderer>().sharedMaterial = matPalo;

                // Luz
                var luzObj = new GameObject("Luz");
                luzObj.transform.SetParent(antorcha.transform);
                luzObj.transform.localPosition = new Vector3(0, 3.2f, 0);
                var luz = luzObj.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.color = new Color(1f, 0.7f, 0.3f);
                luz.range = 12f;
                luz.intensity = 1.5f;
            }
        }

        void CrearPuntosSpawn()
        {
            var contenedor = new GameObject("PuntosSpawn");
            contenedor.transform.SetParent(transform);

            for (int i = 0; i < cantidadSpawns; i++)
            {
                float angulo = (360f / cantidadSpawns) * i;
                float x = Mathf.Sin(angulo * Mathf.Deg2Rad) * distanciaSpawns;
                float z = Mathf.Cos(angulo * Mathf.Deg2Rad) * distanciaSpawns;

                var spawn = new GameObject($"Spawn_{i}");
                spawn.transform.SetParent(contenedor.transform);
                spawn.transform.position = new Vector3(x, 0, z);
                puntosSpawn.Add(spawn.transform);
            }
        }

        public void AsignarRecursos(Material suelo, GameObject[] arboles, GameObject[] rocas, GameObject[] arbustos)
        {
            materialSuelo = suelo;
            prefabsArboles = arboles;
            prefabsRocas = rocas;
            prefabsArbustos = arbustos;
        }
    }
}
