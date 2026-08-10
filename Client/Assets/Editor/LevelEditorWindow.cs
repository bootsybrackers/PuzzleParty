using System.Collections.Generic;
using System.Linq;
using PuzzleParty.Levels;
using PuzzleParty.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PuzzleParty.EditorTools
{
    /// <summary>
    /// Classic level editor for PuzzleParty. Edits the real level files under
    /// StreamingAssets/levels (ready to commit), reorders levels by swapping,
    /// creates new levels from an image, and launches a level straight into
    /// Play mode using the same game code.
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";
        private static readonly string[] GameModes = { "slide", "switch" };

        private List<LevelConf> _levels = new List<LevelConf>();
        private List<Map> _maps = new List<Map>();
        private readonly Dictionary<int, Texture2D> _thumbs = new Dictionary<int, Texture2D>();

        private bool _showMaps;

        private int _selectedIndex = -1;
        private LevelConf _edit;
        private HashSet<(int row, int col)> _locked = new HashSet<(int, int)>();
        private HashSet<int> _ice = new HashSet<int>();

        private Vector2 _listScroll;
        private Vector2 _bodyScroll;

        // Swap state
        private int _swapA;
        private int _swapB;

        // New-level state
        private bool _showNew;
        private string _newImagePath = "";
        private int _newId = 1;
        private string _newName = "";
        private int _newMoves = 50, _newHoles = 2, _newRows = 4, _newColumns = 3;
        private int _newModeIdx = 1;

        [MenuItem("Tools/PuzzleParty/Level Editor")]
        public static void Open()
        {
            var win = GetWindow<LevelEditorWindow>("Level Editor");
            win.minSize = new Vector2(720, 480);
            win.Reload();
        }

        // Always clear the test override when returning to edit mode, so normal
        // play sessions are never accidentally forced onto a level.
        [InitializeOnLoadMethod]
        private static void HookPlayModeCleanup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    PlayerPrefs.DeleteKey("EditorForceLevel");
            };
        }

        private void OnEnable() => Reload();

        private void Reload()
        {
            foreach (var t in _thumbs.Values)
                if (t != null) DestroyImmediate(t);
            _thumbs.Clear();

            _levels = LevelFileService.LoadAll();
            _maps = MapFileService.LoadMaps();
            if (_levels.Count > 0)
            {
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _levels.Count - 1);
                Select(_selectedIndex);
                _swapA = _levels[0].id;
                _swapB = _levels[Mathf.Min(1, _levels.Count - 1)].id;
            }
            else
            {
                _selectedIndex = -1;
                _edit = null;
            }
            _newId = LevelFileService.NextFreeId();
        }

        private void Select(int index)
        {
            _selectedIndex = index;
            _edit = LevelFileService.Load(_levels[index].id);
            _locked = new HashSet<(int, int)>();
            _ice = new HashSet<int>();
            if (_edit.locked_tiles != null)
                foreach (var t in _edit.locked_tiles) _locked.Add((t.row, t.column));
            if (_edit.ice_rows != null)
                foreach (var r in _edit.ice_rows) _ice.Add(r);
        }

        private Texture2D Thumb(int id)
        {
            if (_thumbs.TryGetValue(id, out var cached))
                return cached;
            Texture2D tex = null;
            string png = LevelFileService.PngPath(id);
            if (System.IO.File.Exists(png))
            {
                tex = new Texture2D(2, 2);
                tex.LoadImage(System.IO.File.ReadAllBytes(png));
            }
            _thumbs[id] = tex;
            return tex;
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawLevelList();
            DrawEditor();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Reload();
            GUILayout.FlexibleSpace();
            _showMaps = GUILayout.Toggle(_showMaps, "Maps", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showNew = GUILayout.Toggle(_showNew, "New Level", EditorStyles.toolbarButton, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            if (_showMaps) DrawMapsPanel();
            if (_showNew) DrawNewLevel();
            DrawSwap();
        }

        private void DrawLevelList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240));
            EditorGUILayout.LabelField($"Levels ({_levels.Count})", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

            var shown = new HashSet<int>();
            foreach (var map in _maps.OrderBy(m => m.startLevel))
            {
                EditorGUILayout.LabelField($"▸ {map.name}  ({map.startLevel}-{map.endLevel})", EditorStyles.boldLabel);
                for (int i = 0; i < _levels.Count; i++)
                {
                    if (_levels[i].id < map.startLevel || _levels[i].id > map.endLevel) continue;
                    DrawLevelRow(i);
                    shown.Add(_levels[i].id);
                }
            }

            var unassigned = Enumerable.Range(0, _levels.Count).Where(i => !shown.Contains(_levels[i].id)).ToList();
            if (unassigned.Count > 0)
            {
                EditorGUILayout.LabelField("▸ Unassigned", EditorStyles.boldLabel);
                foreach (int i in unassigned) DrawLevelRow(i);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawLevelRow(int i)
        {
            var lvl = _levels[i];
            bool selected = i == _selectedIndex;
            var style = selected ? EditorStyles.helpBox : EditorStyles.label;
            EditorGUILayout.BeginHorizontal(style);
            GUILayout.Space(10);
            Texture2D tex = Thumb(lvl.id);
            Rect r = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32), GUILayout.Height(32));
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit);
            if (GUILayout.Button($"{lvl.id}: {lvl.name}", EditorStyles.label, GUILayout.Height(32)))
                Select(i);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEditor()
        {
            EditorGUILayout.BeginVertical();
            _bodyScroll = EditorGUILayout.BeginScrollView(_bodyScroll);

            if (_edit == null)
            {
                EditorGUILayout.HelpBox("No level selected.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"Level {_edit.id}", EditorStyles.boldLabel);
            Map inMap = MapFileService.MapForLevel(_maps, _edit.id);
            EditorGUILayout.LabelField("Map", inMap != null ? inMap.name : "Unassigned");
            _edit.name = EditorGUILayout.TextField("Name", _edit.name);
            _edit.moves = EditorGUILayout.IntField("Moves", _edit.moves);
            _edit.holes = EditorGUILayout.IntField("Holes", _edit.holes);
            _edit.rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _edit.rows));
            _edit.columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _edit.columns));

            int modeIdx = Mathf.Max(0, System.Array.IndexOf(GameModes, _edit.game_mode));
            modeIdx = EditorGUILayout.Popup("Game Mode", modeIdx, GameModes);
            _edit.game_mode = GameModes[modeIdx];

            EditorGUILayout.Space();
            DrawGridEditor();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Height(28)))
                SaveEdit();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Test Level  ▶", GUILayout.Height(28)))
                TestLevel(_edit.id);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGridEditor()
        {
            EditorGUILayout.LabelField("Tiles (click a cell to lock/unlock)", EditorStyles.boldLabel);

            Texture2D tex = Thumb(_edit.id);
            const float maxW = 300f;
            float aspect = tex != null ? (float)tex.height / tex.width : (float)_edit.rows / _edit.columns;
            float w = maxW;
            float h = maxW * aspect;

            EditorGUILayout.BeginHorizontal();
            Rect rect = GUILayoutUtility.GetRect(w, h, GUILayout.Width(w), GUILayout.Height(h));
            if (tex != null) GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);
            else EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));

            float cw = rect.width / _edit.columns;
            float ch = rect.height / _edit.rows;

            // Locked-tile overlays + grid lines
            for (int r = 0; r < _edit.rows; r++)
            {
                for (int c = 0; c < _edit.columns; c++)
                {
                    var cell = new Rect(rect.x + c * cw, rect.y + r * ch, cw, ch);
                    if (_locked.Contains((r, c)))
                        EditorGUI.DrawRect(cell, new Color(0.9f, 0.2f, 0.2f, 0.45f));
                    if (_ice.Contains(r))
                        EditorGUI.DrawRect(cell, new Color(0.3f, 0.7f, 1f, 0.30f));
                    Handles.color = new Color(1, 1, 1, 0.25f);
                    Handles.DrawSolidRectangleWithOutline(cell, Color.clear, new Color(1, 1, 1, 0.15f));
                }
            }

            // Click handling for locking tiles
            Event e = Event.current;
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                int c = Mathf.Clamp((int)((e.mousePosition.x - rect.x) / cw), 0, _edit.columns - 1);
                int r = Mathf.Clamp((int)((e.mousePosition.y - rect.y) / ch), 0, _edit.rows - 1);
                if (_locked.Contains((r, c))) _locked.Remove((r, c));
                else _locked.Add((r, c));
                e.Use();
                Repaint();
            }

            // Ice-row toggles beside the image
            EditorGUILayout.BeginVertical(GUILayout.Width(120));
            EditorGUILayout.LabelField("Ice rows", EditorStyles.miniBoldLabel);
            for (int r = 0; r < _edit.rows; r++)
            {
                bool wasIce = _ice.Contains(r);
                bool isIce = EditorGUILayout.ToggleLeft($"Row {r}", wasIce);
                if (isIce && !wasIce) _ice.Add(r);
                else if (!isIce && wasIce) _ice.Remove(r);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Drop stale locked/ice entries that fall outside a resized grid
            _locked.RemoveWhere(t => t.row >= _edit.rows || t.col >= _edit.columns);
            _ice.RemoveWhere(r => r >= _edit.rows);
        }

        private void DrawSwap()
        {
            if (_levels.Count < 2) return;
            int[] ids = _levels.Select(l => l.id).ToArray();
            string[] labels = _levels.Select(l => $"{l.id}: {l.name}").ToArray();

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Swap:", GUILayout.Width(40));
            _swapA = ids[Mathf.Max(0, EditorGUILayout.Popup(IndexOf(ids, _swapA), labels))];
            EditorGUILayout.LabelField("↔", GUILayout.Width(20));
            _swapB = ids[Mathf.Max(0, EditorGUILayout.Popup(IndexOf(ids, _swapB), labels))];
            if (GUILayout.Button("Swap", GUILayout.Width(70)))
            {
                if (_swapA != _swapB &&
                    EditorUtility.DisplayDialog("Swap levels",
                        $"Swap the design of level {_swapA} and level {_swapB}?", "Swap", "Cancel"))
                {
                    LevelFileService.Swap(_swapA, _swapB);
                    Reload();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNewLevel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("New Level", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Image", _newImagePath, EditorStyles.textField);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Select level image", "", "png");
                if (!string.IsNullOrEmpty(path)) _newImagePath = path;
            }
            EditorGUILayout.EndHorizontal();

            _newId = EditorGUILayout.IntField("Target Id", _newId);
            _newName = EditorGUILayout.TextField("Name", _newName);
            _newMoves = EditorGUILayout.IntField("Moves", _newMoves);
            _newHoles = EditorGUILayout.IntField("Holes", _newHoles);
            _newRows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _newRows));
            _newColumns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _newColumns));
            _newModeIdx = EditorGUILayout.Popup("Game Mode", _newModeIdx, GameModes);

            if (LevelFileService.Exists(_newId))
                EditorGUILayout.HelpBox($"Level {_newId} already exists. Choose another id or swap after creating.", MessageType.Warning);
            int lastMap = LevelFileService.LastMapEndLevel();
            if (lastMap > 0 && _newId > lastMap)
                EditorGUILayout.HelpBox($"Level {_newId} is beyond the last map (ends at {lastMap}). It won't belong to a map until you extend config/maps.json.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_newImagePath) || LevelFileService.Exists(_newId)))
            {
                if (GUILayout.Button("Create Level", GUILayout.Height(26)))
                {
                    var conf = new LevelConf
                    {
                        id = _newId,
                        name = _newName,
                        moves = _newMoves,
                        holes = _newHoles,
                        rows = _newRows,
                        columns = _newColumns,
                        game_mode = GameModes[_newModeIdx],
                        locked_tiles = new LockedTile[0],
                        ice_rows = new int[0]
                    };
                    LevelFileService.CreateNew(_newId, _newImagePath, conf);
                    _newImagePath = "";
                    _newName = "";
                    _showNew = false;
                    Reload();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawMapsPanel()
        {
            int maxLevelId = _levels.Count > 0 ? _levels.Max(l => l.id) : 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Maps (levels chain contiguously; edit a map's end to grow it)", EditorStyles.boldLabel);

            int deleteIndex = -1;
            for (int i = 0; i < _maps.Count; i++)
            {
                var m = _maps[i];
                EditorGUILayout.BeginHorizontal();
                m.name = EditorGUILayout.TextField(m.name, GUILayout.Width(160));
                EditorGUILayout.LabelField($"start {m.startLevel}", GUILayout.Width(64));
                EditorGUILayout.LabelField("end", GUILayout.Width(26));
                m.endLevel = EditorGUILayout.IntField(m.endLevel, GUILayout.Width(44));
                m.theme = EditorGUILayout.TextField(m.theme, GUILayout.Width(90));
                if (GUILayout.Button("X", GUILayout.Width(22))) deleteIndex = i;
                EditorGUILayout.EndHorizontal();
            }
            if (deleteIndex >= 0) _maps.RemoveAt(deleteIndex);

            MapFileService.Normalize(_maps); // keep the chain gap-free after any edit

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Map", GUILayout.Width(90)))
            {
                int start = _maps.Count > 0 ? _maps.Max(x => x.endLevel) + 1 : 1;
                _maps.Add(new Map { id = MapFileService.NextMapId(_maps), name = "New Map", startLevel = start, endLevel = start, theme = "" });
                MapFileService.Normalize(_maps);
            }
            if (GUILayout.Button("Save Maps", GUILayout.Width(90)))
            {
                MapFileService.SaveMaps(_maps);
                Reload();
            }
            EditorGUILayout.EndHorizontal();

            if (maxLevelId > 0 && _maps.Count > 0)
            {
                int lastEnd = _maps.Max(x => x.endLevel);
                if (lastEnd > maxLevelId)
                    EditorGUILayout.HelpBox($"Maps reach level {lastEnd}, but the last level on disk is {maxLevelId}. Ranges past {maxLevelId} have no levels yet.", MessageType.Warning);
                else if (lastEnd < maxLevelId)
                    EditorGUILayout.HelpBox($"Levels {lastEnd + 1}-{maxLevelId} aren't in any map (grow the last map or add one).", MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        private void SaveEdit()
        {
            _edit.locked_tiles = _locked
                .OrderBy(t => t.row).ThenBy(t => t.col)
                .Select(t => new LockedTile { row = t.row, column = t.col })
                .ToArray();
            _edit.ice_rows = _ice.OrderBy(x => x).ToArray();
            LevelFileService.Save(_edit);

            int keep = _selectedIndex;
            Reload();
            if (keep >= 0 && keep < _levels.Count) Select(keep);
        }

        private void TestLevel(int id)
        {
            SaveEdit();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            PlayerPrefs.SetInt("EditorForceLevel", id);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(GameScenePath);
            EditorApplication.EnterPlaymode();
        }

        private static int IndexOf(int[] arr, int value)
        {
            int i = System.Array.IndexOf(arr, value);
            return i < 0 ? 0 : i;
        }

        private void OnDisable()
        {
            foreach (var t in _thumbs.Values)
                if (t != null) DestroyImmediate(t);
            _thumbs.Clear();
        }
    }
}
