using Assets.Scripts.UI.Store;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

// Ajoute ce composant sur un GameObject qui possède un UIDocument
// et référence le nom de l’élément StoreActionButton à écouter.
[RequireComponent(typeof(UIDocument))]
public class StoreActionButtonBinder : MonoBehaviour
{
    public string elementName = "StoreActionButton"; // Name de l’élément dans l’UXML
    public bool requireCanBuy = true;                 // Empêche le clic si no-funds
    public UnityEvent onClick;                        // Action à déclencher

    private UIDocument _doc;
    private StoreActionButton _button;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;
        _button = root.Q<StoreActionButton>(elementName);
        if (_button != null)
        {
            _button.RegisterCallback<ClickEvent>(HandleClick);
            // Optionnel : afficher le curseur main via style USS si besoin
            //_button.AddToClassList("clickable");
        }
        else
        {
            Debug.LogWarning($"StoreActionButtonBinder: élément '{elementName}' introuvable dans {name}");
        }
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.UnregisterCallback<ClickEvent>(HandleClick);
    }

    private void HandleClick(ClickEvent evt)
    {
        if (_button == null) return;
        if (requireCanBuy && !_button.canBuy) return; // bloque si fonds insuffisants
        onClick?.Invoke();
    }
}