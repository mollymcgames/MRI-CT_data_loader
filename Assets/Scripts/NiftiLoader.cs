using System;
using System.IO;
using UnityEngine;

public class NiftiLoader : MonoBehaviour
{
    public string niftiFilePath = "Assets/Resources/la_007.nii";
    private int[] shape;
    private float[][] niftiSlices;

    void Start()
    {
        LoadNiftiFile(niftiFilePath);
        CreateSliceTextures();
    }

    void LoadNiftiFile(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            // Read the header
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            int sizeof_hdr = reader.ReadInt32();
            if (sizeof_hdr != 348)
            {
                Debug.LogError("Invalid NIfTI-1 header size.");
                return;
            }

            reader.BaseStream.Seek(40, SeekOrigin.Begin); // Skip to the dim field
            short dim0 = reader.ReadInt16();
            shape = new int[dim0];
            for (int i = 0; i < dim0; i++)
            {
                shape[i] = reader.ReadInt16();
            }

            reader.BaseStream.Seek(70, SeekOrigin.Begin); // Skip to the datatype field
            short datatype = reader.ReadInt16();
            short bitpix = reader.ReadInt16();

            // Skip to the voxel data
            reader.BaseStream.Seek(352, SeekOrigin.Begin);

            // Read voxel data based on datatype
            int sliceCount = shape[2];
            int sliceSize = shape[0] * shape[1];
            niftiSlices = new float[sliceCount][];

            if (datatype == 16) // Check for float32
            {
                for (int z = 0; z < sliceCount; z++)
                {
                    niftiSlices[z] = new float[sliceSize];
                    for (int i = 0; i < sliceSize; i++)
                    {
                        niftiSlices[z][i] = reader.ReadSingle();
                    }
                }
            }
            else if (datatype == 4) // Check for int16
            {
                for (int z = 0; z < sliceCount; z++)
                {
                    niftiSlices[z] = new float[sliceSize];
                    for (int i = 0; i < sliceSize; i++)
                    {
                        niftiSlices[z][i] = reader.ReadInt16();
                    }
                }
            }
            else
            {
                Debug.LogError("Unsupported NIfTI data type.");
                return;
            }
        }
    }

    void CreateSliceTextures()
    {
        for (int z = 0; z < shape[2]; z++)
        {
            CreateTextureForSlice(niftiSlices[z], shape[0], shape[1], z);
        }
    }

    void CreateTextureForSlice(float[] sliceData, int width, int height, int sliceIndex)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RFloat, false);
        Color[] pixels = new Color[sliceData.Length];
        for (int i = 0; i < sliceData.Length; i++)
        {
            float value = sliceData[i];
            pixels[i] = new Color(value, value, value, 1.0f);
        }
        texture.SetPixels(pixels);
        texture.Apply();

        // Create a GameObject for this slice
        GameObject sliceObject = new GameObject("Slice_" + sliceIndex);
        sliceObject.transform.parent = this.transform;
        sliceObject.transform.localPosition = new Vector3(0, 0, sliceIndex * 0.1f); // Adjust the position as needed
        sliceObject.transform.localScale = new Vector3(1, 1, 1);

        Renderer renderer = sliceObject.AddComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Unlit/Texture"));
        renderer.material.mainTexture = texture;

        MeshFilter meshFilter = sliceObject.AddComponent<MeshFilter>();
        meshFilter.mesh = CreateQuadMesh();
    }

    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
