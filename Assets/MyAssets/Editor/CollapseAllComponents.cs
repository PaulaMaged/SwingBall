using UnityEditor;

public static class CollapseAllComponents
{
    [MenuItem("Tools/Collapse All Components %&c")] // Ctrl+Alt+C (Windows) or Cmd+Alt+C (Mac)
    public static void Collapse()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var editor = Editor.CreateEditor(obj);
            if (editor == null) continue;

            var tracker = ActiveEditorTracker.sharedTracker;
            tracker.ForceRebuild();

            for (int i = 0; i < tracker.activeEditors.Length; i++)
            {
                tracker.SetVisible(i, 0); // 0 = collapsed, 1 = expanded
            }
        }

        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }
}
