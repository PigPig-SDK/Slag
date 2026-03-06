using OpenTK.Mathematics;

namespace Models;

public static class OBJFile
{
    private const string VertexToken = "v";
    private const string NormalToken = "vn";
    private const string UvToken = "vt";
    private const string ObjectToken = "o";
    private const string FaceToken = "f";
    private const string MaterialLibToken = "mtllib";
    private const string MaterialToken = "usemtl";
    private const string GroupToken = "g";
    private const string SmoothToken = "s";

    private static bool StreamReaderContainsMultipleObjects(StreamReader reader)
    {
        string? line = "";

        while ((line = reader.ReadLine()) != null)
        {
            string[] parts = line.Split(' ');
            if (parts[0].Equals(ObjectToken))
            {
                reader.DiscardBufferedData();
                reader.BaseStream.Seek(0, SeekOrigin.Begin);//Reset reader.
                return true;
            }
        }
        reader.DiscardBufferedData();
        reader.BaseStream.Seek(0, SeekOrigin.Begin);//Reset reader.
        return false;
    }

    public static void SaveOBJ(StreamWriter writer)
    {
        if(SceneHierarchy.Instance is null) throw new InvalidOperationException("SceneHierarchy not initialized.");

        //TODO: OPTIMIZE TO REMOVE DUPLICATES!
        foreach(Model model in SceneHierarchy.Instance.GetModels(HierarchyType.Model))
        {
            writer.WriteLine($"{ObjectToken} {model.ObjectName}");
            //Write all verts to list
            foreach(Vertex v in model.Verticies)
                writer.WriteLine($"{VertexToken} {v.Position.X} {v.Position.Y} {v.Position.Z}");
            //Write normals
            foreach (Vertex v in model.Verticies)
                writer.WriteLine($"{NormalToken} {v.Normal.X} {v.Normal.Y} {v.Normal.Z}");
            //Write uv
            foreach (Vertex v in model.Verticies)
                writer.WriteLine($"{UvToken} {v.UV.X} {v.UV.Y}");
            //Write faces
            foreach(Face face in model.Faces)
            {
                writer.WriteLine($"{FaceToken} {string.Join(" ", face.Indicies.Select(i => $"{i + 1}/{i + 1}/{i + 1}").ToArray())}");
            }
        }
    }

