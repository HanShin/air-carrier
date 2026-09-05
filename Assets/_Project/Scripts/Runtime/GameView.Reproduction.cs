using AetherArk.Content;
using AetherArk.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AetherArk.Runtime
{
    public sealed partial class GameView
    {
        private RectTransform reproductionPanel;

        public void FitReproductionPanel()
        {
            if (reproductionPanel == null || ui.Root.rect.width <= 40f || ui.Root.rect.height <= 40f) return;
            var fit = Mathf.Min(1f, Mathf.Min((ui.Root.rect.width - 40f) / 1420f, (ui.Root.rect.height - 40f) / 820f));
            reproductionPanel.localScale = new Vector3(fit, fit, 1f);
            reproductionPanel.anchoredPosition = new Vector2((ui.Root.rect.width - 1420f * fit) / 2f, (ui.Root.rect.height - 820f * fit) / 2f);
        }

        private void AddReproductionButton(Transform parent, Vector2 position, Vector2 size)
        {
            ui.Button("ReproductionTools", parent, L("재현 [F9]", "Lab [F9]"), controller.ToggleReproductionPanel,
                position, size, UiFactory.PanelSoft, UiFactory.Brass, 15);
        }

        public void ShowReproductionPanel()
        {
            ui.Clear();
            ui.Background(controller.Background, new Color(0.01f, 0.025f, 0.045f, 0.88f));
            var panel = ui.PanelRect("ReproductionPanel", ui.Root, new Vector2(250f, 130f), new Vector2(1420f, 820f), PanelColor);
            reproductionPanel = panel; FitReproductionPanel();
            ui.Text("ReproductionTitle", panel, L("전투 재현실", "COMBAT REPRODUCTION LAB"), 34, UiFactory.Brass, TextAnchor.MiddleLeft,
                new Vector2(44f, 722f), new Vector2(1332f, 54f), FontStyle.Bold);
            var notice = controller.IsReproduction
                ? L("테스트 저장소 사용 중 · 일반 원정과 해금에는 반영되지 않습니다.", "ISOLATED TEST SESSION · Normal saves and unlocks are untouched.")
                : L("일반 게임은 이 화면에서 멈춰 있습니다. 테스트를 시작해도 원래 게임은 보존됩니다.", "Your normal game is held here. Starting a test preserves it for your return.");
            ui.Text("ReproductionIsolation", panel, notice, 19, UiFactory.Aether, TextAnchor.MiddleLeft,
                new Vector2(44f, 654f), new Vector2(1332f, 56f));
            ui.Text("ReproductionSeedLabel", panel, L("런 시드 (32비트 정수)", "RUN SEED (signed 32-bit integer)"), 17, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(44f, 602f), new Vector2(450f, 32f));
            var seed = ui.Input("ReproductionSeed", panel, controller.ReproductionSeed, "17000", new Vector2(44f, 546f), new Vector2(380f, 52f));
            seed.characterLimit = 24;
            seed.onValueChanged.AddListener(value => controller.ReproductionSeed = value);
            var flagship = ContentCatalog.GetFlagship(controller.ReproductionFlagship);
            ui.Button("ReproductionFlagship", panel, L("기함: ", "Flagship: ") + (flagship == null ? "—" : l10n.T(flagship.nameKey)), controller.CycleReproductionFlagship,
                new Vector2(448f, 546f), new Vector2(440f, 52f), UiFactory.PanelSoft, UiFactory.TextPrimary, 18);
            ui.Button("ReproductionDifficulty", panel, L("난이도: ", "Difficulty: ") + l10n.EnumName(controller.ReproductionDifficulty), controller.CycleReproductionDifficulty,
                new Vector2(912f, 546f), new Vector2(464f, 52f), UiFactory.PanelSoft, UiFactory.TextPrimary, 18);
            ui.Text("ReproductionPreset", panel, L("인간 함장 · 공작선 · 6공역 · 튜토리얼 없음. 시드만 같아도 선택/장비/시간 간격이 다르면 결과는 달라집니다.",
                "Human captain · workshop ship · six regions · no tutorial. Choices, loadout and time steps still affect the outcome."),
                16, UiFactory.TextMuted, TextAnchor.MiddleLeft, new Vector2(44f, 492f), new Vector2(1332f, 46f));
            ui.Button("ReproductionStartCampaign", panel, L("이 시드로 항로 시작", "Start seeded campaign"), () => controller.StartSeededReproduction(false),
                new Vector2(44f, 430f), new Vector2(650f, 54f), UiFactory.Brass, UiFactory.Ink, 20);
            ui.Button("ReproductionStartBattle", panel, L("이 시드로 초기 전투", "Start seeded first-tier battle"), () => controller.StartSeededReproduction(true),
                new Vector2(718f, 430f), new Vector2(658f, 54f), UiFactory.Aether, UiFactory.Ink, 20);
            var state = controller.Simulation?.State;
            var context = state == null ? L("현재 원정 없음", "No active run") :
                $"Seed {state.seed} · {l10n.EnumName(state.difficulty)} · {state.playerShip.displayName} · {state.regionIndex}/{state.regionCount} · {state.combatElapsed:0.0}s";
            ui.Text("ReproductionContext", panel, context, 18, UiFactory.TextPrimary, TextAnchor.MiddleLeft,
                new Vector2(44f, 372f), new Vector2(1332f, 42f), FontStyle.Bold);
            var path = ui.Input("ReproductionSnapshotPath", panel, controller.ReproductionSnapshotPath, L("스냅샷 JSON 전체 경로", "Full path to a snapshot JSON file"),
                new Vector2(44f, 312f), new Vector2(1332f, 48f));
            path.characterLimit = 4096;
            path.onValueChanged.AddListener(value => controller.ReproductionSnapshotPath = value);
            var capture = ui.Button("ReproductionCapture", panel, L("현재 전투 스냅샷 저장", "Capture current battle"), controller.CaptureReproductionSnapshot,
                new Vector2(44f, 234f), new Vector2(418f, 56f), UiFactory.PanelSoft, UiFactory.TextPrimary, 20);
            capture.interactable = controller.CanCaptureSnapshot;
            ui.Button("ReproductionLoad", panel, L("스냅샷 불러오기", "Load snapshot"), controller.LoadReproductionSnapshot,
                new Vector2(490f, 234f), new Vector2(418f, 56f), UiFactory.PanelSoft, UiFactory.TextPrimary, 20);
            var step = ui.Button("ReproductionStep", panel, L("0.1초 진행 [F8]", "Step 0.1 s [F8]"), controller.StepReproduction,
                new Vector2(936f, 234f), new Vector2(440f, 56f), UiFactory.PanelSoft, UiFactory.Aether, 20);
            step.interactable = controller.CanStepReproduction;
            ui.Text("ReproductionMessage", panel, l10n.T(controller.ReproductionMessageKey) +
                (string.IsNullOrEmpty(controller.ReproductionDetails) ? string.Empty : "\n" + controller.ReproductionDetails),
                17, UiFactory.Brass, TextAnchor.MiddleLeft, new Vector2(44f, 132f), new Vector2(1332f, 86f));
            ui.Button("ReproductionClose", panel, L("닫기 [F9 / Esc]", "Close [F9 / Esc]"), controller.ToggleReproductionPanel,
                new Vector2(44f, 58f), new Vector2(418f, 56f), UiFactory.PanelSoft, UiFactory.TextPrimary, 20);
            var restore = ui.Button("ReproductionReturn", panel, L("원래 게임으로 돌아가기", "Return to normal game"), controller.ReturnFromReproduction,
                new Vector2(490f, 58f), new Vector2(418f, 56f), UiFactory.PanelSoft, UiFactory.TextPrimary, 20);
            restore.interactable = controller.IsReproduction;
            ui.Button("ReproductionLatest", panel, L("최근 스냅샷 경로 선택", "Select latest capture path"), controller.SelectLatestReproductionSnapshot,
                new Vector2(936f, 58f), new Vector2(440f, 56f), UiFactory.PanelSoft, UiFactory.TextMuted, 18);
            ui.Text("ReproductionFooter", panel, L("저장은 원본을 덮어쓰지 않습니다. F8은 일시정지된 테스트 전투에서만 작동합니다.",
                "Captures never overwrite earlier files. F8 works only in paused test battles."), 14, UiFactory.TextMuted, TextAnchor.MiddleLeft,
                new Vector2(44f, 12f), new Vector2(1332f, 34f));
            // Give keyboard navigation a starting point after replacing the prior screen's Selectables.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(capture.interactable ? capture.gameObject : seed.gameObject);
        }
    }
}
