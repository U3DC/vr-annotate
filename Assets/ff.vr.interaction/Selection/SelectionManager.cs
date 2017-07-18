﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ff.vr.interaction
{
    /** Handles and broadcasts information about the current selection */
    public class SelectionManager : MonoBehaviour
    {
        public delegate void SelectionChanged(List<ISelectable> newSelection);
        public SelectionChanged SelectionChangedEvent;

        void Awake()
        {
            if (Instance != null)
                throw new UnityException("" + this + " can only be added once.");

            Instance = this;
        }

        public List<ISelectable> Selection { get { return _selection; } }

        public void SelectItem(ISelectable item)
        {
            // Clear old selection (e.g. send property setter updates)
            foreach (var oldSelected in _selection)
            {
                if (oldSelected != item)
                {
                    oldSelected.IsSelected = false;
                }
            }

            _selection.Clear();

            if (item != null)
            {
                _selection.Add(item);
                item.IsSelected = true;
            }

            if (SelectionChangedEvent != null)
                SelectionChangedEvent(_selection);
        }

        // public bool IsItemSelected(ISelectable item)
        // {
        //     return _selection.Contains(item);
        // }

        private List<ISelectable> _selection = new List<ISelectable>();
        public static SelectionManager Instance { get; private set; }
    }
}
