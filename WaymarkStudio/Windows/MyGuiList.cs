using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio.Windows;

using Cursor = (Vector2 pos, float width);

public partial class MyGui
{
    internal const string Dragging = "Dragging";

    private class ListState
    {
        public ListState()
        {
            id = "";
            hoverIndex = -1;
            prevHoverIndex = -1;
            deleteIndex = -1;
            renameIndex = -1;
            renamingText = "";
            renameFocus = false;
            dragSourceIndex = -1;
            dragTargetIndex = -1;
        }
        internal string id = "";
        internal int hoverIndex = -1;
        internal int prevHoverIndex = -1;
        internal int deleteIndex = -1;
        internal int renameIndex = -1;
        internal string renamingText = "";
        internal bool renameFocus = false;
        internal int dragSourceIndex = -1;
        internal int dragTargetIndex = -1;
    }

    private static Dictionary<string, ListState> ListStates = new();

    private static ListState? CurrentListState = null;
    private static bool IsRenamed;
    private static bool IsMoved;
    private static Cursor Cursor;
    private static int RowIndex;
    private static string RowName;
    private static bool dragdroppable;
    public static bool BeginList(string id, bool dragdroppable = true)
    {
        var s = ListStates.GetValueOrDefault(id, new());
        s.id = id;
        s.deleteIndex = -1;
        s.prevHoverIndex = s.hoverIndex;
        s.hoverIndex = -1;
        RowIndex = -1;
        s.dragTargetIndex = -1;
        IsRenamed = false;
        IsMoved = false;
        MyGui.dragdroppable = dragdroppable;

        // Plugin.Log.Info("" + s);

        if (!ImGui.BeginTable(id, 1, ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame)) return false;
        ImGui.TableSetupColumn("Primary", ImGuiTableColumnFlags.WidthStretch);
        CurrentListState = s;
        return true;
    }

    public static void EndList()
    {
        if (CurrentListState == null) return;
        var s = CurrentListState;
        CurrentListState = null;

        ListStates[s.id] = s;
        ImGui.EndTable();
    }

    public static Cursor GetCursor()
    {
        return (ImGui.GetCursorScreenPos(), ImGui.GetContentRegionAvail().X);
    }

    private static void DrawCursorLine(Cursor cursor, uint color)
    {
        ImGui.GetForegroundDrawList().AddCircle(cursor.pos, 5f, color, 0, 2f);
        ImGui.GetForegroundDrawList().PathLineTo(cursor.pos + new Vector2(5, 0));
        ImGui.GetForegroundDrawList().PathLineTo(cursor.pos + new Vector2(cursor.width, 0));
        ImGui.GetForegroundDrawList().PathStroke(color, ImDrawFlags.None, 2f);
    }

    public static bool NextRow(int currIndex, string name)
    {

        RowIndex = currIndex;
        RowName = name;
        if (CurrentListState == null) return false;
        var s = CurrentListState;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        Cursor = GetCursor();

        if (RowIndex == s.renameIndex)
        {
            if (s.renameFocus)
            {
                ImGui.SetKeyboardFocusHere(0);
                s.renameFocus = false;
            }
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 70);
            var result = ImGui.InputText("##preset_rename", ref s.renamingText, 50, ImGuiInputTextFlags.EnterReturnsTrue);
            bool isUnfocused = false; //ImGui.IsItemDeactivated();

            ImGui.SameLine();
            if (ImGuiComponents.IconButton("accept_rename", FontAwesomeIcon.Check) || result)
            {
                if (s.renamingText.Length > 0)
                {
                    IsRenamed = true;
                }
                s.renameIndex = -1;
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("cancel_rename", FontAwesomeIcon.Times) || isUnfocused)
            {
                s.renameIndex = -1;
            }
            return false;
        }

        return true;
    }

    internal static unsafe Vector4 ButtonActiveColor => *ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);
    internal static unsafe Vector4 ButtonHoveredColor => *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);


    public static void EndRow()
    {
        if (CurrentListState == null) return;
        var s = CurrentListState;

        if (dragdroppable)
        {
            if (s.dragSourceIndex < RowIndex)
                Cursor = GetCursor();

            bool isValidTarget = false;
            using (var target = ImRaii.DragDropTarget())
            {
                if (target.Success && s.dragSourceIndex >= 0)
                {
                    s.dragTargetIndex = RowIndex;
                    DrawCursorLine(Cursor, ButtonActiveColor.ToUint());
                    isValidTarget = true;
                }
            }

            using (var source = ImRaii.DragDropSource(ImGuiDragDropFlags.AcceptNoDrawDefaultRect))
            {
                if (source)
                {
                    ImGui.SetDragDropPayload(Dragging, nint.Zero, 0);
                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        ImGui.TextUnformatted(FontAwesomeIcon.GripLines.ToIconString());
                        ImGui.SameLine();
                    }
                    ImGui.TextUnformatted($"{RowName}");
                    if (isValidTarget)
                        ImGui.TextUnformatted($"Move preset here");
                    s.dragSourceIndex = RowIndex;
                }
            }

            if (IsDropping(Dragging))
            {
                IsMoved = true;
            }
        }
    }

    public static void DeleteRow()
    {
        if (CurrentListState == null) return;
        var s = CurrentListState;

        s.deleteIndex = RowIndex;
    }

    public static bool OnDelete(out int deleteIndex)
    {
        deleteIndex = -1;
        if (CurrentListState == null) return false;
        var s = CurrentListState;
        deleteIndex = s.deleteIndex;
        s.deleteIndex = -1;
        return deleteIndex >= 0;
    }

    public static void StartRowRename()
    {
        if (CurrentListState == null) return;
        var s = CurrentListState;
        s.renameIndex = RowIndex;
        s.renameFocus = true;
        s.renamingText = RowName;
    }

    public static bool OnRename(out string newName)
    {
        newName = "";
        if (!IsRenamed || CurrentListState == null) return false;
        var s = CurrentListState;
        newName = s.renamingText;
        s.renamingText = "";
        IsRenamed = false;
        return true;
    }

    public static bool OnMove(out int source, out int target)
    {
        source = -1; target = -1;
        if (!IsMoved || CurrentListState == null) return false;
        var s = CurrentListState;

        source = s.dragSourceIndex;
        target = s.dragTargetIndex;

        s.dragSourceIndex = -1;
        s.dragTargetIndex = -1;

        IsMoved = false;
        return source > -1 && target > -1;
    }

    public static bool OnStartHover()
    {
        if (CurrentListState == null) return false;
        var s = CurrentListState;

        return s.prevHoverIndex != s.hoverIndex && s.prevHoverIndex == -1;
    }

    public static bool OnStopHover()
    {
        if (CurrentListState == null) return false;
        var s = CurrentListState;

        return s.prevHoverIndex != s.hoverIndex && s.hoverIndex == -1;
    }

    public static bool IsDraggingItem()
    {
        if (CurrentListState == null) return false;
        var s = CurrentListState;

        return s.dragSourceIndex == RowIndex;
    }
}
