using System;
using System.Collections.Generic;
using System.Text;
using AetherArk.Content;
using AetherArk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AetherArk.Runtime
{
    public sealed class BlueprintOptions
    {
        public string roomNamePrefix = "Room_";
        public ShipSystemType? selectedSystem;
        public Action<ShipSystemType> onRoomClick;
        public List<CrewState> crew;
        public string selectedCrewId;
        public Action<string> onCrewClick;
        public bool reducedMotion;
        public bool highContrast;
        public bool showAllocatedPower = true;
    }

    /// <summary>
    /// Draws a top-down FTL-style deck plan: hull art, rooms coloured by condition,
    /// hazard overlays, power pips, integrity bars and crew tokens. Enemy deck plans use
    /// silhouette sprites when available and retain the procedural hull as a safe fallback.
    /// </summary>
    public static class ShipBlueprintView
    {
        private const string HullArtResourcePath = "Art/Ships/";
        private static readonly Dictionary<string, Sprite> HullSprites = new Dictionary<string, Sprite>();
        private static readonly Color HullPlate = new Color(0.075f, 0.095f, 0.125f, 0.985f);
        private static readonly Color HullEdge = new Color(0.72f, 0.55f, 0.24f, 0.9f);
        private static readonly Color RoomOperational = new Color(0.09f, 0.3f, 0.34f, 0.96f);
        private static readonly Color RoomUnpowered = new Color(0.2f, 0.22f, 0.25f, 0.96f);
        private static readonly Color RoomDamaged = new Color(0.42f, 0.24f, 0.09f, 0.96f);
        private static readonly Color RoomDisabled = new Color(0.36f, 0.09f, 0.09f, 0.96f);
        private static readonly Color Fire = new Color(1f, 0.45f, 0.1f, 1f);
        private static readonly Color Breach = new Color(0.35f, 0.9f, 0.95f, 1f);

        public static Color LineageColor(CrewLineage lineage)
        {
            switch (lineage)
            {
                case CrewLineage.Elf: return new Color(0.42f, 0.9f, 0.86f, 1f);
                case CrewLineage.Dwarf: return new Color(0.9f, 0.55f, 0.28f, 1f);
                case CrewLineage.Orc: return new Color(0.45f, 0.75f, 0.35f, 1f);
                case CrewLineage.Goblin: return new Color(0.8f, 0.85f, 0.3f, 1f);
                case CrewLineage.Avian: return new Color(0.62f, 0.55f, 0.95f, 1f);
                default: return new Color(0.92f, 0.78f, 0.55f, 1f);
            }
        }

        public static void Draw(UiFactory ui, LocalizationService l10n, RectTransform parent, ShipState ship, DeckPlan plan,
            Vector2 position, Vector2 size, BlueprintOptions options)
        {
            if (ship == null || plan == null) return;
            options = options ?? new BlueprintOptions();
            var container = ui.Rect("Blueprint_" + ship.id, parent, position, size);

            const float bowMargin = 40f;
            const float sternMargin = 20f;
            const float gap = 6f;
            var cell = Mathf.Floor(Mathf.Min((size.x - bowMargin - sternMargin - 12f) / plan.columns, (size.y - 36f) / plan.rows));
            var gridWidth = cell * plan.columns;
            var gridHeight = cell * plan.rows;
            var gridX = Mathf.Floor((size.x - gridWidth - bowMargin + sternMargin) / 2f);
            var gridY = Mathf.Floor((size.y - gridHeight) / 2f);

            DrawHull(ui, container, ship.deckPlanId, size, gridX, gridY, gridWidth, gridHeight, bowMargin, options.highContrast);

            for (var i = 0; i < plan.tiles.Count; i++)
            {
                var tile = plan.tiles[i];
                var system = ship.GetSystem(tile.system);
                if (system == null) continue;
                var room = ship.GetRoom(tile.system);
                var x = gridX + tile.column * cell + gap / 2f;
                var y = gridY + (plan.rows - tile.row - tile.height) * cell + gap / 2f;
                var w = tile.width * cell - gap;
                var h = tile.height * cell - gap;
                DrawRoom(ui, l10n, container, ship, system, room, new Vector2(x, y), new Vector2(w, h), options);
            }
        }

        private static void DrawHull(UiFactory ui, RectTransform container, string deckPlanId, Vector2 canvasSize, float gridX, float gridY,
            float gridWidth, float gridHeight, float bowMargin, bool highContrast)
        {
            var sprite = LoadHullSprite(deckPlanId);
            if (sprite != null)
            {
                var artRect = ui.Rect("HullArt_" + deckPlanId, container, Vector2.zero, canvasSize);
                var art = ui.Image(artRect, highContrast ? Color.white : new Color(1f, 1f, 1f, 0.9f));
                art.sprite = sprite;
                art.preserveAspect = true;
                art.raycastTarget = false;
                return;
            }

            const float plate = 14f;
            var edge = highContrast ? 3f : 2f;
            var plateColor = HullPlate;
            var edgeColor = highContrast ? new Color(0.95f, 0.75f, 0.35f, 1f) : HullEdge;

            // Stern fins.
            var finHeight = Mathf.Max(18f, gridHeight * 0.22f);
            ui.PanelRect("SternFinTop", container, new Vector2(gridX - plate - 12f, gridY + gridHeight - finHeight + plate - 6f), new Vector2(26f, finHeight), plateColor)
                .GetComponent<Image>().raycastTarget = false;
            ui.PanelRect("SternFinBottom", container, new Vector2(gridX - plate - 12f, gridY - plate + 6f), new Vector2(26f, finHeight), plateColor)
                .GetComponent<Image>().raycastTarget = false;

            // Bow: a rotated square whose left half is hidden behind the hull plate.
            var bowSize = Mathf.Min(gridHeight + plate * 2f, bowMargin * 2f) * 0.72f;
            var bowCenter = new Vector2(gridX + gridWidth + plate, gridY + gridHeight / 2f);
            ui.Rotated("Bow", container, bowCenter, new Vector2(bowSize, bowSize), 45f, plateColor);
            ui.Rotated("BowEdge", container, bowCenter + new Vector2(edge, 0f), new Vector2(bowSize, bowSize), 45f, edgeColor);
            ui.Rotated("BowInner", container, bowCenter, new Vector2(bowSize - edge * 2f, bowSize - edge * 2f), 45f, plateColor);

            var platePosition = new Vector2(gridX - plate, gridY - plate);
            var plateSize = new Vector2(gridWidth + plate * 2f, gridHeight + plate * 2f);
            ui.PanelRect("HullPlate", container, platePosition, plateSize, plateColor).GetComponent<Image>().raycastTarget = false;
            ui.Outline("HullEdge", container, platePosition, plateSize, edge, edgeColor);
        }

        public static Sprite LoadHullSprite(string deckPlanId)
        {
            if (string.IsNullOrEmpty(deckPlanId)) return null;
            if (HullSprites.TryGetValue(deckPlanId, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>(HullArtResourcePath + deckPlanId);
            HullSprites[deckPlanId] = sprite;
            return sprite;
        }

        private static void DrawRoom(UiFactory ui, LocalizationService l10n, RectTransform container, ShipState ship, ShipSystemState system,
            RoomState room, Vector2 position, Vector2 size, BlueprintOptions options)
        {
            var condition = BlueprintRules.Classify(system);
            var fill = condition == RoomCondition.Disabled ? RoomDisabled
                : condition == RoomCondition.Damaged ? RoomDamaged
                : condition == RoomCondition.Unpowered ? RoomUnpowered
                : RoomOperational;
            if (options.highContrast) fill = new Color(fill.r * 1.25f, fill.g * 1.25f, fill.b * 1.25f, 1f);

            var localSystem = system.type;
            var button = ui.Button(options.roomNamePrefix + system.type, container, l10n.T(system.displayKey),
                options.onRoomClick == null ? (Action)null : () => options.onRoomClick(localSystem),
                position, size, fill, UiFactory.TextPrimary, size.y >= 100f ? 15 : 14);
            var labelText = button.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.alignment = TextAnchor.UpperCenter;
                labelText.rectTransform.anchoredPosition = new Vector2(0f, -7f);
                labelText.resizeTextForBestFit = false;
            }
            var pips = PowerPips(system, options.showAllocatedPower);
            if (pips.Length > 0)
            {
                var lit = options.showAllocatedPower ? system.power : system.EffectivePower;
                ui.Text("Pips_" + system.type, container, pips, 11, lit > 0 ? UiFactory.Aether : UiFactory.TextMuted, TextAnchor.UpperCenter,
                    new Vector2(position.x, position.y + size.y - 44f), new Vector2(size.x, 16f), FontStyle.Bold);
            }

            // Oxygen and hazards.
            if (room != null)
            {
                if (room.oxygen < 30f)
                    Overlay(ui, container, "Oxygen_" + system.type, position, size, new Color(0f, 0f, 0.05f, Mathf.Clamp01(0.45f - room.oxygen / 100f)));
                if (room.fire > 1f)
                {
                    Overlay(ui, container, "Fire_" + system.type, position, size, new Color(Fire.r, Fire.g, Fire.b, Mathf.Clamp(0.15f + room.fire / 100f * 0.5f, 0.15f, 0.6f)));
                    ui.Text("FireLabel_" + system.type, container, $"▲ {room.fire:0}", 12, Fire, TextAnchor.LowerRight,
                        new Vector2(position.x, position.y + 12f), new Vector2(size.x - 6f, 18f), FontStyle.Bold);
                }
                if (room.breach > 1f)
                {
                    ui.Outline("Breach_" + system.type, container, position, size, options.highContrast ? 4f : 3f, Breach);
                    // Stack under the fire label when both hazards are present.
                    var breachY = room.fire > 1f ? position.y + 28f : position.y + 12f;
                    ui.Text("BreachLabel_" + system.type, container, $"◇ {room.breach:0}", 12, Breach, TextAnchor.LowerRight,
                        new Vector2(position.x, breachY), new Vector2(size.x - 6f, 18f), FontStyle.Bold);
                }
            }

            if (room != null && room.intruders > 0)
            {
                ui.Outline("Intruders_" + system.type, container, position, size, options.highContrast ? 4f : 3f, UiFactory.Danger);
                var intruderY = position.y + 12f + (room.fire > 1f ? 16f : 0f) + (room.breach > 1f ? 16f : 0f);
                ui.Text("IntruderLabel_" + system.type, container, l10n.T("ui.intruders", room.intruders.ToString()), 12, UiFactory.Danger, TextAnchor.LowerRight,
                    new Vector2(position.x, intruderY), new Vector2(size.x - 6f, 18f), FontStyle.Bold);
            }

            if (options.selectedSystem.HasValue && options.selectedSystem.Value == system.type)
                ui.Outline("Selected_" + system.type, container, position, size, options.highContrast ? 4f : 3f, UiFactory.Brass);

            ui.Bar("Integrity_" + system.type, container, system.Integrity, new Vector2(position.x + 5f, position.y + 4f), new Vector2(size.x - 10f, 5f),
                system.Integrity < 0.35f ? UiFactory.Danger : UiFactory.Success);

            if (options.crew != null) DrawCrewTokens(ui, container, system.type, position, size, options);
        }

        public static string PowerPips(ShipSystemState system, bool showAllocated)
        {
            if (system == null || system.maxPower <= 0) return string.Empty;
            var lit = showAllocated ? system.power : system.EffectivePower;
            var builder = new StringBuilder();
            for (var i = 0; i < system.maxPower; i++) builder.Append(i < lit ? '●' : '○');
            return builder.ToString();
        }

        private static void DrawCrewTokens(UiFactory ui, RectTransform container, ShipSystemType roomType, Vector2 position, Vector2 size, BlueprintOptions options)
        {
            var token = Mathf.Clamp(Mathf.Min(size.x, size.y) * 0.36f, 20f, 32f);
            var index = 0;
            for (var i = 0; i < options.crew.Count; i++)
            {
                var crew = options.crew[i];
                if (crew.currentRoom != roomType || crew.isDead || crew.onSortie) continue;
                var x = position.x + 6f + index * (token + 4f);
                if (x + token > position.x + size.x - 4f) break;
                var y = position.y + 12f;
                var selected = options.selectedCrewId == crew.id;
                var ringColor = selected ? Color.white : crew.isCaptain ? UiFactory.Brass : new Color(0f, 0f, 0f, 0.55f);
                ui.Circle("CrewRing_" + crew.id, container, new Vector2(x - 2f, y - 2f), new Vector2(token + 4f, token + 4f), ringColor).raycastTarget = false;
                var color = LineageColor(crew.lineage);
                if (crew.IsDowned) color = new Color(0.55f, 0.18f, 0.2f, 1f);
                var rect = ui.Rect("CrewToken_" + crew.id, container, new Vector2(x, y), new Vector2(token, token));
                var image = ui.Image(rect, color);
                image.sprite = UiFactory.CircleSprite;
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                var localId = crew.id;
                if (options.onCrewClick != null) button.onClick.AddListener(() => options.onCrewClick(localId));
                button.interactable = !crew.IsDowned;
                ui.Text("CrewInitial_" + crew.id, rect, crew.IsDowned ? "!" : BlueprintRules.CrewInitial(crew.displayName), Mathf.RoundToInt(token * 0.5f), UiFactory.Ink,
                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(token, token), FontStyle.Bold);
                index++;
            }
        }

        private static void Overlay(UiFactory ui, RectTransform container, string name, Vector2 position, Vector2 size, Color color)
        {
            ui.PanelRect(name, container, position, size, color).GetComponent<Image>().raycastTarget = false;
        }
    }
}
