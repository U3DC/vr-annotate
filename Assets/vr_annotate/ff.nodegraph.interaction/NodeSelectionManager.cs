﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using ff.utils;
using ff.vr.interaction;
using ff.vr.annotate.viz;

using ff.vr.annotate;

namespace ff.nodegraph.interaction
{
    /** A singleton that handles hit-detection within a node-structure.  */
    public class NodeSelectionManager : MonoBehaviour, IClickableLaserPointerTarget, IHitTester
    {
        private Node SelectedNode = null;

        private Node HoveredNode = null;
        public bool DeepPickingEnabled = false;

        public NodeSelectionMarker _selectionMarker;

        [HideInInspector]
        [NonSerializedAttribute]
        public Dictionary<System.Guid, Node> NodesByGuid = new Dictionary<System.Guid, Node>();

        [Header("--- prefab references ----")]

        [SerializeField]
        TMPro.TextMeshPro _hoverLabel;
        [SerializeField] Renderer _highlightContextRenderer;
        [SerializeField] Renderer _highlightHoverRenderer;

        static public NodeSelectionManager _instance = null;

        void Awake()
        {
            SelectedNode = null;
            if (_instance != null)
            {
                throw new UnityException("NodeSelectionManager can only be added once");
            }
            _instance = this;

            NodeGraphs = FindObjectsOfType<NodeGraph>();
            foreach (var ng in NodeGraphs)
            {
                Debug.Log("initializing graph:" + ng.name + " / " + ng.RootNodeId);
            }
            _annotationManager = FindObjectOfType<AnnotationManager>();
        }

        void Start()
        {
            if (SelectionManager.Instance == null)
            {
                throw new UnityException("" + this + " requires a SelectionManager to be initialized. Are you missing an instance of SelectionManager or is the script execution order incorrect?");
            }
            SelectionManager.Instance.SelectionChangedEvent += SelectionChangedHander;
        }


        private void SelectionChangedHander(List<ISelectable> newSelection)
        {
            var nodeOrNull = (newSelection.Count == 1) ? newSelection[0] as Node : null;
            SetSelectedNode(nodeOrNull);
        }


        public Node FindNodeFromPath(string rootNodeId, string nodePath)
        {
            foreach (var ag in NodeGraphs)
            {
                if (ag.RootNodeId == rootNodeId)
                {
                    var nodeNames = new List<string>(nodePath.Split('/'));
                    var node = ag.Node;

                    // remove rootnode
                    if (node.Name == nodeNames[0])
                    {
                        nodeNames.RemoveAt(0);
                        if (nodeNames.Count == 0)
                            return node;
                    }

                    var stillSearching = true;
                    while (stillSearching)
                    {
                        stillSearching = false;
                        foreach (var child in node.Children)
                        {
                            if (child.Name == nodeNames[0])
                            {
                                nodeNames.RemoveAt(0);
                                if (nodeNames.Count == 0)
                                    return child;

                                node = child;
                                stillSearching = true;
                                break;
                            }
                        }
                    }
                }
            }
            Debug.LogWarningFormat("Scene does not contain reference to: {0} -> {1}", rootNodeId, nodePath);

            return null;
        }


        /*
        Read carefully! This part is tricky...

        This method is called from LaserPointer on Update() additional to
        a normal Physics.RayCast. If both return hitResults the one with the
        smaller distance will be used. If this wins, NodeHitTester.PointerEnter(). 
        We then can use the _lastNodeHitByRay to update the visualization respectively.
        */
        public Node FindAndRememberHit(Ray ray)
        {
            _lastNodeHitByRay = FindHit(ray);
            return _lastNodeHitByRay;
        }


        #region implement LaserInterface
        public void PointerEnter(LaserPointer pointer)
        {
            _hoverLabel.gameObject.SetActive(true);
            HoveredNode = _lastNodeHitByRay;
            UpdateHoverHighlight();
        }

        public void PointerUpdate(LaserPointer pointer)
        {
            if (_lastNodeHitByRay != _renderedNode)
            {
                HoveredNode = _lastNodeHitByRay;
                UpdateHoverHighlight();
                _renderedNode = _lastNodeHitByRay;

            }
            _lastHoverPosition = pointer.LastHitPoint;
            _hoverLabel.transform.position = pointer.LastHitPoint;
            _hoverLabel.transform.LookAt(_hoverLabel.transform.position - Camera.main.transform.position + _hoverLabel.transform.position);
        }


