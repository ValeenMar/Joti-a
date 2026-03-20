using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Esta clase describe la arena demo authored de la escena.
// Sirve como fuente de verdad para el minimapa tactico, spawns y alineacion
// de actores sobre el NavMesh sin depender de una camara cenital.

public class ArenaDemoMapa : MonoBehaviour
{
    [Header("Arena")]
    [SerializeField] private Vector2 tamanoMundo = new Vector2(68f, 68f);
    [SerializeField] private Transform puntoSpawnJugador;
    [SerializeField] private Transform[] puntosSpawnEnemigos;
    [SerializeField] private MiniMapaRegionTactica[] regionesMiniMapa;
    [SerializeField] private Collider sueloPrincipal;

    public Vector2 TamanoMundo => tamanoMundo;
    public Transform PuntoSpawnJugador => puntoSpawnJugador;
    public Transform[] PuntosSpawnEnemigos => puntosSpawnEnemigos;
    public MiniMapaRegionTactica[] RegionesMiniMapa => regionesMiniMapa;
    public Collider SueloPrincipal => sueloPrincipal;

    public Bounds BoundsMundo
    {
        get
        {
            Vector3 centro = transform.position;
            float altura = 4f;

            if (sueloPrincipal != null)
            {
                centro = sueloPrincipal.bounds.center;
                altura = Mathf.Max(4f, sueloPrincipal.bounds.size.y);
            }

            return new Bounds(centro, new Vector3(tamanoMundo.x, altura, tamanoMundo.y));
        }
    }

    public Vector2 MundoANormalizado(Vector3 posicionMundo)
    {
        Bounds bounds = BoundsMundo;
        float xMin = bounds.center.x - (tamanoMundo.x * 0.5f);
        float zMin = bounds.center.z - (tamanoMundo.y * 0.5f);

        float x = Mathf.InverseLerp(xMin, xMin + tamanoMundo.x, posicionMundo.x);
        float y = Mathf.InverseLerp(zMin, zMin + tamanoMundo.y, posicionMundo.z);
        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    public Vector3 ObtenerPosicionEnSuelo(Vector3 posicionAproximada, float radio = 8f)
    {
        if (NavMesh.SamplePosition(posicionAproximada, out NavMeshHit hitNavMesh, Mathf.Max(1f, radio), NavMesh.AllAreas))
        {
            return hitNavMesh.position;
        }

        Bounds bounds = BoundsMundo;
        return new Vector3(posicionAproximada.x, bounds.max.y, posicionAproximada.z);
    }

    public void RecolectarReferenciasDesdeHijos()
    {
        if (puntoSpawnJugador == null)
        {
            Transform spawnJugador = transform.Find("Spawns/SpawnJugador");
            if (spawnJugador != null)
            {
                puntoSpawnJugador = spawnJugador;
            }
        }

        if (regionesMiniMapa == null || regionesMiniMapa.Length == 0)
        {
            regionesMiniMapa = GetComponentsInChildren<MiniMapaRegionTactica>(true);
        }

        if (puntosSpawnEnemigos == null || puntosSpawnEnemigos.Length == 0)
        {
            Transform raizSpawns = transform.Find("Spawns");
            if (raizSpawns != null)
            {
                List<Transform> puntos = new List<Transform>();
                for (int indice = 0; indice < raizSpawns.childCount; indice++)
                {
                    Transform hijo = raizSpawns.GetChild(indice);
                    if (hijo == null || hijo.name == "SpawnJugador")
                    {
                        continue;
                    }

                    puntos.Add(hijo);
                }

                puntosSpawnEnemigos = puntos.ToArray();
            }
        }

        if (sueloPrincipal == null)
        {
            Transform suelo = transform.Find("SueloPrincipal");
            if (suelo != null)
            {
                sueloPrincipal = suelo.GetComponent<Collider>();
            }
        }
    }

    private void OnValidate()
    {
        tamanoMundo.x = Mathf.Max(16f, tamanoMundo.x);
        tamanoMundo.y = Mathf.Max(16f, tamanoMundo.y);
        RecolectarReferenciasDesdeHijos();
    }
}
