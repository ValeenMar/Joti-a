using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RealmBrawl
{
    public class SistemaOleadas : MonoBehaviour
    {
        [Header("Configuracion")]
        [SerializeField] int enemigosBaseOleada = 3;
        [SerializeField] int incrementoPorOleada = 2;
        [SerializeField] float delayEntreSpawns = 0.5f;
        [SerializeField] float delayEntreOleadas = 5f;

        [Header("Boss")]
        [SerializeField] int oleadasParaBoss = 4; // cada 4 oleadas
        [SerializeField] int enemigosOleadaEspecial = 8; // oleada rapida

        [Header("Referencias")]
        [SerializeField] GameObject prefabEnemigo;
        [SerializeField] Transform[] puntosSpawn;

        int oleadaActual;
        int enemigosVivos;
        List<EnemigoBase> enemigosActivos = new List<EnemigoBase>();

        public int OleadaActual => oleadaActual;
        public int EnemigosVivos => enemigosVivos;

        void OnEnable()
        {
            Eventos.AlMatarEnemigo += OnEnemigoMuerto;
        }

        void OnDisable()
        {
            Eventos.AlMatarEnemigo -= OnEnemigoMuerto;
        }

        public void IniciarOleadas()
        {
            oleadaActual = 0;
            StartCoroutine(LoopOleadas());
        }

        IEnumerator LoopOleadas()
        {
            while (true)
            {
                yield return new WaitForSeconds(oleadaActual == 0 ? 2f : delayEntreOleadas);

                oleadaActual++;
                Eventos.AlIniciarOleada?.Invoke(oleadaActual);

                bool esBoss = oleadaActual % oleadasParaBoss == 0;
                bool esEspecial = !esBoss && oleadaActual % oleadasParaBoss == 2 && oleadaActual > 3;

                int cantidadEnemigos;
                if (esBoss)
                    cantidadEnemigos = 1; // solo el boss
                else if (esEspecial)
                    cantidadEnemigos = enemigosOleadaEspecial;
                else
                    cantidadEnemigos = enemigosBaseOleada + (oleadaActual - 1) * incrementoPorOleada;


                yield return StartCoroutine(SpawnearOleada(cantidadEnemigos, esBoss, esEspecial));

                // Esperar a que mueran todos
                while (enemigosVivos > 0)
                    yield return null;


                Eventos.AlCompletarOleada?.Invoke();
            }
        }

        IEnumerator SpawnearOleada(int cantidad, bool esBoss, bool esEspecial)
        {
            enemigosVivos = cantidad;

            for (int i = 0; i < cantidad; i++)
            {
                Vector3 posSpawn = ObtenerPuntoSpawn(i);
                GameObject enemigo = Instantiate(prefabEnemigo, posSpawn, Quaternion.identity);
                var enemigoBase = enemigo.GetComponent<EnemigoBase>();

                if (enemigoBase != null)
                {
                    if (esBoss)
                    {
                        enemigoBase.ConfigurarComoBoss(oleadaActual);
                        enemigo.name = $"Boss_Oleada{oleadaActual}";
                    }
                    else if (esEspecial)
                    {
                        // Oleada especial: muchos enemigos debiles y rapidos
                        enemigoBase.ConfigurarStats(25f, 5f, 4f, 10f);
                        enemigo.name = $"EnemigoRapido_{i}";
                        enemigo.transform.localScale = Vector3.one * 0.7f;
                    }
                    else
                    {
                        // Escalar stats con oleadas
                        float multiplicador = 1f + (oleadaActual - 1) * 0.1f;
                        enemigoBase.ConfigurarStats(
                            50f * multiplicador,
                            10f * multiplicador,
                            3.5f,
                            25f * multiplicador
                        );
                        enemigo.name = $"Esqueleto_{i}_Oleada{oleadaActual}";
                    }

                    enemigosActivos.Add(enemigoBase);
                }

                yield return new WaitForSeconds(delayEntreSpawns);
            }
        }

        Vector3 ObtenerPuntoSpawn(int indice)
        {
            if (puntosSpawn != null && puntosSpawn.Length > 0)
            {
                int idx = indice % puntosSpawn.Length;
                Vector3 pos = puntosSpawn[idx].position;
                // Agregar algo de variacion
                pos += Random.insideUnitSphere * 2f;
                pos.y = puntosSpawn[idx].position.y;
                return pos;
            }

            // Fallback: spawn en circulo alrededor del origen
            float angulo = indice * (360f / Mathf.Max(1, indice + 1));
            return new Vector3(Mathf.Sin(angulo * Mathf.Deg2Rad) * 15f, 0f, Mathf.Cos(angulo * Mathf.Deg2Rad) * 15f);
        }

        void OnEnemigoMuerto(GameObject enemigo)
        {
            enemigosVivos = Mathf.Max(0, enemigosVivos - 1);
            enemigosActivos.RemoveAll(e => e == null || e.gameObject == enemigo);
        }

        public void AsignarPrefab(GameObject prefab) => prefabEnemigo = prefab;
        public void AsignarSpawns(Transform[] spawns) => puntosSpawn = spawns;
    }
}
