using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using static GameXMLConfig.Config;


public class LoadGameConfig : MonoBehaviour
{

    [SerializeField] private string fileNameInStreamingAssetsFolder = "gameConfig.xml";
    private GameXMLConfig config;
    private static LoadGameConfig instance;
    public static Dictionary<string, string> gameConfigMap;
    public static Dictionary<string, Color> gameColorConfigMap;

    private void Awake()
    {
        instance = this;
        gameConfigMap = new Dictionary<string, string>();
        gameColorConfigMap = new Dictionary<string, Color>();
        LoadConfig();

        foreach (GameConfigLoaderForClass instance in Utilities.FindGeneral<GameConfigLoaderForClass>())
        {
            if (instance.loadConfigFromGameXMLFile)
                instance.LoadGameConfigForClass();
        }
    }

    private void LoadConfig()
    {
        string aPathToConfigXML = Path.Combine(Application.streamingAssetsPath, fileNameInStreamingAssetsFolder);
        if (File.Exists(aPathToConfigXML))
        {
            config = GameXMLConfig.Load(aPathToConfigXML);
            Debug.Log("no errors occured during GAME config file load");

            Color col;

            for (int i = 0; i < config.ConfigNodes.Length; i++)
            {
                GameXMLConfig.Config configNode = config.ConfigNodes[i];

                if (configNode.Value != null)
                    gameConfigMap.Add(configNode.Name, configNode.Value);

                if (configNode.r > 0 || configNode.g > 0 || configNode.b > 0 || configNode.r > 0)
                {
                    float test = Mathf.Clamp(configNode.r, 0, 255);

                    col.r = Mathf.Clamp(configNode.r, 0, 255) / 255.0f;
                    col.g = Mathf.Clamp(configNode.g, 0, 255) / 255.0f;
                    col.b = Mathf.Clamp(configNode.b, 0, 255) / 255.0f;
                    col.a = Mathf.Clamp(configNode.a, 0, 255) / 255.0f;
                    gameColorConfigMap.Add(configNode.Name, col);
                }
            }
        }
        else
        {
            Debug.LogWarning($"No GAME config file found at {aPathToConfigXML}. Using default settings: ");
        }
    }

    public static LoadGameConfig Instance()
    {
        return instance;
    }
}
