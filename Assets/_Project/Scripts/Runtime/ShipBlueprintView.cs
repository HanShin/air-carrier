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
        public bool exteriorOnly;
        public bool enlargeDeck;
        public RunState run;
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
        private static readonly Color RoomOperational = new Color(0.045f, 0.1f, 0.13f, 0.9f);
        private static readonly Color RoomUnpowered = new Color(0.11f, 0.12f, 0.14f, 0.94f);
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
            const float gap = 3f;
            var cell = Mathf.Floor(Mathf.Min((size.x - bowMargin - sternMargin - 12f) / plan.columns, (size.y - 36f) / plan.rows));
            var gridWidth = cell * plan.columns;
            var gridHeight = cell * plan.rows;
            var gridX = Mathf.Floor((size.x - gridWidth - bowMargin + sternMargin) / 2f);
            var gridY = Mathf.Floor((size.y - gridHeight) / 2f);
            var artId = string.IsNullOrEmpty(ship.deckPlanId) ? plan.shipId : ship.deckPlanId;
            var sprite = LoadHullSprite(artId);
            Rect? artBounds = null;
            if (sprite != null && ShipArtLayout.TryDeckArea(artId, out var bay))
            {
                var fitted = ShipArtLayout.FitSprite(size, new Vector2(sprite.rect.width, sprite.rect.height));
                gridX = fitted.x + bay.x * fitted.width;
                gridY = fitted.y + bay.y * fitted.height;
                gridWidth = bay.width * fitted.width;
                gridHeight = bay.height * fitted.height;
                if (options.enlargeDeck && !options.exteriorOnly)
                {
                    // Magnify both the hull and its cutaway together; clipping is local to the viewport.
                    var desired = Mathf.Max(plan.columns * 72f / gridWidth, plan.rows * 72f / gridHeight);
                    var maximum = Mathf.Min((size.x - 28f) / gridWidth, (size.y - 28f) / gridHeight);
                    var zoom = Mathf.Max(1f, Mathf.Min(desired, maximum));
                    gridWidth *= zoom;
                    gridHeight *= zoom;
                    gridX = (size.x - gridWidth) / 2f;
                    gridY = (size.y - gridHeight) / 2f;
                    artBounds = new Rect(gridX - bay.x * fitted.width * zoom, gridY - bay.y * fitted.height * zoom,
                        fitted.width * zoom, fitted.height * zoom);
                    container.gameObject.AddComponent<RectMask2D>();
                }
            }
            var cellWidth = gridWidth / plan.columns;
            var cellHeight = gridHeight / plan.rows;

            DrawHull(ui, container, artId, size, gridX, gridY, gridWidth, gridHeight, bowMargin, options.highContrast, artBounds);
            if (options.exteriorOnly) return;

            for (var i = 0; i < plan.tiles.Count; i++)
            {
                var tile = plan.tiles[i];
                var system = ship.GetSystem(tile.system);
                if (system == null) continue;
                var room = ship.GetRoom(tile.system);
                var x = gridX + tile.column * cellWidth + gap / 2f;
                var y = gridY + (plan.rows - tile.row - tile.height) * cellHeight + gap / 2f;
                var w = tile.width * cellWidth - gap;
                var h = tile.height * cellHeight - gap;
                DrawRoom(ui, l10n, container, ship, system, room, new Vector2(x, y), new Vector2(w, h), options);
            }
            if (options.crew != null)
                DrawCrew(ui, container, ship, plan, new Vector2(gridX, gridY), new Vector2(cellWidth, cellHeight), size, options);
        }

        private static void DrawHull(UiFactory ui, RectTransform container, string deckPlanId, Vector2 canvasSize, float gridX, float gridY,
            float gridWidth, float gridHeight, float bowMargin, bool highContrast, Rect? imageBounds)
        {
            var sprite = LoadHullSprite(deckPlanId);
            if (sprite != null)
            {
                var bounds = imageBounds ?? new Rect(0f, 0f, canvasSize.x, canvasSize.y);
                var artRect = ui.Rect("HullArt_" + deckPlanId, container, new Vector2(bounds.x, bounds.y), new Vector2(bounds.width, bounds.height));
                var art = ui.Image(artRect, Color.white);
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
            var compact = size.y < 70f || size.x < 65f;
            var button = ui.Button(options.roomNamePrefix + system.type, container, l10n.T(system.displayKey),
                options.onRoomClick == null ? (Action)null : () => options.onRoomClick(localSystem),
                position, size, fill, UiFactory.TextPrimary, compact ? 10 : 13);
            var accent = condition == RoomCondition.Disabled ? UiFactory.Danger
                : condition == RoomCondition.Damaged ? UiFactory.Brass
                : condition == RoomCondition.Unpowered ? UiFactory.TextMuted : UiFactory.Aether;
            var surfaceRect = ui.Rect("DeckSurface_" + system.type, button.transform, Vector2.zero, size);
            var surface = surfaceRect.gameObject.AddComponent<DeckSurfaceGraphic>();
            surface.systemType = system.type;
            surface.accent = new Color(accent.r, accent.g, accent.b, 0.55f);
            surface.raycastTarget = false;
            surfaceRect.SetAsFirstSibling();
            var labelText = button.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.alignment = TextAnchor.UpperCenter;
                var titleHeight = compact ? 24f : 30f;
                labelText.rectTransform.anchoredPosition = new Vector2(2f, size.y - titleHeight - 6f);
                labelText.rectTransform.sizeDelta = new Vector2(size.x - 4f, titleHeight);
                labelText.resizeTextMinSize = 9;
            }
            var pips = PowerPips(system, options.showAllocatedPower);
            if (pips.Length > 0)
            {
                var lit = options.showAllocatedPower ? system.power : system.EffectivePower;
                ui.Text("Pips_" + system.type, container, pips, compact ? 8 : 10, lit > 0 ? UiFactory.Aether : UiFactory.TextMuted, TextAnchor.UpperRight,
                    new Vector2(position.x + 2f, position.y + 8f), new Vector2(size.x - 6f, 12f), FontStyle.Bold);
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

        }

        public static string PowerPips(ShipSystemState system, bool showAllocated)
        {
            if (system == null || system.maxPower <= 0) return string.Empty;
            var lit = showAllocated ? system.power : system.EffectivePower;
            var builder = new StringBuilder();
            for (var i = 0; i < system.maxPower; i++) builder.Append(i < lit ? '●' : '○');
            return builder.ToString();
        }

        private static void DrawCrew(UiFactory ui, RectTransform container, ShipState ship, DeckPlan plan, Vector2 origin, Vector2 cells, Vector2 size, BlueprintOptions options)
        {
            var routes = ui.Rect("CrewRoutes", container, Vector2.zero, size).gameObject.AddComponent<CrewRouteGraphic>();
            routes.plan = plan; routes.crew = options.crew; routes.selectedCrewId = options.selectedCrewId;
            routes.run = options.run;
            routes.gridOrigin = origin; routes.cellSize = cells; routes.raycastTarget = false;
            var token = Mathf.Clamp(Mathf.Min(cells.x, cells.y) * 0.46f, 18f, 34f);
            for (var i = 0; i < options.crew.Count; i++)
            {
                var crew = options.crew[i];
                if (crew.isDead || crew.onSortie) continue;
                var rect = ui.Rect("CrewToken_" + crew.id, container, Vector2.zero, new Vector2(token, token));
                var figure = rect.gameObject.AddComponent<CrewFigureGraphic>();
                figure.crew = crew; figure.ship = ship; figure.run = options.run;
                figure.gridOrigin = origin; figure.cellSize = cells; figure.rows = plan.rows;
                figure.selected = options.selectedCrewId == crew.id; figure.reducedMotion = options.reducedMotion;
                figure.highContrast = options.highContrast; figure.RefreshPose();
                var button = rect.gameObject.AddComponent<Button>();
                button.targetGraphic = figure;
                var localId = crew.id;
                if (options.onCrewClick != null) button.onClick.AddListener(() => options.onCrewClick(localId));
                button.interactable = !crew.IsDowned;
            }
        }

        private static void Overlay(UiFactory ui, RectTransform container, string name, Vector2 position, Vector2 size, Color color)
        {
            ui.PanelRect(name, container, position, size, color).GetComponent<Image>().raycastTarget = false;
        }
    }
}
