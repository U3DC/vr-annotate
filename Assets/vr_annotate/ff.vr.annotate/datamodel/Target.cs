using System;
using System.Collections.Generic;
using System.Text;
using ff.location;
using ff.nodegraph;
using ff.vr.annotate.viz;
using UnityEngine;
using ff.utils;
using System.Text.RegularExpressions;
using System.Globalization;
using ff.nodegraph.interaction;
using System.IO;
using System.Collections;
using UnityEngine.Networking;

namespace ff.vr.annotate.datamodel
{
    [System.Serializable]
    public class Target : NodeGraph
    {
        public Target() { }

        public Target(JSONObject jSONObject)
        {
            DeserializeFromJson(jSONObject);
        }

        public Guid Guid;

        const string ANNOTATION_ID_PREFIX = "http://annotator/anno/";

        public string JsonLdId { get { return ANNOTATION_ID_PREFIX + Guid; } }

        public string TargetDirectory { get { return Application.dataPath + "/db/targets/"; } }

        public bool SyncWithDataBaseTrigger;

        void Update()
        {
            if (SyncWithDataBaseTrigger)
            {
                SyncWithDataBase();
                SyncWithDataBaseTrigger = false;
            }

        }
        private JSONObject jsonObjectForServer;
        private void SyncWithDataBase()
        {
            StartCoroutine(ReadAllTargetsFromServer());

            bool targetExistsInDB = false;
            foreach (var targetJSON in jsonObjectForServer)
            {
                if (targetJSON["@targetGUID"].str == Guid.ToString())
                    targetExistsInDB = true;
            }

            if (targetExistsInDB)
            {
                Debug.Log("Target " + Guid.ToString() + " allready exists in Database.");
            }
            else
            {
                WriteTargetToServer();
                Debug.Log("Wrote Target " + Guid.ToString() + " to  Server");
            }
        }


        #region serialization

        private void WriteTargetToLocalDirectory()
        {
            File.WriteAllText(TargetDirectory + Guid + ".json", ToJson());

        }

        private void WriteTargetToServer()
        {
            var targetJson = ToJson();

            UnityWebRequest www = UnityWebRequest.Put("http://127.0.0.1:8301/targets/" + Guid, System.Text.Encoding.UTF8.GetBytes(targetJson));
            www.SetRequestHeader("Content-Type", "application/json");

            www.Send();

            if (www.isNetworkError)
                Debug.Log(www.error);
            else
                Debug.Log("Upload complete with: " + www.error);
        }

        public string ToJson()
        {
            return JsonTemplate.FillTemplate(JSONTemplate, new Dictionary<string, string>() {
                {"@id",  Guid.ToString()},
                {"nodeGraph", SerializeNodeTreeRecursive(Node).Replace("'", "\"")},
            });
        }

        private string SerializeNodeTreeRecursive(Node currentNode)
        {
            var json = "{ \"@guid\" : \" " + currentNode.GUID.ToString() + " \",  \"children\" : [";

            for (int i = 0; i < currentNode.Children.Length; i++)
            {
                if (i != 0)
                    json += ",";

                json += SerializeNodeTreeRecursive(currentNode.Children[i]);
            }
            json += "]}";
            return json;
        }

        #endregion


        #region deserialization


        private IEnumerator ReadAllTargetsFromServer()
        {
            UnityWebRequest www = UnityWebRequest.Get("http://127.0.0.1:8301/targets/");
            yield return www.Send();

            if (www.isNetworkError)
                Debug.Log(www.error);
            else
                Debug.Log("Download complete: " + www.downloadHandler.text);

            jsonObjectForServer = new JSONObject(www.downloadHandler.text);

        }

        private List<JSONObject> ReadAllTargetsFromLocalDirectory()
        {
            var allNodes = new List<JSONObject>();
            var filesInDirectory = Directory.GetFiles(TargetDirectory, "*.json");
            foreach (var file in filesInDirectory)
            {
                allNodes.Add(new JSONObject(File.ReadAllText(file)));
            }
            return allNodes;
        }

        public static Target DeserializeFromJson(JSONObject jsonObject)
        {
            var newTarget = new Target();
            newTarget.GUID = new Guid(jsonObject["@id"].str);
            newTarget.Node = DeserializeGraph(jsonObject["nodeGraph"]);
            return newTarget;
        }

        private static Node DeserializeGraph(JSONObject jsonObject)
        {
            var newNode = new Node();

            var GUIDString = jsonObject["@guid"].str;
            newNode.GUID = new Guid(GUIDString);

            var children = new List<Node>();
            foreach (var childJsonNode in jsonObject["children"])
            {
                children.Add(DeserializeGraph(childJsonNode));
            }

            newNode.Children = children.ToArray();
            return newNode;
        }

        #endregion


        private static string JSONTemplate = @"
        {
            '@context': 
            {
                '@vocab':'http://www.w3.org/ns/target.jsonld',
                '@base': 'http://annotator/target/'
            },
            '@id' : '{@id}',
            '@type': '@AnnotationTarget',
            'creator': {'id':'_alan','name':'Alan','email':'alan @google.com'},
            'created': '7/10/2017 7:02:44 PM',
            'generator': 
            {
                'id': 'http://vr-annotator/v/02',
                'type': 'Software',
                'name': 'VR-Annotator v0.2'
            },
            'interpretation': 
            {
                'refinedBy': 
                {
                    'modellerName':'',
                    'modellingSoftware':'',
                    'references': []
                }
            },
            'nodeGraph': {nodeGraph}
        }";
    }
}
