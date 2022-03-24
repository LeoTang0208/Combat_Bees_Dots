﻿using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class Utils
{
    public static bool IsTopOfStack(
        ResourceGridParams resGridParams,
        DynamicBuffer<StackHeightParams> stackHeights,
        int gridX, int gridY, int stackIndex, bool stacked)
    {
        if (stacked)
        {
            int index = resGridParams.gridCounts.y * gridX + gridY;
            if (stackIndex == stackHeights[index].Value - 1)
            {
                return true;
            }
        }
        return false;

    }

    public static void UpdateStackHeights(
        ResourceGridParams resGridParams,
        DynamicBuffer<StackHeightParams> stackHeights,
        int gridX, int gridY, bool stacked, int updateValue)
    {
        if (stacked)
        {
            int index = resGridParams.gridCounts.y * gridX + gridY;
            var element = stackHeights[index];
            element.Value += updateValue;
            stackHeights[index] = element;
        }
    }

    public static float3 GetRandomPosition(ResourceGridParams resGridParams, FieldAuthoring field)
    {
        float3 pos;
        pos.x = UnityEngine.Random.Range(-25f, 25f);
        pos.y = UnityEngine.Random.Range(0f, 20f);
        pos.z = UnityEngine.Random.Range(-10f, 10f);
        return pos;
    }

    public static float3 NearestSnappedPos(ResourceGridParams resGridParams, float3 pos)
    {
        GetGridIndex(resGridParams, pos, out int gridX, out int gridY);
        float3 snapPos;
        snapPos.x = resGridParams.minGridPos.x + gridX * resGridParams.gridSize.x;
        snapPos.y = pos.y;
        snapPos.z = resGridParams.minGridPos.y + gridY * resGridParams.gridSize.y;
        return snapPos;
    }

    public static void GetGridIndex(ResourceGridParams resGridParams, float3 pos, out int gridX, out int gridY)
    {
        gridX = Mathf.FloorToInt((pos.x - resGridParams.minGridPos.x + resGridParams.gridSize.x * .5f) / resGridParams.gridSize.x);
        gridY = Mathf.FloorToInt((pos.z - resGridParams.minGridPos.y + resGridParams.gridSize.y * .5f) / resGridParams.gridSize.y);
        gridX = Mathf.Clamp(gridX, 0, resGridParams.gridCounts.x - 1);
        gridY = Mathf.Clamp(gridY, 0, resGridParams.gridCounts.y - 1);
    }

    public static float3 GetStackPos(ResourceParams resParams, ResourceGridParams resGridParams, FieldAuthoring field,
                                        DynamicBuffer<StackHeightParams> stackHeights, int gridX, int gridY)
    {
        int index = resGridParams.gridCounts.y * gridX + gridY;
        var height = stackHeights[index];

        float3 pos;
        pos.x = resGridParams.minGridPos.x + gridX * resGridParams.gridSize.x;
        pos.y = -field.size.y * .5f + (height.Value + .5f) * resParams.resourceSize;
        pos.z = resGridParams.minGridPos.y + gridY * resGridParams.gridSize.y;
        return pos;
    }
}