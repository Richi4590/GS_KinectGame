using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameConfigLoaderForClass : MonoBehaviour
{
    [SerializeField]
    public bool loadConfigFromGameXMLFile = true;

    /// <summary>
    /// Is called before all standard Awake calls!
    /// Use for example float.Parse(LoadGameConfig.gameConfigMap["..."]);
    /// </summary>
    public abstract void LoadGameConfigForClass();
}
