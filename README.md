# REALM BRAWL

Proyecto de videojuego indie cooperativo en Unity 2022.3 LTS.

## Idea general

REALM BRAWL es un juego de arena medieval/fantasía para 1 a 5 jugadores cooperativos.
El foco principal está en un combate con techo de habilidad alto, donde cada arma tiene
una forma distinta de usarse con el mouse y los enemigos reciben más daño si se golpean
en zonas débiles específicas.

## Stack del proyecto

- Motor: Unity 2022.3 LTS
- Lenguaje: C#
- Editor: VSCode + C# Dev Kit
- Networking: Mirror
- Control de versiones: Git + GitHub + Git LFS

## Estructura esperada

Cuando el proyecto de Unity esté creado, la raíz del repositorio debería verse así:

```text
REALM BRAWL/
├── Assets/
│   ├── Audio/
│   ├── Models/
│   ├── Prefabs/
│   ├── Scenes/
│   └── Scripts/
├── Packages/
├── ProjectSettings/
├── .gitattributes
├── .gitignore
└── README.md
```

## Primer objetivo técnico

Armar un vertical slice simple:

1. Crear el proyecto base en Unity.
2. Configurar Git y Git LFS correctamente.
3. Integrar Mirror.
4. Lograr movimiento básico de un jugador.
5. Crear una primera versión del combate con espada.

## Reglas de colaboración

- Trabajar siempre con escenas, prefabs y assets en formato texto.
- No subir carpetas generadas por Unity como `Library` o `Temp`.
- Hacer commits pequeños y claros.
- Probar cada cambio antes de subirlo.
