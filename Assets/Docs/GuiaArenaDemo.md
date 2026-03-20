# Realm Brawl - Guia Rapida Arena Demo

Esta guia explica como retocar a mano la arena demo sin romper spawns, minimapa ni navegacion.

## Flujo recomendado
1. Corre `Realm Brawl > Setup > Arena Demo Base`.
2. Abri `ArenaDemoBase` en la jerarquia.
3. Edita solo dentro de estas carpetas:
   - `Props`
   - `RegionesMiniMapa`
   - `Spawns`
4. Cuando termines de mover cosas, corre otra vez `Realm Brawl > Setup > Arena Demo Base` si queres reconstruir desde cero.
5. Si solo moviste objetos a mano y queres conservarlos, usa `Window > AI > Navigation` y rehornea el NavMesh desde Unity.

## Que hace cada carpeta
- `Props`: arboles, rocas, vallas, antorchas y el landmark del fondo.
- `RegionesMiniMapa`: formas tacticas abstractas que dibuja el minimapa.
- `Spawns`: punto de jugador y puntos de oleadas enemigas.

## Reglas para editar sin romper nada
- No borres `ArenaDemoBase`.
- No cambies el nombre de `SueloPrincipal`.
- No cambies el nombre de `SpawnJugador`.
- No cambies el nombre de los `SpawnEnemigo_X`.
- Si duplicas o agregas spawns enemigos, despues tenes que reasignarlos en `SistemaOleadas`.
- Si agrandas mucho un arbol o roca, revisa que el `NavMeshObstacle` siga cubriendolo bien.

## Como mover el minimapa tactico
- Cada objeto dentro de `RegionesMiniMapa` representa una masa abstracta del mapa.
- Podes moverlos para que coincidan mejor con los props reales.
- Podes tocar:
  - posicion
  - tamano
  - color
  - rotacion

## Como retocar el landmark del fondo
- El landmark principal vive en `Props/LandmarkFondo`.
- Podes mover:
  - `SantuarioRoto`
  - `CofreLandmark`
  - `CruzIzq`
  - `CruzDer`
  - `RocaLandmarkA/B/C`

## Que conviene hacer primero si queres mejorar la arena
1. Deja el centro siempre despejado.
2. Usa el bosque izquierdo como cobertura.
3. Usa el carril derecho para lectura y kiteo.
4. Mantene los spawns tapados por props, no a la vista directa.
5. Revisa el minimapa despues de mover regiones.

## Consejo practico
Si un cambio te rompe mucho la escena, borra `ArenaDemoBase` y volve a correr:

`Realm Brawl > Setup > Arena Demo Base`
