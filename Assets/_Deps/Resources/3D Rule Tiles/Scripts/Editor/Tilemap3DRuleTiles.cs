using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class Tilemap3DRuleTiles : EditorWindow
{
    string tileTag = "Rule Tile";
    int combinedMeshVertexLimit = 65000;

    [MenuItem("MGL Tools/3D Tilemap Rule Tiles")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Tilemap3DRuleTiles));
    }

    private void OnGUI()
    {
        tileTag = EditorGUILayout.TextField("Tile Base Name", tileTag);
        combinedMeshVertexLimit = EditorGUILayout.IntField("Combined Mesh Vertex Limit", combinedMeshVertexLimit);

        if (GUILayout.Button("Generate Rule Tiles"))
        {
            GenerateRuleTiles();
            Debug.Log("Rule Tiles Generated!");
        }
        if (GUILayout.Button("Add New Level"))
        {
            AddNewLevel();
            Debug.Log("New Level Added!");
        }
        if (GUILayout.Button("Delete Obscured Tiles"))
        {
            RemoveObscuredTiles();

        }
        if (GUILayout.Button("Generate Combined Meshes"))
        {
            Debug.Log("Combined Mesh Generation Started!");
            GenerateCombinedMeshes();
            Debug.Log("Combined Mesh Generation Finished!");
        }
        if (GUILayout.Button("Delete Rule Tiles"))
        {
            BackupTiles();
            DeleteRuleTiles();
            Debug.Log("Rule Tiles Deleted!");
        }
        if (GUILayout.Button("Restore Deleted Rule Tiles"))
        {
            RestoreTilesFromBackup();
        }
    }

    private void AddNewLevel()
    {
        GameObject grid_obj = GameObject.FindGameObjectWithTag("Grid");
        RuleTileList3D ruleTileList = grid_obj.GetComponent<RuleTileList3D>();
        Grid grid = grid_obj.GetComponent<Grid>();

        ruleTileList.CleanTilemapList();

        GameObject new_level = new GameObject();
        new_level.AddComponent<Tilemap>();
        new_level.AddComponent<TilemapRenderer>();

        new_level.name = "Level (" + ruleTileList.tilemapLevels.Count + ")";
        new_level.transform.parent = grid_obj.transform;

        float new_level_height = ruleTileList.tilemapLevels.Count * grid.cellSize.y * grid.transform.localScale.y;
        new_level.transform.position = new Vector3(0, new_level_height, 0);
        new_level.transform.localScale = new Vector3(1, 1, 1);

        ruleTileList.tilemapLevels.Add(new_level);
    }



    private void GenerateRuleTiles()
    {
        ToggleRuleTiles(null, true);

        // Remove old combined meshes
        GameObject[] existing_combined_meshes = GameObject.FindGameObjectsWithTag("Combined Mesh");
        for (int i = 0; i < existing_combined_meshes.Length; i++)
        {
            DestroyImmediate(existing_combined_meshes[i]);
        }

        GameObject[] tiles = GameObject.FindGameObjectsWithTag(tileTag);
        GameObject[] old_tile_meshes = GameObject.FindGameObjectsWithTag("Tile Mesh");
        RuleTile3D[] rule_tiles = new RuleTile3D[tiles.Length];


        for (int i = 0; i < old_tile_meshes.Length; i++)
        {
            DestroyImmediate(old_tile_meshes[i]);
        }

        if (tiles.Length == 0) return;

        RuleTile3D rule_tile = tiles[0].GetComponent<RuleTile3D>();
        rule_tile.SetRuleTileList();

        if (rule_tile.ruleTileController == null) return;

        rule_tile.ruleTileController.InitializeRuleTileList();
        for (int i = 0; i < tiles.Length; i++)
        {
            rule_tile = tiles[i].GetComponent<RuleTile3D>();
            rule_tile.AddToRuleTileList();
            rule_tiles[i] = rule_tile;

            if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(tiles[i]) != null)
                tiles[i].name = PrefabUtility.GetCorrespondingObjectFromOriginalSource(tiles[i]).name;
        }
        for (int i = 0; i < tiles.Length; i++)
        {
            rule_tiles[i].GenerateRuleTileMesh();
        }
    }

    private void RemoveObscuredTiles()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag(tileTag);
        //RuleTile3D[] rule_tiles = new RuleTile3D[tiles.Length];
        List<RuleTile3D> rule_tiles = new List<RuleTile3D>();
        RuleTile3D rule_tile;

        for (int i = 0; i < tiles.Length; i++)
        {
            rule_tile = tiles[i].GetComponent<RuleTile3D>();
            rule_tile.DestroyHidden();
            if(rule_tile != null) rule_tiles.Add(rule_tile);
        }

        tiles = GameObject.FindGameObjectsWithTag(tileTag);

        if (tiles.Length == 0)
        {
            Debug.Log("No Active Rules Tiles!");
            return;
        }

        rule_tile = tiles[0].GetComponent<RuleTile3D>();
        rule_tile.SetRuleTileList();
        rule_tile.ruleTileController.InitializeRuleTileList();

        for (int i = 0; i < rule_tiles.Count; i++)
        {
            rule_tiles[i].AddToRuleTileList();
        }

        Debug.Log("Obscured Tiles Removed!");
    }

    private void GenerateCombinedMeshes()
    {
        GameObject grid_obj = GameObject.FindGameObjectWithTag("Grid");
        RuleTileList3D ruleTileList = grid_obj.GetComponent<RuleTileList3D>();
        ruleTileList.CleanTilemapList();
        ToggleRuleTiles(null, true);

        // Remove old combined meshes
        GameObject[] existing_combined_meshes = GameObject.FindGameObjectsWithTag("Combined Mesh");
        for (int i = 0; i < existing_combined_meshes.Length; i++)
        {
            DestroyImmediate(existing_combined_meshes[i]);
        }

        // Create parent objects for the combined meshes and call a method
        // to continue the combining process based on individual tile type
        for (int i = 0; i < ruleTileList.tilemapLevels.Count; i++)
        {
            GameObject new_combined_level_parent = new GameObject();
            new_combined_level_parent.tag = "Combined Mesh";
            new_combined_level_parent.transform.SetParent(null);
            new_combined_level_parent.name = "Combined Mesh - " + ruleTileList.tilemapLevels[i].name;
            new_combined_level_parent.isStatic = true;
            GenerateCombinedMeshByTileType(ruleTileList.tilemapLevels[i], new_combined_level_parent);
        }
    }

    private void GenerateCombinedMeshByTileType(GameObject level_parent, GameObject combined_parent)
    {
        // Create a distinct tile type list
        level_parent.SetActive(true);
        RuleTile3D[] child_tiles = level_parent.GetComponentsInChildren<RuleTile3D>();
        Dictionary<string, GameObject> distinct_child_tile_types = new Dictionary<string, GameObject>();

        for (int i = 0; i < child_tiles.Length; i++)
        {
            if (!distinct_child_tile_types.ContainsKey(child_tiles[i].name))
                distinct_child_tile_types.Add(child_tiles[i].name, child_tiles[i].gameObject);
        }

        // Create child objects for separate combined meshes based on tile types
        for (int i = 0; i < distinct_child_tile_types.Count; i++)
        {
            string tile_type = distinct_child_tile_types.ElementAt(i).Key;
            GameObject new_combined_mesh = new GameObject();
            new_combined_mesh.name = "Combined - " + tile_type + " Tiles";
            new_combined_mesh.isStatic = true;
            new_combined_mesh.transform.SetParent(combined_parent.transform);

            new_combined_mesh.AddComponent<MeshCombiner>();
            new_combined_mesh.AddComponent<MeshRenderer>();

            MeshCombiner meshCombiner = new_combined_mesh.GetComponent<MeshCombiner>();
            MeshRenderer meshRenderer = new_combined_mesh.GetComponent<MeshRenderer>();

            Material[] materials;

            // First Check for Active Mesh Renderer
            MeshRenderer tileRenderer = distinct_child_tile_types[tile_type].GetComponentInChildren<MeshRenderer>();

            // If there is no Active Mesh Renderer, then try to use mesh renderer/material from placeholder model
            if (tileRenderer == null)
            {
                RuleTile3D ruleTile3D = distinct_child_tile_types[tile_type].GetComponent<RuleTile3D>();
                // Skip Combining this tile type if no mesh renderer is found
                if (ruleTile3D.placeholder == null)
                {
                    Debug.LogWarning("The tile mesh for: " + tile_type + " tiles is not set properly, so Combined Mesh cannot be produced. Try clicking 'Delete Obscured Tiles' then 'Generate Combined Meshes'.");
                    continue;
                }
                tileRenderer = ruleTile3D.placeholder.GetComponentInChildren<MeshRenderer>();
            }

            materials = tileRenderer.sharedMaterials;

            meshRenderer.sharedMaterials = materials;
            meshCombiner.Levels.Add(level_parent);
            meshCombiner.TileName = tile_type;
            meshCombiner.VertexLimit = combinedMeshVertexLimit;
            meshCombiner.CombineMeshes();
        }

        ToggleRuleTiles(child_tiles, false);
    }

    private void ToggleRuleTiles(RuleTile3D[] rule_tiles, bool active_value)
    {
        if (active_value == true)
        {
            GameObject grid_obj = GameObject.FindGameObjectWithTag("Grid");
            Transform[] transforms = grid_obj.transform.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].gameObject.CompareTag(tileTag)) transforms[i].gameObject.SetActive(true);
            }
            return;
        }
    }

    private void BackupTiles()
    {
        // Create Text File to Save old tiles
        string backupPath = "./Assets/Resources/3D Rule Tiles/Tilemap Backups/";
        string openSceneName = SceneManager.GetActiveScene().name;
        string fileName = "Backup Tiles " + openSceneName + ".txt";
        ToggleRuleTiles(null, true);
        GameObject[] tiles = GameObject.FindGameObjectsWithTag(tileTag);

        if (tiles.Length == 0) return;

        if (File.Exists(backupPath + fileName))
        {
            File.Delete(Path.Combine(backupPath, fileName));
        }

        using (StreamWriter outputFile = new StreamWriter(Path.Combine(backupPath, fileName), true))
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                outputFile.WriteLine(tiles[i].name + "," + tiles[i].transform.position);
            }
        }
    }

    private void RestoreTilesFromBackup()
    {
        string backupPath = "./Assets/Resources/3D Rule Tiles/Tilemap Backups/";
        string openSceneName = SceneManager.GetActiveScene().name;
        string fileName = "Backup Tiles " + openSceneName + ".txt";
        string prefabPath = "3D Rule Tiles/Rule Tiles/";

        if (!File.Exists(backupPath + fileName))
        {
            Debug.Log("No Backup File Found!");
            return;
        }

        DeleteRuleTiles();

        GameObject grid_obj = GameObject.FindGameObjectWithTag("Grid");
        RuleTileList3D ruleTileList = grid_obj.GetComponent<RuleTileList3D>();
        ruleTileList.CleanTilemapList();
        List<GameObject> Levels = ruleTileList.tilemapLevels;

        using (StreamReader inputFile = new StreamReader(Path.Combine(backupPath, fileName), true))
        {
            string line = inputFile.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                string vector_string = line.Substring(line.IndexOf(",") + 1);
                Vector3 position = GetVector3(vector_string);

                string prefab_string = line.Substring(0, line.IndexOf(","));
                GameObject prefab = Resources.Load<GameObject>(prefabPath + prefab_string);
                if (prefab == null)
                {
                    Debug.Log("Prefab for: " + prefabPath + prefab_string + "\nDoes not exist.");
                    line = inputFile.ReadLine();
                    continue;
                }
                GameObject tile = Instantiate(prefab);
                tile.transform.position = position;
                tile.name = prefab.name;

                GameObject parent_level = null;
                for (int i = 0; i < Levels.Count; i++)
                {
                    if (Levels[i].transform.position.y == position.y)
                    {
                        parent_level = Levels[i];
                        break;
                    }
                }

                tile.transform.SetParent(parent_level.transform);
                tile.transform.localScale = prefab.transform.localScale;

                line = inputFile.ReadLine();
            }
        }
        Debug.Log("Rule Tiles Restored!");
        GenerateRuleTiles();
    }

    public Vector3 GetVector3(string vector_string)
    {
        string[] temp = vector_string.Substring(1, vector_string.Length - 2).Split(',');
        float x = float.Parse(temp[0]);
        float y = float.Parse(temp[1]);
        float z = float.Parse(temp[2]);
        Vector3 rValue = new Vector3(x, y, z);
        return rValue;
    }

    private void DeleteRuleTiles()
    {
        ToggleRuleTiles(null, true);
        GameObject[] tiles = GameObject.FindGameObjectsWithTag(tileTag);
        for (int i = 0; i < tiles.Length; i++)
        {
            DestroyImmediate(tiles[i]);
        }
    }
}
