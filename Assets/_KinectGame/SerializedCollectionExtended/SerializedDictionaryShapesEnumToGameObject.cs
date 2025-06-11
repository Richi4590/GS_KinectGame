using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.SerializedCollections
{

    public class SerializedDictionaryShapesEnumToGameObject : MonoBehaviour
    {
        [SerializedDictionary("ID", "GameObjectPrefab")]
        public SerializedDictionary<ShapesEnum, GameObject> dict;

        public static void SetObjectPrefabParameters(GameObject g, float longestObjectSide)
        {
            if (!g.TryGetComponent<CustomTag>(out CustomTag t))
                return;

            string tag = t.Tags[0].ToLower(); 

            switch (tag)
            {
                case "default":
                    break;
                case "magnet":
                    g.GetComponent<PullPushMagnet>().effectRadius = longestObjectSide;
                    break;
                case "windblower":
                    WindBlower b = g.GetComponent<WindBlower>();
                    b.effectRadius = longestObjectSide;
                    b.boxHeight = longestObjectSide;
                    b.boxWidth = longestObjectSide;
                    break;
                case "reflector":
                    break;
                default:
                    break;
            }
        }
    }
} 