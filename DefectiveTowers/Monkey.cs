using DefectiveTowers.Utils;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Models.Towers.Weapons;
using Il2CppAssets.Scripts.Models.TowerSets;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Utils;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefectiveTowers {
    internal class Monkey {
        public const string Name = "Monkey";
        public const string After = "DartMonkey";

        private const string Display = Name + "_Display";
        private const string Icon = Name + "_Icon";
        private const string Portrait = Name + "_Portrait";
        private const string InstaIcon = Name + "_InstaIcon";

        public static ShopTowerDetailsModel Details => new(Name, -1, 0, 0, 0, -1, 0);

        public static TowerModel GetTower(GameModel gameModel) {
            TowerModel monkey = gameModel.GetTowerFromId("DartMonkey").CloneCast();

            monkey.name = Name;
            monkey.baseId = Name;
            monkey.display = new PrefabReference(Display);
            monkey.cost = 0;
            monkey.upgrades = new Il2CppReferenceArray<UpgradePathModel>(0);
            monkey.icon = new SpriteReference(Icon);
            monkey.portrait = new SpriteReference(Portrait);
            monkey.instaIcon = new SpriteReference(InstaIcon);
            monkey.dontDisplayUpgrades = true;

            Il2CppReferenceArray<Model> behaviors = new(monkey.behaviors.Length);
            for (int i = 0; i < behaviors.Length; i++) {
                behaviors[i] = monkey.behaviors[i].CloneCast();

                if (behaviors[i].TryCast(out DisplayModel display))
                    display.display = new PrefabReference(Display);
                else if (behaviors[i].TryCast(out AttackModel attack)) {
                    Il2CppReferenceArray<WeaponModel> weapons = new(1);
                    weapons[0] = attack.weapons[0].CloneCast();

                    ProjectileModel projectile = attack.weapons[0].projectile.CloneCast();
                    projectile.display = null;
                    System.Collections.Generic.List<Model> projectileBehaviors = new(projectile.behaviors.Length);
                    for (int j = 0; j < projectile.behaviors.Length; j++) {
                        if (!projectile.behaviors[j].IsIl2CppType<DamageModel>()) {
                            if (projectile.behaviors[j].IsIl2CppType<TravelStraitModel>())
                                projectileBehaviors.Add(new AgeModel("", 0, 0, false, null));
                            else
                                projectileBehaviors.Add(projectile.behaviors[j].Clone());

                            if (projectileBehaviors.Last().TryCast(out DisplayModel pDisplay))
                                pDisplay.display = null;
                        }
                    }
                    projectile.behaviors = projectileBehaviors.ToArray();
                    weapons[0].projectile = projectile;

                    attack.weapons = weapons;
                }
            }
            monkey.behaviors = behaviors;

            return monkey;
        }

        public static bool IsThisProto(string name) => name.Equals(Display);

        public static void LoadProto(Factory factory, System.Action<UnityDisplayNode> onComplete) => factory.FindAndSetupPrototypeAsync(new PrefabReference("cbac06a37a38a0746a4593de4a9b6296"), DisplayCategory.Tower, new System.Action<UnityDisplayNode>(udn => {
            UnityDisplayNode display = Object.Instantiate(udn.gameObject).GetComponent<UnityDisplayNode>();
            display.gameObject.name = "Monkey";
            Transform dart = display.transform.FindChildRecursive("DartMonkeyDart");
            Object.DestroyImmediate(dart.gameObject);
            onComplete.Invoke(display);
        }));

        public static Sprite LoadSprite(string name) => name switch {
            Icon or Portrait => Resources.MonkeyPortrait,
            InstaIcon => Resources.MonkeyInstaIcon,
            _ => null,
        };

        public static void AddLocalization(Dictionary<string, string> table) {
            table.AddIfNotPresent(Name, "Monkey");
            table.AddIfNotPresent(Name + " Description", "Monkey with no dart? What will he do?");
        }
    }
}
