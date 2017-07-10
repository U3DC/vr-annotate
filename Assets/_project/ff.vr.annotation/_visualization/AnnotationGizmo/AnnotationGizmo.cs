using UnityEngine;
using ff.vr.annotate;
using ff.nodegraph;
using ff.vr.interaction;
using System;

namespace ff.vr.annotate
{
    public class AnnotationGizmo : MonoBehaviour, ISelectable
    {

        [Header("--- internal prefab references-----------")]
        [SerializeField]
        TMPro.TextMeshPro _annotationObjectLabel;
        [SerializeField]
        TMPro.TextMeshPro _annotationBodyLabel;
        [SerializeField]
        TMPro.TextMeshPro _authorLabel;
        [SerializeField]
        TMPro.TextMeshPro _annotationDateLabel;

        [SerializeField]
        GameObject _hoverGroup;

        Color SelectedColor;

        void Update()
        {
            utils.Helpers.FaceCameraAndKeepSize(this.transform, DEFAULT_SIZE);
        }

        /** Called from annotation manager */
        public void SetAnnotation(Annotation newAnnotation)
        {
            _annotation = newAnnotation;
            UpdateVisibility();
        }


        private void UpdateVisibility()
        {
            if (_annotation != null)
            {
                _annotationObjectLabel.text = _annotation.TargetNode != null ? _annotation.TargetNode.Name : "<Without Object>"; // FIXME: Needs to be implemented
                _annotationBodyLabel.text = _annotation.Text;
                _authorLabel.text = _annotation.Author.name;
                _annotationDateLabel.text = _annotation.CreatedAt.ToString("yyyy/MM/dd");
            }
            _hoverGroup.SetActive(_isHovered);
        }

        public void UpdateBodyText(string newText)
        {
            _annotationBodyLabel.text = newText;
        }


        public void OnHover()
        {
            _isHovered = true;
            UpdateVisibility();
        }

        public void OnUnhover()
        {
            _isHovered = false;
            UpdateVisibility();
        }

        public void Select()
        {
            _isSelected = true;
            InformationPanel._instance.SetSelection(this);
        }

        #region implemented ISelectable
        public void OnSelected()
        {
            UpdateVisibility();
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }
        #endregion

        private bool _isHovered = false;
        private bool _isSelected = false;
        private float _startTime;
        private Annotation _annotation;
        private const float DEFAULT_SIZE = 0.3f;
    }
}
