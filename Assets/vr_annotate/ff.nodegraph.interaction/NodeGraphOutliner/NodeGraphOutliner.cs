﻿using System.Collections;
using System.Collections.Generic;
using ff.vr.interaction;
using UnityEngine;

namespace ff.nodegraph.interaction
{
    public class NodeGraphOutliner : MonoBehaviour
    {
        public Node root { get; set; }

        [HideInInspector]
        public List<Node> selectedNodes = new List<Node>();

        [Header("--- internal prefab references-----")]

        [SerializeField]
        NodeGraphOutlinerItem _itemPrefab;

        [SerializeField]
        Transform _itemsContainer;

        void Start()
        {
            _nodeSelectionManager = GameObject.FindObjectOfType<NodeSelectionManager>();
            RebuildUI();
        }

        void Update()
        {
            RebuildUI();
        }

        public void SetSelectedNode(Node newNode)
        {
            _selectedNode = newNode;
            selectedNodes.Clear();

            if (newNode != null)
            {
                selectedNodes.Add(newNode);
            }
            RebuildUI();
        }

        private void RebuildUI()
        {
            ClearItems();

            if (selectedNodes.Count > 1)
            {
                Debug.LogWarning("graph list only supports one item");
            }

            // Path to selection
            if (selectedNodes.Count == 1)
            {
                var node = selectedNodes[0];
                var path = new List<Node>();

                // Collect path to get indentation level
                while (node != null)
                {
                    path.Insert(0, node);
                    node = node.Parent;
                }

                for (var indentIndex = 0; indentIndex < path.Count; indentIndex++)
                {
                    InsertItem(path[indentIndex], indentIndex, indentIndex);
                }

                foreach (var child in path[path.Count - 1].Children)
                    InsertItem(child, -1, path.Count);
            }

            // Add other graph-root nodes before and after
            var insertionIndex = 0;
            foreach (var ng in _nodeSelectionManager.NodeGraphs)
            {
                // Skip already added root
                if (HasAlreadyInsertedNode(ng.Node))
                {
                    insertionIndex = _items.Count;
                    continue;
                }
                InsertItem(ng.Node, insertionIndex);
                insertionIndex++;
            }
            LayoutItems();
        }


        private bool HasAlreadyInsertedNode(Node node)
        {
            foreach (var n in _items)
            {
                if (node == n.Node)
                {
                    return true;
                }
            }
            return false;
        }


        /** Use index -1 to append */
        private NodeGraphOutlinerItem InsertItem(Node node, int index = -1, int indentation = 0)
        {
            // -1 appends to list
            if (index == -1)
                index = _items.Count;

            var newItem = GameObject.Instantiate(_itemPrefab);
            _items.Insert(index, newItem);
            newItem.Indentation = indentation;
            newItem.IsSelected = node == _selectedNode;
            newItem.IsHovered = node == _nodeSelectionManager.HoveredNode;
            newItem.name += "-" + node.Name;
            newItem.Node = node;

            var prefix = (node.Children.Length == 0) ? "- " : "+ ";
            newItem.Text = prefix + node.Name;

            newItem.transform.SetParent(_itemsContainer, false);
            newItem.SceneGraphPanel = this;
            return newItem;
        }

        private void LayoutItems()
        {
            _localPositionOfSelectedItem = Vector3.zero;

            for (var index = 0; index < _items.Count; index++)
            {
                var pos = new Vector3(0, -LINE_HEIGHT * index, 0);
                _items[index].transform.localPosition = pos;
                if (_items[index].Node == _selectedNode)
                {
                    _localPositionOfSelectedItem = pos;
                }
            }
        }


        private void ClearItems()
        {
            foreach (var item in _items)
            {
                GameObject.DestroyImmediate(item.gameObject);
            }
            _items.Clear();
        }


        private float LINE_HEIGHT = 0.1f;
        private Node _selectedNode;

        public Vector3 PositionOfSelectedItem
        {
            get
            {
                //var pInWorld = transform.InverseTransformVector(_localPositionOfSelectedItem);
                //Debug.Log("pWorld:" + pInWorld + " <--- local: " + _localPositionOfSelectedItem);
                //return this.transform.position;
                return this.transform.TransformPoint(_localPositionOfSelectedItem);
            }
        }

        private Vector3 _localPositionOfSelectedItem;
        private List<NodeGraphOutlinerItem> _items = new List<NodeGraphOutlinerItem>();
        private NodeSelectionManager _nodeSelectionManager;
    }
}
