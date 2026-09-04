namespace AetherArk.Content
{
    /// <summary>Weapon strings. Generated from tools/gen_weapons.py.</summary>
    public sealed partial class LocalizationService
    {
        private void AddWeaponStrings()
        {
            Add("enum.weaponfamily.cannon", "함포", "Cannon");
            Add("enum.weaponfamily.lance", "랜스", "Lance");
            Add("enum.weaponfamily.piercer", "관통", "Piercer");
            Add("enum.weaponfamily.missile", "미사일", "Missile");
            Add("enum.weaponfamily.flak", "플랙", "Flak");
            Add("enum.weaponfamily.incendiary", "소이", "Incendiary");
            Add("enum.weaponfamily.breacher", "파공", "Breacher");
            Add("weapon.aether_cannon", "에테르 포", "Aether Cannon");
            Add("weapon.aether_cannon.desc", "균형 잡힌 기본 함포.", "The balanced standard gun.");
            Add("weapon.heavy_cannon", "중포", "Heavy Cannon");
            Add("weapon.heavy_cannon.desc", "느리지만 강한 함포.", "Slow, hard-hitting gun.");
            Add("weapon.siege_cannon", "공성포", "Siege Cannon");
            Add("weapon.siege_cannon.desc", "구획을 부수는 대구경포. 시스템 피해 1.5배.", "A great gun that wrecks compartments; 1.5x system damage.");
            Add("weapon.ward_lance", "결계 랜스", "Ward Lance");
            Add("weapon.ward_lance.desc", "결계에 2배 피해, 장갑은 뚫지 못함.", "Double damage to wards; cannot pierce armor.");
            Add("weapon.resonance_lance", "공명 랜스", "Resonance Lance");
            Add("weapon.resonance_lance.desc", "결계에 2배 피해, 명중 +6%.", "Double damage to wards, +6% accuracy.");
            Add("weapon.sky_lance", "창공 랜스", "Sky Lance");
            Add("weapon.sky_lance.desc", "결계에 2.5배 피해.", "2.5x damage to wards.");
            Add("weapon.bolt_thrower", "볼트 투사기", "Bolt Thrower");
            Add("weapon.bolt_thrower.desc", "피해의 60%가 장갑을 관통. 결계에는 절반.", "60% of the hit bypasses armor; half damage to wards.");
            Add("weapon.rail_harpoon", "레일 작살", "Rail Harpoon");
            Add("weapon.rail_harpoon.desc", "피해의 70%가 장갑을 관통. 결계에는 절반.", "70% of the hit bypasses armor; half damage to wards.");
            Add("weapon.gate_piercer", "천공 관통포", "Gate Piercer");
            Add("weapon.gate_piercer.desc", "피해의 80%가 장갑을 관통, 파공 확률 30%.", "80% of the hit bypasses armor; 30% breach chance.");
            Add("weapon.rocket_pod", "로켓 포드", "Rocket Pod");
            Add("weapon.rocket_pod.desc", "결계 무시. 발당 군수품 1. 화재 확률 30%.", "Ignores wards. 1 ordnance per shot. 30% fire chance.");
            Add("weapon.storm_missiles", "폭풍 미사일", "Storm Missiles");
            Add("weapon.storm_missiles.desc", "결계 무시. 발당 군수품 1. 화재 확률 45%.", "Ignores wards. 1 ordnance per shot. 45% fire chance.");
            Add("weapon.ruin_missiles", "파멸 미사일", "Ruin Missiles");
            Add("weapon.ruin_missiles.desc", "결계 무시. 발당 군수품 2. 시스템 피해 1.5배.", "Ignores wards. 2 ordnance per shot. 1.5x system damage.");
            Add("weapon.flak_battery", "플랙 포대", "Flak Battery");
            Add("weapon.flak_battery.desc", "피해는 낮지만 발사마다 요격 충전 +1.", "Light damage, but every shot grants an intercept charge.");
            Add("weapon.flak_curtain", "플랙 장막", "Flak Curtain");
            Add("weapon.flak_curtain.desc", "발사마다 요격 충전 +1. 명중 +8%.", "Every shot grants an intercept charge; +8% accuracy.");
            Add("weapon.ember_mortar", "잿불 박격포", "Ember Mortar");
            Add("weapon.ember_mortar.desc", "화재 확률 80%, 시스템 피해 1.3배.", "80% fire chance, 1.3x system damage.");
            Add("weapon.hellfire_mortar", "지옥불 박격포", "Hellfire Mortar");
            Add("weapon.hellfire_mortar.desc", "화재 확률 100%, 시스템 피해 1.6배.", "Always starts a fire; 1.6x system damage.");
            Add("weapon.breacher_charges", "파공 폭약", "Breacher Charges");
            Add("weapon.breacher_charges.desc", "파공 확률 70%.", "70% breach chance.");
            Add("weapon.hull_ripper", "선체 파쇄기", "Hull Ripper");
            Add("weapon.hull_ripper.desc", "파공 확률 80%, 장갑 40% 관통.", "80% breach chance; 40% of the hit bypasses armor.");
        }
    }
}
