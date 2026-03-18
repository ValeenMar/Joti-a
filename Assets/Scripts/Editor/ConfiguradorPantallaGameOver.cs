using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// COPILOT-CONTEXT:
// Proyecto: Realm Brawl.
// Motor: Unity 2022.3 LTS.
// Este script de editor crea automaticamente la pantalla de Game Over,
// su panel, textos, boton y referencias al GameManager.

// Esta clase agrega un menu que arma toda la pantalla de Game Over sin pasos manuales.
public static class ConfiguradorPantallaGameOver
{
    // Esta opcion de menu construye o reutiliza todos los objetos necesarios.
    [MenuItem("Realm Brawl/Configurar/Pantalla Game Over")]
    public static void ConfigurarPantallaGameOver()
    {
        // Buscamos o creamos un Canvas para la UI.
        Canvas canvas = BuscarOCrearCanvas();

        // Buscamos o creamos un EventSystem para que el boton funcione.
        BuscarOCrearEventSystem();

        // Buscamos o creamos el GameManager global.
        BuscarOCrearGameManager();

        // Buscamos o creamos el objeto controlador del sistema.
        GameObject objetoControl = BuscarOCrearHijo(canvas.transform, "ControlPantallaGameOver");

        // Agregamos el componente principal si todavia no estaba.
        PantallaGameOver pantallaGameOver = objetoControl.GetComponent<PantallaGameOver>();

        // Si no existia, lo agregamos ahora.
        if (pantallaGameOver == null)
        {
            pantallaGameOver = objetoControl.AddComponent<PantallaGameOver>();
        }

        // Buscamos o creamos el panel visual del game over.
        GameObject panelGameOver = BuscarOCrearHijo(canvas.transform, "PanelGameOver");

        // Ajustamos el panel para que cubra toda la pantalla.
        RectTransform rectPanel = panelGameOver.GetComponent<RectTransform>();

        // Si no tenia RectTransform, lo agregamos con el panel.
        if (rectPanel == null)
        {
            rectPanel = panelGameOver.AddComponent<RectTransform>();
        }

        // Estiramos el panel a pantalla completa.
        EstirarATodaLaPantalla(rectPanel);

        // Si no tenia imagen, la agregamos para fondo oscuro.
        Image imagenFondo = panelGameOver.GetComponent<Image>();

        // Si falta, la agregamos.
        if (imagenFondo == null)
        {
            imagenFondo = panelGameOver.AddComponent<Image>();
        }

        // Le damos un negro translucido para el fondo del Game Over.
        imagenFondo.color = new Color(0f, 0f, 0f, 0.82f);

        // Creamos los textos principales del panel.
        Text textoTitulo = CrearOReutilizarTexto(panelGameOver.transform, "TextoTitulo", "GAME OVER", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.78f), new Vector2(420f, 60f));
        Text textoOleada = CrearOReutilizarTexto(panelGameOver.transform, "TextoOleada", "Oleada: 0", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.62f), new Vector2(320f, 30f));
        Text textoKills = CrearOReutilizarTexto(panelGameOver.transform, "TextoKills", "Kills: 0", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.54f), new Vector2(320f, 30f));
        Text textoTiempo = CrearOReutilizarTexto(panelGameOver.transform, "TextoTiempo", "Tiempo: 00:00", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.46f), new Vector2(320f, 30f));
        Text textoNivel = CrearOReutilizarTexto(panelGameOver.transform, "TextoNivel", "Nivel: 1", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.38f), new Vector2(320f, 30f));
        Text textoRacha = CrearOReutilizarTexto(panelGameOver.transform, "TextoRacha", "Mejor racha: 0", 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.30f), new Vector2(320f, 30f));

        // Creamos o reutilizamos el boton de reinicio.
        Button botonReiniciar = CrearOReutilizarBoton(panelGameOver.transform, "BotonReiniciar", "Reiniciar", new Vector2(0.5f, 0.16f), new Vector2(220f, 44f));

        // Asignamos todas las referencias privadas usando SerializedObject.
        SerializedObject serializadoPantalla = new SerializedObject(pantallaGameOver);
        serializadoPantalla.FindProperty("panelGameOver").objectReferenceValue = panelGameOver;
        serializadoPantalla.FindProperty("textoTitulo").objectReferenceValue = textoTitulo;
        serializadoPantalla.FindProperty("textoOleada").objectReferenceValue = textoOleada;
        serializadoPantalla.FindProperty("textoKills").objectReferenceValue = textoKills;
        serializadoPantalla.FindProperty("textoTiempo").objectReferenceValue = textoTiempo;
        serializadoPantalla.FindProperty("textoNivel").objectReferenceValue = textoNivel;
        serializadoPantalla.FindProperty("textoRacha").objectReferenceValue = textoRacha;
        serializadoPantalla.FindProperty("botonReiniciar").objectReferenceValue = botonReiniciar;
        serializadoPantalla.ApplyModifiedPropertiesWithoutUndo();

        // Dejamos el panel oculto para que solo aparezca en Game Over.
        panelGameOver.SetActive(false);

        // Marcamos la escena como modificada para que Unity permita guardar.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        // Marcamos el componente como sucio para asegurar que guarde referencias.
        EditorUtility.SetDirty(pantallaGameOver);

        // Seleccionamos el controlador para que el usuario vea lo que se creo.
        Selection.activeGameObject = objetoControl;
    }

    // Este metodo busca o crea un Canvas listo para UI.
    private static Canvas BuscarOCrearCanvas()
    {
        // Intentamos encontrar un Canvas existente.
        Canvas canvasExistente = Object.FindObjectOfType<Canvas>();

        // Si ya existe, lo devolvemos.
        if (canvasExistente != null)
        {
            return canvasExistente;
        }

        // Creamos un objeto nuevo para el Canvas.
        GameObject objetoCanvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        // Obtenemos su componente Canvas.
        Canvas canvas = objetoCanvas.GetComponent<Canvas>();

        // Lo configuramos en modo Screen Space Overlay.
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Obtenemos el escalador para que la UI se adapte mejor.
        CanvasScaler escalador = objetoCanvas.GetComponent<CanvasScaler>();

        // Lo configuramos para escalar con la resolucion.
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);

        // Devolvemos el Canvas ya listo.
        return canvas;
    }

    // Este metodo busca o crea un EventSystem en la escena.
    private static void BuscarOCrearEventSystem()
    {
        // Si ya existe uno, no hacemos nada.
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        // Creamos un EventSystem nuevo con su input module clasico.
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // Este metodo busca o crea un GameManager global.
    private static GameManager BuscarOCrearGameManager()
    {
        // Intentamos encontrar uno ya existente.
        GameManager gameManager = Object.FindObjectOfType<GameManager>();

        // Si ya existe, lo devolvemos.
        if (gameManager != null)
        {
            return gameManager;
        }

        // Creamos un objeto vacio para alojar el GameManager.
        GameObject objetoGameManager = new GameObject("GameManager");

        // Agregamos el componente correspondiente.
        return objetoGameManager.AddComponent<GameManager>();
    }

    // Este metodo busca o crea un hijo por nombre.
    private static GameObject BuscarOCrearHijo(Transform padre, string nombre)
    {
        // Buscamos si ya existe un hijo con ese nombre.
        Transform hijoExistente = padre.Find(nombre);

        // Si existe, devolvemos su GameObject.
        if (hijoExistente != null)
        {
            return hijoExistente.gameObject;
        }

        // Si no existe, lo creamos como GameObject vacio con RectTransform.
        GameObject objetoNuevo = new GameObject(nombre, typeof(RectTransform));

        // Lo parentamos al padre indicado sin moverlo de la UI.
        objetoNuevo.transform.SetParent(padre, false);

        // Devolvemos el nuevo objeto.
        return objetoNuevo;
    }

    // Este metodo estira un RectTransform a pantalla completa.
    private static void EstirarATodaLaPantalla(RectTransform rectTransform)
    {
        // Llevamos anchors al minimo de la pantalla.
        rectTransform.anchorMin = Vector2.zero;

        // Llevamos anchors al maximo de la pantalla.
        rectTransform.anchorMax = Vector2.one;

        // Llevamos offsets a cero para cubrir todo.
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    // Este metodo crea o reutiliza un texto UI.
    private static Text CrearOReutilizarTexto(Transform padre, string nombre, string contenido, int tamanoFuente, TextAnchor alineacion, Vector2 anchorNormalizado, Vector2 tamano)
    {
        // Buscamos o creamos el objeto del texto.
        GameObject objetoTexto = BuscarOCrearHijo(padre, nombre);

        // Obtenemos o agregamos el componente Text.
        Text texto = objetoTexto.GetComponent<Text>();

        // Si no existe, lo agregamos.
        if (texto == null)
        {
            texto = objetoTexto.AddComponent<Text>();
        }

        // Tomamos la fuente Arial integrada de Unity.
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Escribimos el contenido inicial.
        texto.text = contenido;

        // Ajustamos tamaño y alineacion.
        texto.fontSize = tamanoFuente;
        texto.alignment = alineacion;
        texto.color = Color.white;

        // Obtenemos el RectTransform del texto.
        RectTransform rectTexto = objetoTexto.GetComponent<RectTransform>();

        // Colocamos anchors y pivot en el punto deseado.
        rectTexto.anchorMin = anchorNormalizado;
        rectTexto.anchorMax = anchorNormalizado;
        rectTexto.pivot = new Vector2(0.5f, 0.5f);

        // Definimos tamaño fijo.
        rectTexto.sizeDelta = tamano;

        // Centramos localmente.
        rectTexto.anchoredPosition = Vector2.zero;

        // Devolvemos el texto configurado.
        return texto;
    }

    // Este metodo crea o reutiliza un boton UI basico.
    private static Button CrearOReutilizarBoton(Transform padre, string nombre, string textoBoton, Vector2 anchorNormalizado, Vector2 tamano)
    {
        // Buscamos o creamos el objeto del boton.
        GameObject objetoBoton = BuscarOCrearHijo(padre, nombre);

        // Obtenemos o agregamos la imagen necesaria para el fondo del boton.
        Image imagenBoton = objetoBoton.GetComponent<Image>();

        // Si falta, la agregamos.
        if (imagenBoton == null)
        {
            imagenBoton = objetoBoton.AddComponent<Image>();
        }

        // Le damos un color sobrio al fondo del boton.
        imagenBoton.color = new Color(0.35f, 0.1f, 0.1f, 0.95f);

        // Obtenemos o agregamos el componente Button.
        Button boton = objetoBoton.GetComponent<Button>();

        // Si falta, lo agregamos.
        if (boton == null)
        {
            boton = objetoBoton.AddComponent<Button>();
        }

        // Ajustamos el RectTransform del boton.
        RectTransform rectBoton = objetoBoton.GetComponent<RectTransform>();
        rectBoton.anchorMin = anchorNormalizado;
        rectBoton.anchorMax = anchorNormalizado;
        rectBoton.pivot = new Vector2(0.5f, 0.5f);
        rectBoton.sizeDelta = tamano;
        rectBoton.anchoredPosition = Vector2.zero;

        // Creamos o reutilizamos el texto hijo del boton.
        Text texto = CrearOReutilizarTexto(objetoBoton.transform, "Texto", textoBoton, 18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), tamano);

        // Aseguramos que el texto no reciba raycasts.
        texto.raycastTarget = false;

        // Devolvemos el boton listo.
        return boton;
    }

    // COPILOT-EXPAND: Aqui podes agregar mas layouts de fin de partida o un menu maestro que configure toda la escena.
}
