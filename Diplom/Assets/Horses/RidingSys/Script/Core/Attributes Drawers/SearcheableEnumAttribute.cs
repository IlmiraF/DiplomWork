using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RidingSystem
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SearcheableEnumAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SearcheableEnumAttribute))]
    public class SearchableEnumDrawer : PropertyDrawer
    {
        private const string TYPE_ERROR =
            "SearchableEnum can only be used on enum fields.";
        private int idHash;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.type != "Enum")
            {
                GUIStyle errorStyle = "CN EntryErrorIconSmall";
                Rect r = new Rect(position);
                r.width = errorStyle.fixedWidth;
                position.xMin = r.xMax;
                GUI.Label(r, "", errorStyle);
                GUI.Label(position, TYPE_ERROR);
                return;
            }

            if (idHash == 0) idHash = "SearchableEnumDrawer".GetHashCode();
            int id = GUIUtility.GetControlID(idHash, FocusType.Keyboard, position);

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, id, label);

            GUIContent buttonText;
            if (property.enumValueIndex < 0 || property.enumValueIndex >= property.enumDisplayNames.Length)
            {
                buttonText = new GUIContent();
            }
            else
            {
                buttonText = new GUIContent(property.enumDisplayNames[property.enumValueIndex]);
            }

            if (DropdownButton(id, position, buttonText))
            {
                Action<int> onSelect = i =>
                {
                    property.enumValueIndex = i;
                    property.serializedObject.ApplyModifiedProperties();
                };

                SearchablePopup.Show(position, property.enumDisplayNames, property.enumValueIndex, onSelect);
            }
            EditorGUI.EndProperty();
        }

        private static bool DropdownButton(int id, Rect position, GUIContent content)
        {
            Event current = Event.current;
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && current.button == 0)
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id && current.character == '\n')
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.Repaint:
                    EditorStyles.popup.Draw(position, content, id, false);
                    break;
            }
            return false;
        }
    }


    public class SearchablePopup : PopupWindowContent
    {
        #region -- Constants --------------------------------------------------
        private const float ROW_HEIGHT = 16.0f;

        private const float ROW_INDENT = 8.0f;

        private const string SEARCH_CONTROL_NAME = "EnumSearchText";
        #endregion -- Constants -----------------------------------------------

        #region -- Static Functions -------------------------------------------
        public static void Show(Rect activatorRect, string[] options, int current, Action<int> onSelectionMade)
        {
            SearchablePopup win =
                new SearchablePopup(options, current, onSelectionMade);
            PopupWindow.Show(activatorRect, win);
        }

        private static void Repaint()
        { EditorWindow.focusedWindow.Repaint(); }

        private static void DrawBox(Rect rect, Color tint)
        {
            Color c = GUI.color;
            GUI.color = tint;
            GUI.Box(rect, "", Selection);
            GUI.color = c;
        }
        #endregion -- Static Functions ----------------------------------------

        #region -- Helper Classes ---------------------------------------------
        private class FilteredList
        {
            public struct Entry
            {
                public int Index;
                public string Text;
            }

            private readonly string[] allItems;

            public FilteredList(string[] items)
            {
                allItems = items;
                Entries = new List<Entry>();
                UpdateFilter("");
            }

            public string Filter { get; private set; }

            public List<Entry> Entries { get; private set; }

            public int MaxLength
            { get { return allItems.Length; } }

            public bool UpdateFilter(string filter)
            {
                if (Filter == filter)
                    return false;

                Filter = filter;
                Entries.Clear();

                for (int i = 0; i < allItems.Length; i++)
                {
                    if (string.IsNullOrEmpty(Filter) || allItems[i].ToLower().Contains(Filter.ToLower()))
                    {
                        Entry entry = new Entry
                        {
                            Index = i,
                            Text = allItems[i]
                        };
                        if (string.Equals(allItems[i], Filter, StringComparison.CurrentCultureIgnoreCase))
                            Entries.Insert(0, entry);
                        else
                            Entries.Add(entry);
                    }
                }
                return true;
            }
        }
        #endregion -- Helper Classes ------------------------------------------

        #region -- Private Variables ------------------------------------------
        private readonly Action<int> onSelectionMade;

        private readonly int currentIndex;

        private readonly FilteredList list;

        private Vector2 scroll;

        private int hoverIndex;

        private int scrollToIndex;

        private float scrollOffset;
        #endregion -- Private Variables ---------------------------------------

        #region -- GUI Styles -------------------------------------------------
        private static GUIStyle SearchBox = "ToolbarSeachTextField";
        private static GUIStyle CancelButton = "ToolbarSeachCancelButton";
        private static GUIStyle DisabledCancelButton = "ToolbarSeachCancelButtonEmpty";
        private static GUIStyle Selection = "SelectionRect";
        #endregion -- GUI Styles ----------------------------------------------

        #region -- Initialization ---------------------------------------------
        private SearchablePopup(string[] names, int currentIndex, Action<int> onSelectionMade)
        {
            list = new FilteredList(names);
            this.currentIndex = currentIndex;
            this.onSelectionMade = onSelectionMade;

            hoverIndex = currentIndex;
            scrollToIndex = currentIndex;
            scrollOffset = GetWindowSize().y - ROW_HEIGHT * 2;
        }
        #endregion -- Initialization ------------------------------------------

        #region -- PopupWindowContent Overrides -------------------------------
        public override void OnOpen()
        {
            base.OnOpen();
            EditorApplication.update += Repaint;
        }

        public override void OnClose()
        {
            base.OnClose();
            EditorApplication.update -= Repaint;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(base.GetWindowSize().x,
                Mathf.Min(600, list.MaxLength * ROW_HEIGHT +
                EditorStyles.toolbar.fixedHeight));
        }

        public override void OnGUI(Rect rect)
        {
            Rect searchRect = new Rect(0, 0, rect.width, EditorStyles.toolbar.fixedHeight);
            Rect scrollRect = Rect.MinMaxRect(0, searchRect.yMax, rect.xMax, rect.yMax);

            HandleKeyboard();
            DrawSearch(searchRect);
            DrawSelectionArea(scrollRect);
        }
        #endregion -- PopupWindowContent Overrides ----------------------------

        #region -- GUI --------------------------------------------------------
        private void DrawSearch(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbar.Draw(rect, false, false, false, false);

            Rect searchRect = new Rect(rect);
            searchRect.xMin += 6;
            searchRect.xMax -= 6;
            searchRect.y += 2;
            searchRect.width -= CancelButton.fixedWidth;

            GUI.FocusControl(SEARCH_CONTROL_NAME);
            GUI.SetNextControlName(SEARCH_CONTROL_NAME);
            string newText = GUI.TextField(searchRect, list.Filter, SearchBox);

            if (list.UpdateFilter(newText))
            {
                hoverIndex = 0;
                scroll = Vector2.zero;
            }

            searchRect.x = searchRect.xMax;
            searchRect.width = CancelButton.fixedWidth;

            if (string.IsNullOrEmpty(list.Filter))
                GUI.Box(searchRect, GUIContent.none, DisabledCancelButton);
            else if (GUI.Button(searchRect, "x", CancelButton))
            {
                list.UpdateFilter("");
                scroll = Vector2.zero;
            }
        }

        private void DrawSelectionArea(Rect scrollRect)
        {
            Rect contentRect = new Rect(0, 0,
                scrollRect.width - GUI.skin.verticalScrollbar.fixedWidth,
                list.Entries.Count * ROW_HEIGHT);

            scroll = GUI.BeginScrollView(scrollRect, scroll, contentRect);

            Rect rowRect = new Rect(0, 0, scrollRect.width, ROW_HEIGHT);

            for (int i = 0; i < list.Entries.Count; i++)
            {
                if (scrollToIndex == i &&
                    (Event.current.type == EventType.Repaint
                     || Event.current.type == EventType.Layout))
                {
                    Rect r = new Rect(rowRect);
                    r.y += scrollOffset;
                    GUI.ScrollTo(r);
                    scrollToIndex = -1;
                    scroll.x = 0;
                }

                if (rowRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseMove ||
                        Event.current.type == EventType.ScrollWheel)
                        hoverIndex = i;
                    if (Event.current.type == EventType.MouseDown)
                    {
                        onSelectionMade(list.Entries[i].Index);
                        EditorWindow.focusedWindow.Close();
                    }
                }

                DrawRow(rowRect, i);

                rowRect.y = rowRect.yMax;
            }

            GUI.EndScrollView();
        }

        private void DrawRow(Rect rowRect, int i)
        {
            if (list.Entries[i].Index == currentIndex)
                DrawBox(rowRect, Color.cyan);
            else if (i == hoverIndex)
                DrawBox(rowRect, Color.white);

            Rect labelRect = new Rect(rowRect);
            labelRect.xMin += ROW_INDENT;

            GUI.Label(labelRect, list.Entries[i].Text);
        }

        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    hoverIndex = Mathf.Min(list.Entries.Count - 1, hoverIndex + 1);
                    Event.current.Use();
                    scrollToIndex = hoverIndex;
                    scrollOffset = ROW_HEIGHT;
                }

                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    hoverIndex = Mathf.Max(0, hoverIndex - 1);
                    Event.current.Use();
                    scrollToIndex = hoverIndex;
                    scrollOffset = -ROW_HEIGHT;
                }

                if (Event.current.keyCode == KeyCode.Return)
                {
                    if (hoverIndex >= 0 && hoverIndex < list.Entries.Count)
                    {
                        onSelectionMade(list.Entries[hoverIndex].Index);
                        EditorWindow.focusedWindow.Close();
                    }
                }

                if (Event.current.keyCode == KeyCode.Escape)
                {
                    EditorWindow.focusedWindow.Close();
                }
            }
        }
        #endregion -- GUI -----------------------------------------------------
    }

#endif

}