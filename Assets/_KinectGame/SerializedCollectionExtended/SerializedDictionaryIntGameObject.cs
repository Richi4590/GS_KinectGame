using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.SerializedCollections
{
    public class SerializedDictionaryIntGameObject : MonoBehaviour
    {
        [SerializedDictionary("ArucoCodeIntID", "GameObjectPrefab")]
        public SerializedDictionary<int, GameObject> dict;
    }
} 