        public void PointerExit(LaserPointer pointer)
        {
            HoveredNode = null;
            UpdateHoverHighlight();
            _lastNodeHitByRay = null;    // really?
            _hoverLabel.gameObject.SetActive(false);
        }
        #endregion implement LaserInterface


        public void PointerTriggered(LaserPointer pointer)
        {
            if (HoveredNode != null)
            {
                SelectionManager.Instance.SelectItem(HoveredNode);
                _selectionMarker.SetPosition(_lastHoverPosition);
            }
        }

        public void CreateAnnotation()
        {
            if (SelectedNode != null)
            {
                _annotationManager.CreateAnnotation(SelectedNode, _lastHoverPosition);
            }
        }


        public void PointerUntriggered(LaserPointer pointer)
        {

        }

        public void SelectParentNode()
        {
            if (this.SelectedNode == null)
            {
                Debug.LogWarning("Tried to select parent when no selected?");
                return;
            }

            if (this.SelectedNode.Parent == null)
            {
                Debug.LogWarning("Tried to select parent when current selection had no parent?");
                return;
            }
            SelectionManager.Instance.SelectItem(SelectedNode.Parent);
        }


        private void SetSelectedNode(Node newSelectedNode)
        {
            if (newSelectedNode == SelectedNode)
                return;

            SelectedNode = newSelectedNode;

            if (SelectedNode == null)
            {
                _highlightContextRenderer.enabled = false;
            }
            else
            {
                var bounds = SelectedNode.CollectGeometryBounds().ToArray();
                _highlightContextRenderer.GetComponent<MeshFilter>().mesh = GenerateMeshFromBounds.GenerateMesh(bounds);
                _highlightContextRenderer.enabled = true;
            }
        }


        #region Hit Detection
        private Node FindHit(Ray ray)
        {
            var hits = new List<Node>();
            if (SelectedNode != null)
            {
                SelectedNode.CollectLeavesIntersectingRay(ray, hits);
            }
            else
            {
                foreach (var ag in NodeGraphs)
                {
                    ag.Node.CollectLeavesIntersectingRay(ray, hits);
                }
            }
            if (hits.Count == 0)
                return null;

            var closestHitNode = FindClosestHit(hits);

            if (closestHitNode == null)
            {
                return null;
            }

            if (DeepPickingEnabled)
            {
                return closestHitNode;
            }

            if (closestHitNode == SelectedNode)
            {
                return null;
            }

            // Walk up to find child of context
            var n = closestHitNode;
            while (n.Parent != SelectedNode && n.Parent != null)
            {
                n = n.Parent;
            }
            n.HitDistance = closestHitNode.HitDistance;

            if (n == SelectedNode)
            {
                Debug.Log("self selection!");
            }
            return n;
        }


        private Node FindClosestHit(List<Node> hits)
        {
            hits.Sort((h1, h2) => (h1.HitDistance).CompareTo(h2.HitDistance));

            foreach (var h in hits)
            {
                if (h.HitDistance > 0)
                {
                    return h;
                }
            }
            return null;
        }
        #endregion



        private void UpdateHoverHighlight()
        {
            if (HoveredNode == null)
            {
                _hoverLabel.gameObject.SetActive(false);
                _highlightHoverRenderer.enabled = false;
            }
            else
            {
                _hoverLabel.text = HoveredNode.Name;
                var bounds = HoveredNode.CollectGeometryBounds().ToArray();
                _highlightHoverRenderer.GetComponent<MeshFilter>().mesh = GenerateMeshFromBounds.GenerateMesh(bounds);
                _highlightHoverRenderer.enabled = true;
            }
        }





        private Vector3 _lastHoverPosition;
        private TrackpadButtonUI _trackpadButtonUI;
        private Node _lastNodeHitByRay;
        private Node _renderedNode;
        private SteamVR_TrackedController _controller;
        private string _lastResult;
        private NodeSelectionManager _nodeHitTester;

        [HideInInspector]
        public NodeGraph[] NodeGraphs;
        private AnnotationManager _annotationManager;

    }
}