using DefectiveTowers.Utils;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Unity.UI_New;
using Il2CppAssets.Scripts.Unity.UI_New.Main.MonkeySelect;
using Il2CppAssets.Scripts.Unity.UI_New.Upgrade;
using Il2CppAssets.Scripts.Utils;
using Il2CppNinjaKiwi.Common;
using MelonLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(DefectiveTowers.Mod), "Defective Towers", "1.0.0", "Baydock")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace DefectiveTowers {
    [HarmonyPatch]
    public sealed class Mod : MelonMod {
        public static MelonLogger.Instance Logger { get; private set; }

        private static System.Collections.Generic.Dictionary<string, UnityDisplayNode> Protos { get; } = new();

        public override void OnInitializeMelon() {
            Logger = LoggerInstance;
        }

        [HarmonyPatch(typeof(GameModelLoader), nameof(GameModelLoader.Load))]
        [HarmonyPostfix]
        public static void Load(ref GameModel __result) {
            AddTower(__result, MiniTackShooter.Details, MiniTackShooter.After, MiniTackShooter.GetTower(__result));
            MiniTackShooter.AddLocalization(LocalizationManager.Instance.defaultTable);

            AddTower(__result, Monkey.Details, Monkey.After, Monkey.GetTower(__result));
            Monkey.AddLocalization(LocalizationManager.Instance.defaultTable);

            AddTower(__result, Bomb.Details, Bomb.After, Bomb.GetTower(__result));
            Bomb.AddLocalization(LocalizationManager.Instance.defaultTable);
        }

        private static void AddTower(GameModel gameModel, ShopTowerDetailsModel details, string after, TowerModel tower) {
            int index = 0;
            bool gotIndex = false;
            for (int i = 0; i < gameModel.towerSet.Length; i++) {
                if (gotIndex) {
                    gameModel.towerSet[i].towerIndex++;
                } else if (gameModel.towerSet[i].towerId.Equals(after)) {
                    gotIndex = true;
                    index = i + 1;
                }
            }
            if (!gotIndex) index = gameModel.towerSet.Length;

            details.towerIndex = index;
            gameModel.towerSet = gameModel.towerSet.Insert(index, details);
            gameModel.childDependants.Add(details);

            TowerType.towers = TowerType.towers.Insert(index, details.towerId);

            gameModel.towers = gameModel.towers.Add(tower);
            gameModel.childDependants.Add(tower);
        }

        [HarmonyPatch(typeof(ProfileModel), nameof(ProfileModel.Validate))]
        [HarmonyPostfix]
        public static void UnlockModdedTowers(ref ProfileModel __instance) {
            __instance.unlockedTowers.AddIfNotPresent(MiniTackShooter.Name);
            __instance.unlockedTowers.AddIfNotPresent(Monkey.Name);
            __instance.unlockedTowers.AddIfNotPresent(Bomb.Name);
        }

        [HarmonyPatch(typeof(Factory), nameof(Factory.FindAndSetupPrototypeAsync))]
        [HarmonyPrefix]
        public static bool LoadProtos(ref Factory __instance, string objectId, Il2CppSystem.Action<UnityDisplayNode> onComplete) {
            if (!Protos.ContainsKey(objectId) || Protos[objectId].isDestroyed) {
                Factory factory = __instance;
                void registerDisplay(UnityDisplayNode display) {
                    display.gameObject.SetActive(false);
                    display.transform.parent = factory.PrototypeRoot;
                    display.RecalculateGenericRenderers();
                    Protos.Add(objectId, display);
                    onComplete?.Invoke(display);
                }
                if (MiniTackShooter.IsThisProto(objectId))
                    MiniTackShooter.LoadProto(factory, registerDisplay);
                else if (Monkey.IsThisProto(objectId))
                    Monkey.LoadProto(factory, registerDisplay);
                else
                    return true;
                return false;
            } else if (Protos.ContainsKey(objectId)) {
                onComplete?.Invoke(Protos[objectId]);
                return false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Factory), nameof(Factory.ProtoFlush))]
        [HarmonyPostfix]
        public static void ClearProtos() {
            foreach (UnityDisplayNode proto in Protos.Values) {
                if (proto is not null)
                    Object.Destroy(proto.gameObject);
            }
            Protos.Clear();
        }

        [HarmonyPatch(typeof(ResourceLoader), nameof(ResourceLoader.LoadSpriteFromSpriteReferenceAsync), new System.Type[] { typeof(SpriteReference), typeof(Image), typeof(bool) })]
        [HarmonyPrefix]
        public static bool LoadSprites(SpriteReference reference, Image image) {
            if (reference is not null) {
                Sprite sprite = MiniTackShooter.LoadSprite(reference.GUID) ??
                                Monkey.LoadSprite(reference.GUID);
                if (sprite is not null) {
                    image.sprite = sprite;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(MonkeySelectMenu), nameof(MonkeySelectMenu.UpdateTowers))]
        [HarmonyPrefix]
        public static void SortMonkeySelectMenu(ref MonkeySelectMenu __instance) {
            System.Collections.Generic.List<ShopTowerDetailsModel> temp = new(__instance.shopTowerDetailsModels.ToArray());
            temp.Sort((a, b) => a.towerIndex - b.towerIndex);

            for (int i = 0; i < temp.Count; i++)
                __instance.shopTowerDetailsModels[i] = temp[i];
        }

        [HarmonyPatch(typeof(UpgradeScreen), nameof(UpgradeScreen.UpdateUi))]
        [HarmonyPrefix]
        public static bool NextTowerUpgrades(ref UpgradeScreen __instance, string towerId) {
            if (towerId.Equals(MiniTackShooter.Name) || towerId.Equals(Monkey.Name) || towerId.Equals(Bomb.Name)) {
                __instance.currentIndex = TowerType.towers.IndexOf(towerId);

                NK_TextMeshProUGUI towerName = __instance.towerTitle.Cast<NK_TextMeshProUGUI>();
                towerName.localizeKey = towerId;
                towerName.Start();
                towerName.gameObject.SetActive(true);
                NK_TextMeshProUGUI towerDescription = __instance.towerDescription.Cast<NK_TextMeshProUGUI>();
                towerDescription.localizeKey = towerId + " Description";
                towerDescription.Start();
                towerDescription.gameObject.SetActive(true);

                __instance.paragonTitlePanel.SetActive(false);
                __instance.towerTitleParagon.gameObject.SetActive(false);
                __instance.towerDescriptionParagon.gameObject.SetActive(false);

                CommonBackgroundScreen.instance.customBackgroundIn.color = __instance.backgroundColour;

                __instance.ResetTowerXpToSpend();
                __instance.purchaseTowerXP.gameObject.SetActive(false);

                ShopTowerDetailsModel details = Game.instance.model.towerSet[__instance.currentIndex].Cast<ShopTowerDetailsModel>();
                if (details.pathOneMax == 0 && details.pathTwoMax == 0 && details.pathThreeMax == 0) {
                    System.Collections.Generic.List<Transform> hidden = new();
                    for (int i = 0; i < __instance.transform.childCount; i++) {
                        Transform child = __instance.transform.GetChild(i);
                        string childName = child.name.ToLower();
                        if ((childName.Contains("arrows") || childName.Contains("upgrade")) && child.gameObject.activeSelf) {
                            child.gameObject.SetActive(false);
                            if (!childName.Contains("purchase"))
                                hidden.Add(child);
                        }
                    }

                    Button nextButton = __instance.nextArrow.GetComponentInChildren<Button>();
                    Button prevButton = __instance.prevArrow.GetComponentInChildren<Button>();

                    UnityAction unhide = null;
                    unhide = new System.Action(() => {
                        foreach (Transform child in hidden)
                            child.gameObject.SetActive(true);
                        nextButton.onClick.RemoveListener(unhide);
                        prevButton.onClick.RemoveListener(unhide);
                    });

                    nextButton.onClick.AddListener(unhide);
                    prevButton.onClick.AddListener(unhide);
                }
                return false;
            }
            return true;
        }
    }
}
