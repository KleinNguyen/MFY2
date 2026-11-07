using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyBoardView : MonoBehaviour
{
    [SerializeField] private List<Transform> slots;
    public List<EnemyView> EnemyViews { get; private set; } = new();

    public void AddEnemies(List<EnemyData> enemyDatas)
    {
        if (enemyDatas == null || enemyDatas.Count == 0) return;

        if (enemyDatas.Count > slots.Count)
        {
            Debug.LogError(
                $"Encounter has {enemyDatas.Count} enemies, but the board only has {slots.Count} slots."
            );
            return;
        }

        int[] slotIndices = GetSlotIndices(enemyDatas);
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            Transform slot = slots[slotIndices[i]];
            EnemyView enemyView = EnemyViewCreator.Instance.CreateEnemyView(
                enemyDatas[i],
                slot.position,
                slot.rotation
            );
            enemyView.transform.parent = slot;
            EnemyViews.Add(enemyView);
        }
    }

    private int[] GetSlotIndices(List<EnemyData> enemyDatas)
    {
        int bossIndex = enemyDatas.FindIndex(enemy => enemy.Rank == EnemyRank.Boss);
        if (bossIndex < 0)
        {
            return Enumerable.Range(0, enemyDatas.Count)
                .Select(index => enemyDatas.Count == 1
                    ? slots.Count / 2
                    : index * (slots.Count - 1) / (enemyDatas.Count - 1))
                .ToArray();
        }

        int middleIndex = slots.Count / 2;
        int[] slotIndices = new int[enemyDatas.Count];
        slotIndices[bossIndex] = middleIndex;

        List<int> sideSlots = Enumerable.Range(0, slots.Count)
            .Where(index => index != middleIndex)
            .ToList();
        int sideSlotIndex = 0;
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            if (i == bossIndex) continue;

            slotIndices[i] = sideSlots[sideSlotIndex];
            sideSlotIndex++;
        }

        return slotIndices;
    }

    public IEnumerator RemoveEnemy(EnemyView enemyView)
    {
        EnemyViews.Remove(enemyView);
        Tween tween = enemyView.transform.DOScale(Vector3.zero, 0.25f);
        yield return tween.WaitForCompletion();
        Destroy(enemyView.gameObject);
    }
}
