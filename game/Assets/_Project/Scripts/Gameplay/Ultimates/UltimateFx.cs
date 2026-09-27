using System;
using System.Collections;
using NinjaVillage.Gameplay.Animation;
using UnityEngine;
using VfxApi = NinjaVillage.Gameplay.Vfx.Vfx;

namespace NinjaVillage.Gameplay.Ultimates
{
    /// <summary>
    /// Visuals for the ultimates, using the pixel-art <see cref="Vfx.VfxArt"/> catalog: the Dragon Slash
    /// sweep and the Heavenly Storm's falling shuriken. Everything is cosmetic except the optional
    /// <c>onLand</c> callback, so damage can land exactly when the shuriken does.
    /// </summary>
    public static class UltimateFx
    {
        private const int SortingOrder = 160;

        /// <summary>A giant golden dragon flies across the whole view at <paramref name="center"/>'s height, slashing as it goes.</summary>
        public static void DragonSweep(MonoBehaviour runner, Vector3 center)
        {
            var art = VfxApi.ArtCatalog;
            VfxApi.Burst(center, VfxApi.GoldColor, 6f, 0.5f);
            if (art == null || art.Dragon.Length == 0 || runner == null) return;
            runner.StartCoroutine(DragonRoutine(art, center));
        }

        private static IEnumerator DragonRoutine(Vfx.VfxArt art, Vector3 center)
        {
            var cam = UnityEngine.Camera.main;
            float halfWidth = cam != null ? cam.orthographicSize * cam.aspect : 6f;
            float camX = cam != null ? cam.transform.position.x : center.x;

            var dragon = new GameObject("DragonSlash_Dragon");
            var renderer = dragon.AddComponent<SpriteRenderer>();
            renderer.sprite = art.Dragon[0];
            renderer.sortingOrder = SortingOrder;
            renderer.color = new Color(1f, 0.88f, 0.45f, 0.95f); // golden spirit dragon
            dragon.AddComponent<SpriteLoop>().SetFrames(art.Dragon, 12f);
            dragon.transform.localScale = Vector3.one * 5f;

            float startX = camX - halfWidth - 4f, endX = camX + halfWidth + 4f;
            const float duration = 0.9f;
            float nextSlash = 0f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                var position = new Vector3(Mathf.Lerp(startX, endX, k), center.y + Mathf.Sin(k * Mathf.PI * 2f) * 0.6f, 0f);
                dragon.transform.position = position;
                if (t >= nextSlash)
                {
                    nextSlash += 0.07f;
                    VfxApi.PlayFrames(art.BigSlash.Length > 0 ? art.BigSlash : art.Slash, position + (Vector3)UnityEngine.Random.insideUnitCircle * 1.2f,
                        2.2f, UnityEngine.Random.Range(-40f, 40f), new Color(1f, 0.9f, 0.55f), SortingOrder - 1);
                }
                yield return null;
            }
            UnityEngine.Object.Destroy(dragon);
        }

        /// <summary>A spinning shuriken drops from the sky onto <paramref name="target"/>; <paramref name="onLand"/> runs on impact.</summary>
        public static void FallingShuriken(MonoBehaviour runner, Vector2 target, Action onLand)
        {
            var art = VfxApi.ArtCatalog;
            if (art == null || art.Shuriken.Length == 0 || runner == null)
            {
                onLand?.Invoke();
                VfxApi.HitSpark(target, true);
                return;
            }
            runner.StartCoroutine(FallRoutine(art, target, onLand));
        }

        private static IEnumerator FallRoutine(Vfx.VfxArt art, Vector2 target, Action onLand)
        {
            var shuriken = new GameObject("HeavenlyStorm_Shuriken");
            var renderer = shuriken.AddComponent<SpriteRenderer>();
            renderer.sprite = art.Shuriken[0];
            renderer.sortingOrder = SortingOrder;
            shuriken.AddComponent<SpriteLoop>().SetFrames(art.Shuriken, 18f);
            shuriken.transform.localScale = Vector3.one * 0.9f;

            Vector2 start = target + new Vector2(-1.8f, 6f);
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                shuriken.transform.position = Vector2.Lerp(start, target, t / duration);
                yield return null;
            }
            UnityEngine.Object.Destroy(shuriken);
            onLand?.Invoke();
            VfxApi.HitSpark(target, true);
        }
    }
}
