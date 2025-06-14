using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCombiner : MonoBehaviour
{
    public int VertexLimit = 65000;
    public List<GameObject> Levels = new List<GameObject>();
    public string TileTag = "Rule Tile";
    public string TileName;
    public List<GameObject> FilteredTiles = new List<GameObject>();
    private GameObject[] TilesWithMeshes;
    private List<MeshFilter> sourceMeshFilters;

    [ContextMenu("Combine Meshes")]
    public void CombineMeshes()
    {
        TilesWithMeshes = GameObject.FindGameObjectsWithTag(TileTag);
        FilteredTiles = new List<GameObject>();

        for (int i = 0; i < TilesWithMeshes.Length; i++)
        {
            if (TilesWithMeshes[i].name == TileName && CompareLevels(TilesWithMeshes[i]))
                FilteredTiles.Add(TilesWithMeshes[i]);
        }

        sourceMeshFilters = new List<MeshFilter>();
        for (int i = 0; i < FilteredTiles.Count; i++)
        {
            MeshFilter meshfilter = FilteredTiles[i].GetComponentInChildren<MeshFilter>();
            if (meshfilter != null)
                sourceMeshFilters.Add(meshfilter);
        }

        CombineInstance[] combine = new CombineInstance[sourceMeshFilters.Count];

        int vertex_count = 0;
        int combine_offset = 0;
        for (int i = 0; i < sourceMeshFilters.Count; i++)
        {
            combine[i - combine_offset].mesh = sourceMeshFilters[i].sharedMesh;
            combine[i - combine_offset].transform = sourceMeshFilters[i].transform.localToWorldMatrix;
            vertex_count += combine[i - combine_offset].mesh.vertexCount;
            if (CheckVertexCount(vertex_count))
            {
                SplitCombinedMeshes(combine);
                combine_offset = i + 1;
                combine = new CombineInstance[sourceMeshFilters.Count - combine_offset];
                vertex_count = 0;
            }
        }
        SplitCombinedMeshes(combine);
        HideFilteredTiles();
        DestroyImmediate(GetComponent<MeshRenderer>());
        DestroyImmediate(this);
    }

    void HideFilteredTiles()
    {
        for (int i = 0; i < FilteredTiles.Count; i++)
        {
            FilteredTiles[i].SetActive(false);
        }
    }

    void SplitCombinedMeshes(CombineInstance[] combine)
    {
        GameObject new_combined_mesh = new GameObject();
        new_combined_mesh.name = "SubCombined - " + TileName + " Tiles";
        new_combined_mesh.transform.SetParent(transform);

        new_combined_mesh.AddComponent<MeshFilter>();
        new_combined_mesh.AddComponent<MeshRenderer>();
        new_combined_mesh.AddComponent<MeshCollider>();

        int new_length = 0;
        for (int i = 0; i < combine.Length; i++)
        {
            if (combine[i].mesh != null)
                new_length++;
        }

        CombineInstance[] new_combine = new CombineInstance[new_length];

        for (int i = 0; i < new_length; i++)
        {
            new_combine[i].mesh = combine[i].mesh;
            new_combine[i].transform = combine[i].transform;
        }

        CombineMesh(new_combine, new_combined_mesh);
        new_combined_mesh.isStatic = true;

        MeshRenderer meshRenderer = new_combined_mesh.GetComponent<MeshRenderer>();
        Material[] materials;
        materials = GetComponent<MeshRenderer>().sharedMaterials;
        meshRenderer.sharedMaterials = materials;
    }

    void CombineMesh(CombineInstance[] combine, GameObject combined_obj)
    {
        var mesh = new Mesh();
        mesh.CombineMeshes(combine, true);
        mesh.Optimize();
        combined_obj.GetComponent<MeshFilter>().mesh = mesh;
        combined_obj.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    bool CompareLevels(GameObject Tile)
    {
        for (int i = 0; i < Levels.Count; i++)
        {
            if (Tile.transform.parent.name == Levels[i].name)
                return true;
        }
        return false;
    }

    bool CheckVertexCount(int vertex_count)
    {
        if (vertex_count > VertexLimit)
        {
            Debug.Log("Too Many Vertices to combine full mesh, splitting into sub meshes: " + transform.parent.name + "/" + gameObject.name + "\n Vertex Count: " + vertex_count);
            return true;
        }
        return false;
    }
}
