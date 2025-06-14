using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RuleTile3D : MonoBehaviour
{
    public RuleTileScriptableObject ruleTile;
    public RuleTileList3D ruleTileController;
    public GameObject placeholder;
    public bool[] neighborTilePositions = new bool[8];
    private bool IsHidden = false;

    private void Awake()
    {
        Destroy(placeholder);
    }

    public void AddToRuleTileList()
    {
        SetRuleTileList();
        transform.position = new Vector3(transform.position.x, transform.parent.position.y, transform.position.z);
        ruleTileController.ruleTileDict.Add(transform.position, gameObject);
    }

    public void SetRuleTileList()
    {
        ruleTileController = GetComponentInParent<RuleTileList3D>();
    }

    public void GenerateRuleTileMesh()
    {
        IsHidden = false;
        CheckNeighbors();
        SetRuleTile();
        HideObscuredTile();
    }

    void CheckNeighbors()
    {
        Grid grid = GetComponentInParent<Grid>();
        float tileSpacing = grid.cellSize.x * grid.transform.localScale.x;
        // Check for 0 position
        neighborTilePositions[0] = CheckNeighborAtPosition(transform.position + new Vector3(-tileSpacing, 0, tileSpacing));

        // Check for 1 position
        neighborTilePositions[1] = CheckNeighborAtPosition(transform.position + new Vector3(0, 0, tileSpacing));

        // Check for 2 position
        neighborTilePositions[2] = CheckNeighborAtPosition(transform.position + new Vector3(tileSpacing, 0, tileSpacing));

        // Check for 3 position
        neighborTilePositions[3] = CheckNeighborAtPosition(transform.position + new Vector3(-tileSpacing, 0, 0));

        // Check for 4 position
        neighborTilePositions[4] = CheckNeighborAtPosition(transform.position + new Vector3(tileSpacing, 0, 0));

        // Check for 5 position
        neighborTilePositions[5] = CheckNeighborAtPosition(transform.position + new Vector3(-tileSpacing, 0, -tileSpacing));

        // Check for 6 position
        neighborTilePositions[6] = CheckNeighborAtPosition(transform.position + new Vector3(0, 0, -tileSpacing));

        // Check for 7 position
        neighborTilePositions[7] = CheckNeighborAtPosition(transform.position + new Vector3(tileSpacing, 0, -tileSpacing));
    }

    bool CheckNeighborAtPosition(Vector3 position)
    {
        return (ruleTileController.ruleTileDict.ContainsKey(position) && ruleTileController.ruleTileDict[position].GetComponent<RuleTile3D>().ruleTile == ruleTile);
    }

    void SetRuleTile()
    {
        /* Neighbor bool layout map
        * [0   1   2
        * 3        4
        * 5    6   7]*/

        for (int i = 0; i < ruleTile.rules.Length; i++)
        {
            if (CompareNeighborRules(ruleTile.rules, i))
            {
                placeholder.SetActive(false);
                GameObject shownTile = Instantiate(ruleTile.rules[i].tile, transform);
                shownTile.transform.rotation = Quaternion.Euler(0, ruleTile.rules[i].rotation, 0);
                if (ruleTile.material != null) shownTile.GetComponentInChildren<Renderer>().material = ruleTile.material;
                ApplyTileOffset(ruleTile.rules[i].position_offset);
                return;
            }
        }
        placeholder.SetActive(true);
        if (ruleTile.material != null) placeholder.GetComponentInChildren<Renderer>().material = ruleTile.material;
    }

    bool CompareNeighborRules(Rule[] tile_rules, int rule_index)
    {
        int[] rules = new int[8];
        rules[0] = tile_rules[rule_index].TL;
        rules[1] = tile_rules[rule_index].T;
        rules[2] = tile_rules[rule_index].TR;
        rules[3] = tile_rules[rule_index].L;
        rules[4] = tile_rules[rule_index].R;
        rules[5] = tile_rules[rule_index].BL;
        rules[6] = tile_rules[rule_index].B;
        rules[7] = tile_rules[rule_index].BR;

        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i] == -1 && neighborTilePositions[i])
            {
                return false;
            }
            if (rules[i] == 1 && !neighborTilePositions[i])
            {
                return false;
            }
        }
        return true;
    }

    void HideObscuredTile()
    {
        // Needs Neighbors
        for (int i = 0; i < neighborTilePositions.Length; i++)
        {
            if (!neighborTilePositions[i])
                return;
        }

        // Needs Tile Above of the same type
        int tileSpacing = 1;
        Vector3 above_position = transform.position + new Vector3(0, tileSpacing, 0);
        if (!(ruleTileController.ruleTileDict.ContainsKey(above_position) && ruleTileController.ruleTileDict[above_position].name == this.name))
        {
            return;
        }

        // Above Tile also needs neighbors
        RuleTile3D above_rule_tile = ruleTileController.ruleTileDict[above_position].GetComponent<RuleTile3D>();
        for (int j = 0; j < neighborTilePositions.Length; j++)
        {
            if (!above_rule_tile.neighborTilePositions[j])
                return;
        }

        // Hide the tile
        GameObject[] old_tile_meshes = GameObject.FindGameObjectsWithTag("Tile Mesh");

        for (int i = 0; i < old_tile_meshes.Length; i++)
        {
            if (old_tile_meshes[i].transform.parent == this.transform)
                DestroyImmediate(old_tile_meshes[i]);
        }
        IsHidden = true;
    }

    public void DestroyHidden()
    {
        if (!IsHidden) return;
        DestroyImmediate(gameObject);
    }

    void ApplyTileOffset(Vector3 position_offset)
    {
        transform.position += position_offset;
    }

}
