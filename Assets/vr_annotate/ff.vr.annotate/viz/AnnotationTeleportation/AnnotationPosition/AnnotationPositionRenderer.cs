﻿using System;
using System.Collections;
using System.Collections.Generic;
using ff.utils;
using ff.vr.annotate;
using ff.vr.annotate.datamodel;
using UnityEngine;

namespace ff.vr.interaction
{
    public class AnnotationPositionRenderer : MonoBehaviour
    {
        private bool Selected;
        private bool Hovered = true;

        [Header("--- configuration -----")]

        [SerializeField]
        private Color DefaultColor;
        [SerializeField]
        private Color HoverColor;
        [SerializeField]
        private Color SelectedColor;
        [SerializeField]
        private Color HoverSelectedColor;

        [SerializeField]
        private AnimationCurve AlphaOverVisibility;
        [SerializeField]
        private AnimationCurve LineLengthOverVisibility;
        [SerializeField]
        private AnimationCurve LineWidthOverVisibility;

        public AnnotationGizmo RenderedAnnotationGizmo;

        [Header("--- internal prefab references -----")]
        [SerializeField]
        private LineRenderer _lineToAnnotation;
        [SerializeField]
        private GameObject _target;
        [SerializeField]
        private GameObject _silhouette;
        [SerializeField]
        private Transform _lineAnchor;

        void Start()
        {
            _initialScale = this.transform.localScale;
            _allRenderers = GetComponentsInChildren<Renderer>();
        }

        void OnEnable()
        {
            SelectionManager.Instance.SelectedAnnotationGizmoChangedEvent += SelectedAnnotationGizmoChangedHandler;
            SelectionManager.Instance.OnAnnotationGizmoHover += OnAnnotationGizmoHoverHandler;
            SelectionManager.Instance.OnAnnotationGizmoUnhover += OnAnnotationGizmoUnhoverHandler;
        }

        void OnDisable()
        {
            SelectionManager.Instance.SelectedAnnotationGizmoChangedEvent -= SelectedAnnotationGizmoChangedHandler;
            SelectionManager.Instance.OnAnnotationGizmoHover -= OnAnnotationGizmoHoverHandler;
            SelectionManager.Instance.OnAnnotationGizmoUnhover -= OnAnnotationGizmoUnhoverHandler;
        }

        private void SelectedAnnotationGizmoChangedHandler(AnnotationGizmo annotationGizmo)
        {
            Selected = (annotationGizmo != null) && (annotationGizmo == RenderedAnnotationGizmo);
        }

        private void OnAnnotationGizmoUnhoverHandler(AnnotationGizmo annotationGizmo)
        {
            if (RenderedAnnotationGizmo == annotationGizmo && RenderedAnnotationGizmo != null)
                Hovered = false;
        }

        private void OnAnnotationGizmoHoverHandler(AnnotationGizmo annotationGizmo)
        {
            if (RenderedAnnotationGizmo == annotationGizmo && RenderedAnnotationGizmo != null)
                Hovered = true;
        }

        void Update()
        {

            if (Hovered || Selected)
                _shouldHaveVisibility = 1;
            else
                _shouldHaveVisibility = 0;

            _visibility = Mathf.Lerp(_visibility, _shouldHaveVisibility, 0.2f);
            _visibility = _shouldHaveVisibility;
            var isVisible = _visibility > 0.001f;

            if (Hovered || Selected || isVisible)
                RenderAnnotationPositionForCurrentVisibility();
            else
                GameObject.Destroy(gameObject);
        }


        private void RenderAnnotationPositionForCurrentVisibility()
        {
            foreach (var r in _allRenderers)
                r.enabled = true;

            _target.transform.localScale = _initialScale * (MINSIZE + _visibility * (1 - MINSIZE));


            if (Hovered || Selected)
            {
                _silhouette.SetActive(true);
                _silhouette.transform.LookAt(Camera.main.transform.position);

                if (RenderedAnnotationGizmo != null)
                {
                    var annotationInSilhouetteSpace = _silhouette.transform.InverseTransformPoint(RenderedAnnotationGizmo.Annotation.AnnotationPosition.position);
                    if (annotationInSilhouetteSpace.x > 0)
                    {
                        _silhouette.transform.localScale = new Vector3(-1, 1, 1);
                        UpdateLine();

                    }

                }
                // _silhouette.transform.localRotation = Quaternion.Euler(0, _silhouette.transform.localRotation.y, 0);

            }
            else
            {
                _silhouette.SetActive(false);
            }

            foreach (var r in _allRenderers)
            {
                Color color;
                if (Selected)
                {
                    if (Hovered)
                        color = HoverSelectedColor;
                    else
                        color = SelectedColor;
                }
                else
                {
                    if (Hovered)
                        color = HoverColor;
                    else
                        color = DefaultColor;
                }

                var colorWithAlpha = new Color(color.r, color.g, color.b, color.a * AlphaOverVisibility.Evaluate(_visibility));
                r.material.SetColor("_Color", colorWithAlpha);
                r.material.SetColor("_EmissionColor", colorWithAlpha);
            }
            if (Hovered)
            {
                _lineToAnnotation.gameObject.SetActive(true);
                _lineToAnnotation.widthMultiplier = LineWidthOverVisibility.Evaluate(_visibility);
                _lineToAnnotation.material.color = new Color(HoverColor.r, HoverColor.g, HoverColor.b, HoverColor.a * AlphaOverVisibility.Evaluate(_visibility));
            }
            else
            {
                _lineToAnnotation.gameObject.SetActive(false);
            }
        }

        // todo should be Constructor
        public void SetAnnotationData(AnnotationGizmo annotationGizmo)
        {
            RenderedAnnotationGizmo = annotationGizmo;
            var annotation = RenderedAnnotationGizmo.Annotation;
            transform.position = new Vector3(annotation.ViewPointPosition.position.x, 0, annotation.ViewPointPosition.position.z);
            UpdateLine();
        }

        public void UpdateLine()
        {

            _lineStartPosition = _lineAnchor.position;
            _lineEndPosition = RenderedAnnotationGizmo.Annotation.AnnotationPosition.position;

            _lineToAnnotation.SetPosition(0, _lineStartPosition);
            _lineToAnnotation.SetPosition(1, _lineEndPosition);
        }

        private float _shouldHaveVisibility = 1;
        private float _visibility = 0;

        Vector3 _initialScale = Vector3.one;

        private Renderer[] _allRenderers;

        private Vector3 _lineStartPosition;
        private Vector3 _lineEndPosition;

        private const float MINSIZE = 0.9f;

    }
}