    public static List<Model> LoadOBJ(StreamReader reader)
    {
        List<Model> list = [];
        int lineCount = 0;

        List<Vector4> vertexPositions = [];
        List<Vector2> uv = [];

        Dictionary<int, int> objVertexMapper = [];// OBJ file vertex -> Model vertex

        try
        {
            if (!StreamReaderContainsMultipleObjects(reader))
            {
                FileStream? fileStream = reader.BaseStream as FileStream;
                if (fileStream != null) 
                    list.Add(new Model { ObjectName = fileStream.Name });
                else
                    list.Add(new Model { ObjectName = "Object" });
            }

            string? line = "";
            while ((line = reader.ReadLine()) != null)
            {
                lineCount++;
                //Handle comment.
                string[] commentInfo = line.Split('#');
                if (commentInfo.Length <= 0) commentInfo = [line];
                string data = commentInfo[0];
                if(string.IsNullOrEmpty(data)) continue;

                //Handle actual tokens
                string[] tokens = data.Split(' ');
                if(tokens.Length < 1) continue;

                switch(tokens[0])
                {
                    case VertexToken:
                        {
                            if (!(tokens.Length == 4 || tokens.Length == 5)) throw new InvalidDataException($"Vertex data not valid on line : {lineCount}\n{data}");

                            float x, y, z, w;
                            w = 1.0f;

                            if (!float.TryParse(tokens[1], out x)
                                || !float.TryParse(tokens[2], out y)
                                || !float.TryParse(tokens[3], out z)
                                || (tokens.Length == 5 && !float.TryParse(tokens[4], out w)))
                                throw new InvalidDataException($"Vertex data not valid on line : {lineCount}\n{data}");
                            vertexPositions.Add(new Vector4(x, y, z, w));
                            break;
                        }
                    case UvToken:
                        { 
                            if (tokens.Length != 3) throw new InvalidDataException($"UV data not valid on line : {lineCount}\n{data}");

                            float u, v;
                            if (!float.TryParse(tokens[1], out u)
                                || !float.TryParse(tokens[2], out v))
                                throw new InvalidDataException($"UV data not valid on line : {lineCount}\n{data}");

                            uv.Add(new Vector2(u,v));
                            break;
                        }
                    case ObjectToken:
                        {
                            //I Think only blender uses this, I find it incredibly helpful so I'm going to steal it.
                            if (tokens.Length != 2) throw new InvalidDataException($"Object name invalid on line : {lineCount}\n{data}");
                            list.Add(new Model { ObjectName = tokens[1] });
                            objVertexMapper.Clear();//Clear for new model.
                            break;
                        }
                    case FaceToken:
                        {
                            if (tokens.Length == 1) throw new InvalidDataException($"Face data not valid on line : {lineCount}\n{data}");

                            List<uint> faceData = [];

                            for (int i = 1; i < tokens.Length; i++)
                            {
                                string vertexToken = tokens[i];
                                string[] vertexPositionTokens = vertexToken.Split('/');
                                int? index = null;
                                int? uvIndex = null;

                                switch (vertexPositionTokens.Length)
                                {
                                    case 1://Only vertex location is given.
                                        {
                                            if (!int.TryParse(vertexToken, out int soleIndex))//only contains vertex location
                                                throw new InvalidDataException($"Face data not valid on line : {lineCount}\n{data}");
                                            index = soleIndex;
                                            break;
                                        }
                                    case 2://Only UV/Vertex are given.
                                        {
                                            if (!int.TryParse(vertexPositionTokens[0], out int soleIndex)
                                                || !int.TryParse(vertexPositionTokens[1], out int soleUVIndex))
                                                throw new InvalidDataException($"Face data not valid on line : {lineCount}\n{data}");
                                            uvIndex = soleUVIndex;
                                            index = soleIndex;
                                            break;
                                        }
                                    case 3://given n//n or n/n/n
                                        {
                                            if (!int.TryParse(vertexPositionTokens[0], out int soleIndex))
                                            {
                                                throw new InvalidDataException($"Face data not valid on line : {lineCount}\n{data}");
                                            }
                                            index = soleIndex;
                                            if(!string.IsNullOrEmpty(vertexPositionTokens[1]))
                                            {
                                                if(!int.TryParse(vertexPositionTokens[1], out int soleUVIndex)) throw new InvalidDataException($"Face data not valid on line : {lineCount}\n{data}");
                                                uvIndex = soleUVIndex;
                                            }
                                            break;
                                        }
                                }

                                if(index == null) throw new InvalidDataException($"Face data not valid on line : {lineCount}\n{data}");

                                index--;//Decrement for indexing (1's to 0's)

                                //Vertex does not appear in mapping list.
                                if (!objVertexMapper.ContainsKey(index.Value))
                                {
                                    if(index.Value >= vertexPositions.Count)
                                        throw new InvalidDataException($"Face data invalid, Index id not defined on line: {lineCount}\nID:{index.Value}");

                                    objVertexMapper.Add(index.Value, list[^1].Verticies.Count());//Creat OBJ -> Model mapping!

                                    if (uvIndex == null)
                                    {
                                        list[^1].AddVertex(new Vertex(vertexPositions[index.Value].Xyz, Vector2.Zero));
                                    }
                                    else
                                    {
                                        uvIndex--;//Decrement for indexing (1's to 0's)
                                        if (uvIndex.Value >= uv.Count) throw new InvalidDataException($"Face data invalid, TextCord id not defined on line: {lineCount}\nTextCordID:{uvIndex.Value}");
                                        list[^1].AddVertex(new Vertex(vertexPositions[index.Value].Xyz, uv[uvIndex.Value]));
                                    }
                                }
                                faceData.Add((uint)objVertexMapper[index.Value]);
                            }
                            list[^1].AddFace(faceData);
                            break;
                        }
                        ///Unused tokens.
                    case NormalToken:
                    case MaterialLibToken:
                    case MaterialToken:
                    case GroupToken:
                    case SmoothToken:
                        break;
                    default:
                        {
                            throw new InvalidDataException($"Invalid line : {lineCount}");
                        }
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine($"An error occurred while reading the file: {e.Message}");
        }
        return list;
    }
}
