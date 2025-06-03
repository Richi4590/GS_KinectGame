using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AYellowpaper.SerializedCollections
{
    public class SerializedDictionaryStringString : MonoBehaviour
    {
        [SerializedDictionary("AnimationAction", "AnimatorAnimationNameString")]
        public SerializedDictionary<string, string> dict;
    }
} 