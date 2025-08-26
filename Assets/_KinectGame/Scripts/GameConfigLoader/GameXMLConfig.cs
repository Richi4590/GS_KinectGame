using System.IO;
using System.Xml.Serialization;

[XmlRoot("GameXMLConfig")]
public class GameXMLConfig
{
    public class Config
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlText]
        public string Value;

        public int r = -1;
        public int g = -1;
        public int b = -1;
        public int a = -1;
    }

    [XmlArray("ConfigNodes")]
    [XmlArrayItem("Config")]
    public Config[] ConfigNodes;

    public void Save(string path)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(GameXMLConfig));
        using FileStream stream = new FileStream(path, FileMode.Create);
        xmlSerializer.Serialize(stream, this);
    }

    public static GameXMLConfig Load(string path)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(GameXMLConfig));
        using FileStream stream = new FileStream(path, FileMode.Open);
        return xmlSerializer.Deserialize(stream) as GameXMLConfig;
    }

    public static GameXMLConfig LoadFromText(string text)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(GameXMLConfig));
        return xmlSerializer.Deserialize(new StringReader(text)) as GameXMLConfig;
    }
}