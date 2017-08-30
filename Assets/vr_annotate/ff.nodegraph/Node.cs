﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ff.vr.interaction;
using UnityEngine;

namespace ff.nodegraph
{
    [System.Serializable]
    public class Node : ISelectable
    {
        public struct BoundsWithContextStruct
        {
            public Bounds Bounds;
            public Bounds LocalBounds;
            public Transform LocalTransform;
            public bool HasLocalBounds;
        }

        public BoundsWithContextStruct BoundsWithContext = new BoundsWithContextStruct();
        public bool IsAnnotatable;
        public bool HasGeometry;
        public string Name;
        public bool HasBounds = false;
        public System.Guid Id;

        public float HitDistance;

        [System.NonSerializedAttribute]
        public Node Parent;

        [System.NonSerializedAttribute]
        public Node[] Children;

        [System.NonSerializedAttribute]
        public GameObject UnityObj;

        public void PrintStructure(int level = 0)
        {
            Debug.LogFormat(new System.String(' ', level) + this.Name);
            foreach (var c in Children)
            {
                c.PrintStructure(level + 1);
            }
        }

        public Node RootNode
        {
            get
            {
                var root = this;
                while (root.Parent != null)
                {
                    root = root.Parent;
                }
                return root;
            }
        }

        public bool IsSelected { get; set; }

        public NodeGraph NodeGraphRoot
        {
            get
            {
                if (RootNode == null || RootNode.UnityObj == null)
                    return null;

                return RootNode.UnityObj.GetComponent<NodeGraph>();
            }
        }

        public string NodePath
        {
            get
            {
                var sb = new StringBuilder();

                var n = this;
                sb.Append(n.Name);
                while (n.Parent != null)
                {
                    n = n.Parent;
                    sb.Insert(0, "/");
                    sb.Insert(0, n.Name);
                }

                return sb.ToString();
            }
        }

        // FIXME: this should be the contructor
        public static Node FindChildNodes(GameObject unityObj)
        {
            var node = new Node()
            {
                Name = unityObj.name,
                Children = new Node[unityObj.transform.childCount],
                Id = new System.Guid(),
                UnityObj = unityObj,
            };
            Debug.Log("unityObj defined: " + node.UnityObj.transform.localRotation + " rotation of " + node.UnityObj);
            node.IsAnnotatable = node.CheckIfObjectIsAnnotatable();

            var renderer = unityObj.GetComponent<MeshRenderer>();
            var meshfilter = unityObj.GetComponent<MeshFilter>();
            if (renderer != null && unityObj.GetComponent<IgnoreNode>() == null)
            {
                node.HasBounds = true;

                node.BoundsWithContext.Bounds = renderer.bounds;
                node.BoundsWithContext.HasLocalBounds = meshfilter != null;
                if (node.BoundsWithContext.HasLocalBounds)
                {
                    node.BoundsWithContext.LocalBounds = meshfilter.mesh.bounds;
                    node.BoundsWithContext.LocalTransform = unityObj.transform;
                }

                node.HasGeometry = true;
            }

            for (int index = 0; index < unityObj.transform.childCount; index++)
            {
                var childObj = unityObj.transform.GetChild(index).gameObject;
                var childNode = FindChildNodes(childObj);
                childNode.Parent = node;

                node.Children[index] = childNode;

                if (childNode.HasBounds)
                {
                    if (node.HasBounds)
                    {
                        node.BoundsWithContext.Bounds.Encapsulate(childNode.BoundsWithContext.Bounds);
                        node.HasBounds = true;
                    }
                    else
                    {
                        node.BoundsWithContext.Bounds = childNode.BoundsWithContext.Bounds;
                        node.HasBounds = childNode.HasBounds;
                    }
                }
            }
            return node;
        }

        public static void PrintBound(Bounds b)
        {
            Debug.Log("bounds: center: " + b.center + " , size: " + b.size);
        }

        public void CollectLeavesIntersectingRay(Ray ray, List<Node> hits)
        {
            if (this.BoundsWithContext.HasLocalBounds)
            {
                var localRayOrigin = UnityObj.transform.InverseTransformPoint(ray.origin);
                var localRayDirection = UnityObj.transform.InverseTransformDirection(ray.direction);
                if (!this.BoundsWithContext.LocalBounds.IntersectRay(new Ray(localRayOrigin, localRayDirection), out HitDistance))
                    return;
            }
            else
            {
                if (!this.BoundsWithContext.Bounds.IntersectRay(ray, out HitDistance))
                    return;
            }

            Debug.Log("Ray hit Node " + this.UnityObj + " , LocalBounds?: " + BoundsWithContext.LocalBounds + " , HasGeometry?: " + HasGeometry);

            // Set back distance to favor selection of smaller object
            // if (HitDistance > 0)
            // {
            //     var hitPoint = ray.origin + ray.direction * HitDistance;
            //     var backed = Vector3.Lerp(hitPoint, Bounds.center, 0.98f);
            //     var backDistance = Vector3.Distance(ray.origin, backed);
            //     HitDistance = backDistance;
            // }

            if (this.HasGeometry)
                hits.Add(this);

            if (Children == null)
                return;

            foreach (var child in Children)
            {
                child.CollectLeavesIntersectingRay(ray, hits);
            }
        }


        public bool CheckIfObjectIsAnnotatable()
        {
            //GameObject obj = this.UnityObj;
            return true;
        }

        public List<BoundsWithContextStruct> CollectBoundsWithContext(List<BoundsWithContextStruct> result = null)
        {
            if (result == null)
                result = new List<BoundsWithContextStruct>();

            if (this.HasGeometry)
            {
                result.Add(this.BoundsWithContext);
            }

            foreach (var c in Children)
            {
                c.CollectBoundsWithContext(result);
            }
            return result;
        }

        public Vector3 GetPosition()
        {
            return this.BoundsWithContext.Bounds.center;
        }


        private bool IntersectsWithRay(Ray ray)
        {

            var intersects = this.BoundsWithContext.Bounds.IntersectRay(ray, out HitDistance);
            return intersects;
        }
    }
}