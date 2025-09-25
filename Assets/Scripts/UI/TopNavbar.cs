using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    [UxmlElement]
    public partial class TopNavbar : VisualElement
    {
        public TopNavbar()
        {
            // On attend que l'UXML ait fini d'ajouter les enfants pour pouvoir les "re-slotter"
            RegisterCallback<AttachToPanelEvent>(_ => Build());
        }

        private bool _built;
        private VisualElement _left, _center, _right;

        private void Build()
        {
            if (_built) return;
            _built = true;

            // 1) Récupère les enfants déjà présents (depuis l'UXML)
            var extras = this.Children().ToList();

            // 2) Reset et (re)construit la barre
            Clear();
            AddToClassList("topbar");

            _left = new VisualElement { name = "Section_Left" };
            _left.AddToClassList("section");

            _center = new VisualElement { name = "Section_Center" };
            _center.AddToClassList("section");
            _center.AddToClassList("center");

            _right = new VisualElement { name = "Section_Right" };
            _right.AddToClassList("section");

            Add(_left); Add(_center); Add(_right);

            // 3) Re-slot : range chaque enfant UXML dans la bonne section
            foreach (var e in extras)
            {
                if (e.parent != null) e.RemoveFromHierarchy();
                var classes = e.GetClasses();
                if (classes.Contains("slot-right")) _right.Add(e);
                else if (classes.Contains("slot-center")) _center.Add(e);
                else _left.Add(e);     // défaut
            }
        }

        public VisualElement Left => _left;
        public VisualElement Center => _center;
        public VisualElement Right => _right;
    }
}