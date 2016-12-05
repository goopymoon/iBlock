using UnityEngine;
using System.Collections;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public static class BrickBinarySerializer
{
    public class Color32SerializationSurrogate : ISerializationSurrogate
    {
        // Method called to serialize a Vector3 object
        public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context)
        {

            Color32 color = (Color32)obj;
            info.AddValue("r", color.r);
            info.AddValue("g", color.g);
            info.AddValue("b", color.b);
            info.AddValue("a", color.a);
        }

        // Method called to deserialize a Vector3 object
        public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                           StreamingContext context, ISurrogateSelector selector)
        {

            Color32 color = (Color32)obj;
            color.r = (byte)info.GetValue("r", typeof(byte));
            color.g = (byte)info.GetValue("g", typeof(byte));
            color.b = (byte)info.GetValue("b", typeof(byte));
            color.a = (byte)info.GetValue("a", typeof(byte));
            obj = color;
            return obj;
        }
    }

    public class Vector3SerializationSurrogate : ISerializationSurrogate
    {
        // Method called to serialize a Vector3 object
        public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context)
        {

            Vector3 v3 = (Vector3)obj;
            info.AddValue("x", v3.x);
            info.AddValue("y", v3.y);
            info.AddValue("z", v3.z);
        }

        // Method called to deserialize a Vector3 object
        public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                           StreamingContext context, ISurrogateSelector selector)
        {

            Vector3 v3 = (Vector3)obj;
            v3.x = (float)info.GetValue("x", typeof(float));
            v3.y = (float)info.GetValue("y", typeof(float));
            v3.z = (float)info.GetValue("z", typeof(float));
            obj = v3;
            return obj;
        }
    }

    public static void ExportBrickMesh(string relPath, BrickMesh brickMesh)
    {
        var path = Path.Combine(Application.dataPath, relPath);
        var filePath = Path.Combine(path, brickMesh.name);

        string directoryName = Path.GetDirectoryName(filePath);
        bool exists = Directory.Exists(directoryName);
        if (!exists)
            Directory.CreateDirectory(directoryName);

        FileStream stream = File.Create(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        SurrogateSelector surrogateSelector = new SurrogateSelector();
        Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();
        Color32SerializationSurrogate color32SS = new Color32SerializationSurrogate();

        surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
        surrogateSelector.AddSurrogate(typeof(Color32), new StreamingContext(StreamingContextStates.All), color32SS);
        bf.SurrogateSelector = surrogateSelector;

        bf.Serialize(stream, brickMesh);

        stream.Close();
    }

    public static void ImportBrickMesh(string relPath, ref BrickMesh brickMesh)
    {
        var path = Path.Combine(Application.dataPath, relPath);
        var filePath = Path.Combine(path, brickMesh.name);

        FileStream stream = File.OpenRead(filePath);

        BinaryFormatter bf = new BinaryFormatter();
        SurrogateSelector surrogateSelector = new SurrogateSelector();
        Vector3SerializationSurrogate vector3SS = new Vector3SerializationSurrogate();
        Color32SerializationSurrogate color32SS = new Color32SerializationSurrogate();

        surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), vector3SS);
        surrogateSelector.AddSurrogate(typeof(Color32), new StreamingContext(StreamingContextStates.All), color32SS);
        bf.SurrogateSelector = surrogateSelector;

        brickMesh = (BrickMesh)bf.Deserialize(stream);

        stream.Close();
    }
}
