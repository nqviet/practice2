using Game.Board;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public class LevelValidatorWindow : EditorWindow
    {
        private LevelDefinition m_TargetLevel;
        private ValidationReport m_LastReport;
        private Vector2 m_ScrollPosition;

        [MenuItem("Window/Block Breaker/Level Validator", false, 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelValidatorWindow>("Level Validator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Level Coverage & Access Validator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enforces Floor Rules:\n1. Coverage: Every brick must sit inside some colored block's 3x3 ring.\n2. Access: Every colored block must be reachable 4-way from landing lane or same-color chained.", MessageType.Info);

            EditorGUILayout.Space(6);
            m_TargetLevel = (LevelDefinition)EditorGUILayout.ObjectField("Target Level", m_TargetLevel, typeof(LevelDefinition), false);

            if (GUILayout.Button("Find Level_01 in Project", GUILayout.Height(24)))
            {
                string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    m_TargetLevel = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                }
            }

            if (m_TargetLevel != null)
            {
                EditorGUILayout.Space(8);
                if (GUILayout.Button("Validate Level", GUILayout.Height(30)))
                {
                    RunValidation(m_TargetLevel);
                }
            }

            if (GUILayout.Button("Validate All Levels in Project", GUILayout.Height(26)))
            {
                ValidateAllLevels();
            }

            if (m_LastReport != null)
            {
                EditorGUILayout.Space(12);
                DrawReportGUI(m_LastReport);
            }
        }

        private void RunValidation(LevelDefinition levelDef)
        {
            m_LastReport = LevelValidator.Validate(levelDef);
            if (m_LastReport.IsValid)
            {
                EditorUtility.SetDirty(levelDef);
                AssetDatabase.SaveAssets();
            }
        }

        private void ValidateAllLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            int validCount = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level != null)
                {
                    var report = LevelValidator.Validate(level);
                    if (report.IsValid) validCount++;
                    else Debug.LogError($"[LevelValidator] Level '{level.LevelId}' failed validation:\n{report.GenerateSummary()}");
                }
            }
            Debug.Log($"[LevelValidator] Validated {guids.Length} levels. {validCount} valid, {guids.Length - validCount} invalid.");
        }

        private void DrawReportGUI(ValidationReport report)
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            if (report.IsValid)
            {
                EditorGUILayout.HelpBox("SUCCESS: Level passes all coverage, access, and connectivity rules!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"FAILED: {report.Errors.Count} errors found.", MessageType.Error);
            }

            EditorGUILayout.LabelField("Summary & Final Mix", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Bricks: {report.BrickCount}");
            EditorGUILayout.LabelField($"Colored Blocks: {report.ColoredCount} (Red: {report.RedCount}, Blue: {report.BlueCount}, Yellow: {report.YellowCount})");
            EditorGUILayout.LabelField($"Steel (Obstacles): {report.SteelCount}");
            EditorGUILayout.LabelField($"Empty Cells: {report.EmptyCount}");
            EditorGUILayout.LabelField($"Total Cells: {report.BrickCount + report.ColoredCount + report.SteelCount + report.EmptyCount} / 130");
            EditorGUILayout.LabelField($"Colors Used: {string.Join(", ", report.ColorsUsed)}");

            if (report.Errors.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Errors", EditorStyles.boldLabel);
                for (int i = 0; i < report.Errors.Count; i++)
                {
                    EditorGUILayout.SelectableLabel(report.Errors[i], EditorStyles.textField, GUILayout.Height(20));
                }
            }

            if (report.UncoveredBricks.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField($"Uncovered Bricks ({report.UncoveredBricks.Count})", EditorStyles.boldLabel);
                for (int i = 0; i < report.UncoveredBricks.Count; i++)
                {
                    EditorGUILayout.LabelField($" - Cell: {report.UncoveredBricks[i]}");
                }
            }

            if (report.UnreachableColored.Count > 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField($"Unreachable Colored Blocks ({report.UnreachableColored.Count})", EditorStyles.boldLabel);
                for (int i = 0; i < report.UnreachableColored.Count; i++)
                {
                    EditorGUILayout.LabelField($" - Cell: {report.UnreachableColored[i]}");
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
