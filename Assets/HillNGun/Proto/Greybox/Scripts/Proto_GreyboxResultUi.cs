using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox
{
    public sealed class Proto_GreyboxResultUi
    {
        public const string RootName = "Greybox_ResultOverlay";
        public const float ResultRestartHitX0 = 38f;
        public const float ResultRestartHitX1 = 62f;
        public const float ResultRestartHitY0 = 73f;
        public const float ResultRestartHitY1 = 79f;

        static readonly Color CardFill = new Color(16f / 255f, 18f / 255f, 26f / 255f, 0.92f);
        static readonly Color CardBorder = new Color(220f / 255f, 210f / 255f, 180f / 255f, 1f);
        static readonly Color TitleClear = new Color(245f / 255f, 220f / 255f, 140f / 255f, 1f);
        static readonly Color TitleDown = new Color(214f / 255f, 112f / 255f, 112f / 255f, 1f);
        static readonly Color StatLabel = new Color(196f / 255f, 200f / 255f, 210f / 255f, 1f);
        static readonly Color StatValue = new Color(230f / 255f, 232f / 255f, 238f / 255f, 1f);
        static readonly Color HintColor = new Color(196f / 255f, 200f / 255f, 210f / 255f, 0.95f);
        static readonly Color DividerColor = new Color(245f / 255f, 220f / 255f, 140f / 255f, 0.55f);

        GameObject _root;
        Text _title;
        Text _scoreVal;
        Text _waveVal;
        Text _survivalVal;
        Text _killsVal;
        Text _hint;
        bool _useKorean;

        public bool IsBuilt => _root != null;

        public void Clear()
        {
            if (_root != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_root);
                else
                    Object.DestroyImmediate(_root);
            }

            _root = null;
            _title = null;
            _scoreVal = null;
            _waveVal = null;
            _survivalVal = null;
            _killsVal = null;
            _hint = null;
        }

        public void Ensure(Transform canvasTransform)
        {
            if (_root != null) return;
            if (canvasTransform == null) return;

            var font = Proto_GreyboxHudBuilder.GetUiFont(out _useKorean);

            _root = new GameObject(RootName);
            _root.transform.SetParent(canvasTransform, false);
            var rootRt = _root.AddComponent<RectTransform>();
            StretchFull(rootRt);

            PlaceImage(_root.transform, "Greybox_ResultDim", 0f, 0f, 100f, 100f,
                new Color(0.04f, 0.05f, 0.07f, 0.55f));
            PlaceImage(_root.transform, "Greybox_ResultCardBorder", 27f, 15.5f, 73f, 80.5f, CardBorder);
            PlaceImage(_root.transform, "Greybox_ResultCard", 28f, 16.5f, 72f, 80f, CardFill);
            PlaceImage(_root.transform, "Greybox_ResultDivider", 32f, 28.8f, 68f, 29.6f, DividerColor);

            _title = PlaceText(_root.transform, "Greybox_ResultTitle", 28f, 20f, 72f, 29f, font, 34,
                TextAnchor.MiddleCenter, TitleClear, string.Empty);
            var titleOutline = _title.gameObject.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
            titleOutline.effectDistance = new Vector2(1f, -1f);

            string survivalLabel = _useKorean ? "생존 시간" : "SURVIVAL";
            string killsLabel = _useKorean ? "처치" : "KILLS";
            string hint = _useKorean ? "또는 R  /  Enter" : "OR  R  /  ENTER";
            string restartLabel = _useKorean ? "다시하기" : "RESTART";

            PlaceText(_root.transform, "Greybox_ResultScoreLabel", 34.5f, 34f, 50f, 38f, font, 20,
                TextAnchor.MiddleLeft, StatLabel, "SCORE");
            _scoreVal = PlaceText(_root.transform, "Greybox_ResultScoreValue", 50f, 34f, 65.5f, 38f, font, 24,
                TextAnchor.MiddleRight, StatValue, "0");

            PlaceText(_root.transform, "Greybox_ResultWaveLabel", 34.5f, 41f, 50f, 45f, font, 20,
                TextAnchor.MiddleLeft, StatLabel, "WAVE");
            _waveVal = PlaceText(_root.transform, "Greybox_ResultWaveValue", 50f, 41f, 65.5f, 45f, font, 24,
                TextAnchor.MiddleRight, StatValue, "1 / 3");

            PlaceText(_root.transform, "Greybox_ResultSurvivalLabel", 34.5f, 48f, 50f, 52f, font, 20,
                TextAnchor.MiddleLeft, StatLabel, survivalLabel);
            _survivalVal = PlaceText(_root.transform, "Greybox_ResultSurvivalValue", 50f, 48f, 65.5f, 52f, font, 24,
                TextAnchor.MiddleRight, StatValue, "0:00");

            PlaceText(_root.transform, "Greybox_ResultKillsLabel", 34.5f, 55f, 50f, 59f, font, 20,
                TextAnchor.MiddleLeft, StatLabel, killsLabel);
            _killsVal = PlaceText(_root.transform, "Greybox_ResultKillsValue", 50f, 55f, 65.5f, 59f, font, 24,
                TextAnchor.MiddleRight, StatValue, "0");

            PlaceImage(_root.transform, "Greybox_ResultRestartPlate", ResultRestartHitX0, ResultRestartHitY0,
                ResultRestartHitX1, ResultRestartHitY1, new Color(0.22f, 0.45f, 0.72f, 0.92f));
            PlaceText(_root.transform, "Greybox_ResultRestartLabel", ResultRestartHitX0, ResultRestartHitY0,
                ResultRestartHitX1, ResultRestartHitY1, font, 22, TextAnchor.MiddleCenter, Color.white, restartLabel);
            _hint = PlaceText(_root.transform, "Greybox_ResultHint", 28f, 81f, 72f, 84.5f, font, 16,
                TextAnchor.MiddleCenter, HintColor, hint);

            var texts = _root.GetComponentsInChildren<Text>(true);
            foreach (var t in texts)
            {
                t.raycastTarget = false;
                t.supportRichText = false;
                t.maskable = false;
            }

            _root.SetActive(false);
        }

        public void Refresh(Proto_GreyboxRunResult result, int score, int wave, int totalWaves, float simTime, int kills)
        {
            if (_root == null) return;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();

            _title.text = result == Proto_GreyboxRunResult.Clear ? "STAGE CLEAR" : "DOWN";
            _title.color = result == Proto_GreyboxRunResult.Clear ? TitleClear : TitleDown;
            _scoreVal.text = score.ToString();
            _waveVal.text = $"{wave} / {totalWaves}";
            _survivalVal.text = FormatSurvival(simTime);
            _killsVal.text = kills.ToString();
            if (_hint != null)
                _hint.enabled = true;

            foreach (var g in _root.GetComponentsInChildren<Graphic>(true))
                g.SetAllDirty();
            Canvas.ForceUpdateCanvases();
        }

        public static bool IsRestartHit(float xPct, float yPct) =>
            xPct >= ResultRestartHitX0 && xPct <= ResultRestartHitX1 && yPct >= ResultRestartHitY0
            && yPct <= ResultRestartHitY1;

        static string FormatSurvival(float simTime)
        {
            int sec = Mathf.Max(0, Mathf.FloorToInt(simTime));
            int m = sec / 60;
            int s = sec % 60;
            return $"{m}:{s:D2}";
        }

        static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void PlaceImage(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var img = go.AddComponent<Image>();
            img.sprite = Proto_GreyboxSpriteUtil.WhiteSprite;
            img.color = color;
            img.raycastTarget = false;
            img.maskable = false;
        }

        static Text PlaceText(Transform parent, string name, float x0, float y0, float x1, float y1, Font font,
            int size, TextAnchor align, Color color, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Proto_GreyboxHudBuilder.ApplyRect(rt, x0, y0, x1, y1);
            var label = go.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.alignment = align;
            label.color = color;
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}