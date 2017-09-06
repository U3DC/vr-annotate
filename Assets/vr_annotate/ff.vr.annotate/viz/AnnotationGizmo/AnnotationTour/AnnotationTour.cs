﻿using System.Collections;
using System.Collections.Generic;
using ff.utils;
using ff.vr.interaction;
using UnityEngine;

namespace ff.vr.annotate.viz
{
    public class AnnotationTour : Singleton<AnnotationTour>
    {

        [SerializeField] GameObject _annotationContainer;
        [SerializeField] AnnotationGizmo[] _annotationGizmos;

        public AnnotationGizmo GetNextGizmo(AnnotationGizmo gizmo)
        {
            var iGizmo = GetIndexOfGizmoInList(gizmo);
            return _annotationGizmos[(iGizmo + 1) % _annotationGizmos.Length];
        }

        public AnnotationGizmo GetPreviousGizmo(AnnotationGizmo gizmo)
        {
            var iGizmo = GetIndexOfGizmoInList(gizmo);
            return _annotationGizmos[(iGizmo - 1 + _annotationGizmos.Length) % _annotationGizmos.Length];
        }

        public int GetIndexOfGizmoInList(AnnotationGizmo gizmo)
        {
            if (gizmo == null)
            {
                Debug.Log("trying to find null in gizmo list");
                return -1;
            }
            if (_annotationGizmos == null || _annotationGizmos.Length < 1)
                _annotationGizmos = _annotationContainer.GetComponentsInChildren<AnnotationGizmo>();

            for (int iGizmo = 0; iGizmo < _annotationGizmos.Length; iGizmo++)
            {
                // Debug.Log(_annotationGizmos[iGizmo].Annotation.GUID + " =?= " + gizmo.Annotation.GUID);
                if (_annotationGizmos[iGizmo] == gizmo)
                    return iGizmo;
            }
            return -1;
        }

        public int GetLengthOfTour()
        {
            return _annotationGizmos.Length;
        }
    }
}