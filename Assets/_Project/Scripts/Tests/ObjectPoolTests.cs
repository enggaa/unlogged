using NUnit.Framework;
using UnityEngine;
using Patterns.ObjectPool;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Bug 6: ObjectPool Fetch() 중복 반환 테스트
    /// 증상: iterator가 끝에 도달하면 pool[0]이 두 번 연속 반환됨
    /// 수정: 모듈로 연산으로 래핑하여 중복 제거
    /// </summary>
    public class ObjectPoolTests
    {
        [Test]
        public void Bug6_풀_순환_시_중복_없이_모든_항목_반환()
        {
            // Given: 5개 항목의 ObjectPool
            var prefab = new GameObject("PoolPrefab");
            var pool = new ObjectPool<GameObject>(prefab, 5);

            // When: 풀 크기의 2배만큼 Fetch 호출
            var fetchedObjects = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                fetchedObjects.Add(pool.Fetch());
            }

            // Then: 각 항목이 정확히 2번씩 반환되어야 함 (중복 없음)
            var counts = new Dictionary<GameObject, int>();
            foreach (var obj in fetchedObjects)
            {
                if (!counts.ContainsKey(obj))
                    counts[obj] = 0;
                counts[obj]++;
            }

            Assert.AreEqual(5, counts.Count, "5개의 고유한 오브젝트가 반환되어야 합니다.");
            foreach (var kvp in counts)
            {
                Assert.AreEqual(2, kvp.Value, 
                    $"각 오브젝트는 정확히 2번씩 반환되어야 합니다. (실제: {kvp.Value}번)");
            }

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Bug6_첫_번째_항목이_연속으로_반환되지_않음()
        {
            // Given: 3개 항목의 ObjectPool
            var prefab = new GameObject("PoolPrefab");
            var pool = new ObjectPool<GameObject>(prefab, 3);

            // When: 풀 크기만큼 Fetch 호출 후 하나 더 호출
            var first = pool.Fetch();
            var second = pool.Fetch();
            var third = pool.Fetch();
            var fourth = pool.Fetch(); // 이제 다시 순환

            // Then: 4번째는 1번째와 같아야 하지만, 2번째나 3번째와는 달라야 함
            Assert.AreEqual(first, fourth, "4번째는 1번째(pool[0])와 같아야 합니다.");
            Assert.AreNotEqual(second, third, "2번째와 3번째는 달라야 합니다.");
            Assert.AreNotEqual(first, second, "1번째와 2번째가 같으면 버그입니다!");

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Bug6_순환_패턴이_정확하게_유지됨()
        {
            // Given: 4개 항목의 ObjectPool
            var prefab = new GameObject("PoolPrefab");
            var pool = new ObjectPool<GameObject>(prefab, 4);

            // When: 8번 Fetch하여 순환 패턴 확인
            var sequence = new List<GameObject>();
            for (int i = 0; i < 8; i++)
            {
                sequence.Add(pool.Fetch());
            }

            // Then: 첫 4개와 다음 4개가 동일한 순서여야 함
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(sequence[i], sequence[i + 4], 
                    $"인덱스 {i}와 {i + 4}는 같은 오브젝트여야 합니다 (순환 패턴).");
            }

            Object.DestroyImmediate(prefab);
        }

        [Test]
        public void Bug6_AudioSource_풀에서도_중복_없음()
        {
            // Given: AudioSource 풀 (실제 사용 케이스)
            var audioObj = new GameObject("AudioPrefab");
            var audioSource = audioObj.AddComponent<AudioSource>();
            var pool = new ObjectPool<AudioSource>(audioSource, 10);

            // When: 20번 Fetch
            var fetched = new List<AudioSource>();
            for (int i = 0; i < 20; i++)
            {
                fetched.Add(pool.Fetch());
            }

            // Then: 각 AudioSource가 정확히 2번씩만 사용됨
            var usage = new Dictionary<AudioSource, int>();
            foreach (var src in fetched)
            {
                if (!usage.ContainsKey(src))
                    usage[src] = 0;
                usage[src]++;
            }

            foreach (var kvp in usage)
            {
                Assert.AreEqual(2, kvp.Value, 
                    "AudioSource 풀에서도 각 항목이 정확히 2번씩 사용되어야 합니다.");
            }

            Object.DestroyImmediate(audioObj);
        }

        [Test]
        public void Bug6_FetchAll은_풀의_모든_항목을_반환()
        {
            // Given: 3개 항목의 ObjectPool
            var prefab = new GameObject("PoolPrefab");
            var pool = new ObjectPool<GameObject>(prefab, 3);

            // When: FetchAll 호출
            var allItems = pool.FetchAll();

            // Then: 3개 항목이 모두 반환되어야 함
            Assert.AreEqual(3, allItems.Length, "FetchAll은 풀의 모든 항목을 반환해야 합니다.");
            
            // 모든 항목이 고유해야 함
            var uniqueItems = new HashSet<GameObject>(allItems);
            Assert.AreEqual(3, uniqueItems.Count, "FetchAll의 모든 항목은 고유해야 합니다.");

            Object.DestroyImmediate(prefab);
        }
    }
}
