/*
 * ランダムにAoEを生成するテスト用クラス
 */

using UnityEngine;
using UnityEngine.Tilemaps;

public class AoESpawnTester : MonoBehaviour {

    [Header("AoE Prefab")]
    [SerializeField] private GameObject aoePrefab;

    [Header("Grid Reference")]
    [SerializeField] private Tilemap targetTilemap;

    [Header("Random Spawn Area (Grid Coordinates)")]
    [SerializeField] private Vector3Int minCellBounds = new Vector3Int(-5, -3, 0);
    [SerializeField] private Vector3Int maxCellBounds = new Vector3Int(4, 0, 0);

    [Header("AoE Size")]
    [SerializeField] private float aoeSize = 1.0f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1f;

    private void Start() {
        InvokeRepeating("SpawnRandomAoE", 0f, spawnInterval);
    }

    private void SpawnRandomAoE() {
        // Random.Rangeは整数範囲でmax値を含まないため、+1を加算
        int randomX = Random.Range(minCellBounds.x, maxCellBounds.x + 1);
        int randomY = Random.Range(minCellBounds.y, maxCellBounds.y + 1);

        Vector3Int randomCell = new Vector3Int(randomX, randomY, minCellBounds.z);

        // Grid Cell座標→World座標
        Vector3 spawnWorldPosition = targetTilemap.GetCellCenterWorld(randomCell);

        // AoEの生成
        GameObject aoeInstance = Instantiate(aoePrefab, spawnWorldPosition, Quaternion.identity);

        // サイズの適用
        aoeInstance.transform.localScale = new Vector3(aoeSize, aoeSize, 1f);
    }
}