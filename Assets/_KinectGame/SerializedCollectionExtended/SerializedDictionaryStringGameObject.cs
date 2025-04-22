using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.SerializedCollections
{
    public class SerializedDictionaryStringGameObject : MonoBehaviour
    {
        [SerializedDictionary("QRCodeString", "GameObjectPrefab")]
        public SerializedDictionary<string, GameObject> dict;
    }
} 