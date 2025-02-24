using UnityEngine;
using UnityEditor;

namespace ModTool.Editor
{
    public abstract class SelectionWindow<T> : EditorWindow where T : SelectionWindow<T>
    {        
        protected abstract new string title { get; }

        protected abstract string message { get; }

        private Vector2 scrollPosition;

        /// <summary>
        /// Open A selection window.
        /// </summary>
        public static void Open()
        {            
            T window = GetWindow<T>(true, "");

            window.minSize = new Vector2(400f, 300f);
            window.titleContent = new GUIContent(window.title);

            window.Show();
        }

        private void OnEnable()
        {
            Init();
        }       

        private void OnGUI()
        {
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Space(5);
                GUILayout.Label(message, EditorStyles.wordWrappedLabel);
                GUILayout.Space(5);

                DrawLine();
                GUILayout.Space(-2);

                using (var scrollView = new GUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scrollView.scrollPosition;

                    Rect backgroundRect = new Rect(0, 0, position.width, position.height);
                    EditorGUI.DrawRect(backgroundRect, new Color(0, 0, 0, .1f));

                    RenderItems();
                }

                DrawLine();

                GUILayout.Space(5);

                using (new GUILayout.HorizontalScope())
                {
                    RenderFooter();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select"))
                        Select();
                }

                GUILayout.Space(5);
            }
        }

        protected abstract void Init();

        protected abstract void RenderItems();

        protected virtual void RenderFooter()
        {

        }

        protected abstract void SelectItems();

        private void Select()
        {
            SelectItems();
            Close();
        }

        protected static void DrawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, -1, GUILayout.ExpandWidth(true));

            rect.y -= 2;
            rect.x -= 3;

            rect.width += 6;
            rect.height = 1;

            EditorGUI.DrawRect(rect, new Color(0, 0, 0, .4f));
        }
    }
}
