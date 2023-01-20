using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChunkManager : MonoBehaviour
{
    public Material blockMaterial;
    public BlockManager blockManager;
    public Dictionary<Vector2, Chunk> chunks;
    public static Dictionary<Vector2, Chunk> activeChunks;
    public int seed;
    int chunkWidth = 16;
    int chunkHeight = 256;

    //[...]

    public void GenerateChunk(int xPos, int zPos)
    {
        Chunk c = new Chunk(new Vector2Int(xPos, zPos), new BlockState[chunkWidth, chunkHeight, chunkWidth], blockManager, this);

        for (int x = 0; x < chunkWidth; x++)
            for (int y = 0; y < chunkHeight; y++)
                for (int z = 0; z < chunkWidth; z++)
                {
                    GenerateBlock(new Vector3Int(x, y, z) + new Vector3Int(c.position.x, 0, c.position.y), new Vector3Int(x, y, z), c);
                }

        chunks.Add(new Vector2(xPos, zPos), c);
    }

    public void GenerateBlock(Vector3Int pos, Vector3Int localPos, Chunk c)
    {
        byte block = 0;
        int maxHeight = 50 + Noise.Get2DNoiseValue(new Vector2(pos.x, pos.z), seed, 101f, 25f, 3);
        bool canGenerateWater = true;

        //[...] Der Rest dieser Funktion bestimmt die Block-ID zur jeweils entsprechenden Höhe (Wasser, Stein, Gras, ...)

        c.blocks[localPos.x, localPos.y, localPos.z] = new BlockState(block, MeshData.maxLightLevel);
    }

    public void DrawChunk(int xPos, int zPos, bool destroyOld)
    {
        Chunk c = chunks[new Vector2(xPos, zPos)];

        if (destroyOld)
            Destroy(GameObject.Find("Chunk " + c.position.x + ", " + c.position.y));

        if (!activeChunks.ContainsKey(new Vector2(xPos, zPos)))
            activeChunks.Add(new Vector2(xPos, zPos), c);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();
        List<Color> colours = new List<Color>();
        int vertIndex = 0;
        Mesh m = new Mesh();

        if (c.hasLiquidChunk)
        {
            Destroy(GameObject.Find("Liquid Chunk: " + c.position.x + ", " + c.position.y));
            c.hasLiquidChunk = false;
        }

        for (int x = 0; x < chunkWidth; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkWidth; z++)
                {
                    byte id = c.blocks[x, y, z].id;

                    if (id != 0 && blockManager.blockDatas[id].type != BlockType.Liquid)
                    {
                        for (int face = 0; face < MeshData.typeFaces[BlockType.Default]; face++)
                        {
                            BlockState nextBlock = new BlockState(0, MeshData.maxLightLevel);

                            //Der benachbarte Block wird herausgesucht.
                            Vector3Int nextBlockPos = new Vector3Int(x + (int)MeshData.defaultDirections[face].x, y + (int)MeshData.defaultDirections[face].y, z + (int)MeshData.defaultDirections[face].z);
                            if (IsBlockInChunk(nextBlockPos))
                                nextBlock = c.blocks[nextBlockPos.x, nextBlockPos.y, nextBlockPos.z];
                            else if (MeshData.defaultDirections[face].y == 0)
                            {
                                Chunk nC = GetChunkFromVector2(c.position + new Vector2(MeshData.defaultDirections[face].x, MeshData.defaultDirections[face].z) * 16, chunks);
                                if (nC != null)
                                    nextBlock = nC.blocks[x - 15 * (int)MeshData.defaultDirections[face].x, y, z - 15 * (int)MeshData.defaultDirections[face].z];
                            }


                            //Wenn diese Seite des Blockes sichtbar (anschließend an Luft, Flüssigkeit o.Ä.) ist, wird sie zum Chunk-Mesh hinzugefügt.
                            if (nextBlock.id == 0 || blockManager.blockDatas[nextBlock.id].type == BlockType.Liquid || blockManager.blockDatas[nextBlock.id].isTransparent)
                            {
                                int textureIndex = blockManager.blockDatas[c.blocks[x, y, z].id].textureList[face];
                                for (int i = 0; i < 6; i++)
                                {
                                    //Die Mesh-Daten (Vector3-Daten zu den einzelnen Blöcken) sind in einem extra Skript als Array gespeichert.
                                    int index = MeshData.defaultTriangles[face, i];
                                    vertices.Add(MeshData.defaultVertices[index] + new Vector3Int(x, y, z));
                                    triangles.Add(vertIndex);
                                    uv.Add(MeshData.defaultUvs[i] + new Vector2(0.0625f * textureIndex, 0));

                                    float lightLevel = nextBlock.lightLevel;
                                    colours.Add(new Color(0, 0, 0, lightLevel));

                                    vertIndex++;
                                }
                            }
                        }
                    }
                    else if (blockManager.blockDatas[id].type == BlockType.Liquid)
                    {
                        //[...] Dasselbe wie oben, nur dieses Mal für Wasser/Lava
                    }
                }
            }
        }

        m.vertices = vertices.ToArray();
        m.triangles = triangles.ToArray();
        m.uv = uv.ToArray();
        m.colors = colours.ToArray();
        m.RecalculateNormals();
        m.Optimize();

        GameObject chunkObject = new GameObject("Chunk " + c.position.x + ", " + c.position.y, typeof(MeshFilter), typeof(MeshRenderer));
        chunkObject.transform.position = new Vector3(c.position.x, 0, c.position.y);
        chunkObject.transform.SetParent(transform);
        chunkObject.tag = "World";
        chunkObject.layer = 6;
        chunkObject.GetComponent<MeshFilter>().mesh = m;
        chunkObject.GetComponent<MeshRenderer>().material = blockMaterial;
        chunkObject.AddComponent(typeof(MeshCollider));
    }
}