using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ChillPatcher.JSApi
{
    /// <summary>
    /// Unity UGUI 树操作 API
    /// 
    /// JS 端用法：
    ///   chill.ui.getTree("Paremt/Canvas/UI", 2)
    ///   chill.ui.find("Paremt/Canvas/UI/MostFrontArea/TopIcons")
    ///   chill.ui.click("Paremt/Canvas/UI/.../IconExit_Button")
    ///   chill.ui.hide("Paremt/Canvas/UI/BottomBackImage")
    ///   chill.ui.show("Paremt/Canvas/UI/BottomBackImage")
    /// </summary>
    public class ChillUIApi
    {
        private readonly ManualLogSource _logger;

        public ChillUIApi(ManualLogSource logger)
        {
            _logger = logger;
        }

        #region UI 树查询

        /// <summary>
        /// 获取当前场景的所有根 GameObject 名称列表
        /// </summary>
        public string getRoots()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var result = new List<Dictionary<string, object>>();
            foreach (var root in roots)
            {
                result.Add(new Dictionary<string, object>
                {
                    ["name"] = root.name,
                    ["active"] = root.activeSelf,
                    ["childCount"] = root.transform.childCount
                });
            }
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 获取指定路径下的 UI 树结构
        /// </summary>
        /// <param name="path">GameObject 路径（如 "Paremt/Canvas/UI"）</param>
        /// <param name="depth">递归深度，默认 1（仅直接子节点），-1 为无限深度</param>
        /// <returns>树节点信息字典</returns>
        public string getTree(string path, int depth = 1)
        {
            // 使用 FindByPath 支持查找未激活的对象
            var go = FindByPath(path) ?? GameObject.Find(path);
            if (go == null)
            {
                _logger.LogWarning($"[UIApi] getTree: 路径不存在: {path}");
                return "null";
            }
            return JSApiHelper.ToJson(BuildTreeNode(go.transform, depth, 0));
        }

        /// <summary>
        /// 获取指定路径 UI 节点的信息
        /// </summary>
        public string find(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return "null";
            return JSApiHelper.ToJson(GetNodeInfo(go));
        }

        /// <summary>
        /// 获取指定路径节点的所有直接子节点名称
        /// </summary>
        public string getChildren(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return "[]";

            var t = go.transform;
            var result = new string[t.childCount];
            for (int i = 0; i < t.childCount; i++)
                result[i] = t.GetChild(i).name;
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 在指定路径下按名称递归查找第一个匹配的子节点，返回完整路径
        /// </summary>
        public string findChild(string parentPath, string childName)
        {
            var go = GameObject.Find(parentPath);
            if (go == null) return null;

            var found = FindChildRecursive(go.transform, childName);
            return found != null ? GetFullPath(found) : null;
        }

        #endregion

        #region UI 可见性控制

        /// <summary>
        /// 隐藏指定路径的 UI 子树（不再显示且不接收交互）
        /// </summary>
        public bool hide(string path)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                _logger.LogWarning($"[UIApi] hide: 路径不存在: {path}");
                return false;
            }

            // 优先使用 CanvasGroup 隐藏（保留 GameObject 激活状态，更安全）
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            return true;
        }

        /// <summary>
        /// 显示之前隐藏的 UI 子树
        /// </summary>
        public bool show(string path)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                _logger.LogWarning($"[UIApi] show: 路径不存在: {path}");
                return false;
            }

            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }

            // 确保 GameObject 本身也是激活的
            if (!go.activeSelf)
                go.SetActive(true);

            return true;
        }

        /// <summary>
        /// 设置指定路径 UI 节点的透明度（0~1）
        /// </summary>
        public bool setAlpha(string path, float alpha)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;

            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = go.AddComponent<CanvasGroup>();

            cg.alpha = Mathf.Clamp01(alpha);
            return true;
        }

        /// <summary>
        /// 通过 SetActive 设置 UI 节点的激活状态
        /// </summary>
        public bool setActive(string path, bool active)
        {
            // 使用 FindByPath 支持查找未激活的对象
            var go = FindByPath(path);
            if (go == null) return false;

            go.SetActive(active);
            return true;
        }

        /// <summary>
        /// 判断指定路径节点是否激活且可见
        /// </summary>
        public bool isVisible(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;
            if (!go.activeInHierarchy) return false;

            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null && cg.alpha <= 0f) return false;

            return true;
        }

        #endregion

        #region UI 交互

        /// <summary>
        /// 模拟点击指定路径上的 UI 按钮
        /// 支持 UGUI Button / IPointerClickHandler
        /// </summary>
        public bool click(string path)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                _logger.LogWarning($"[UIApi] click: 路径不存在: {path}");
                return false;
            }

            // 尝试 UGUI Button
            var button = go.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
                return true;
            }

            // 尝试 IPointerClickHandler
            var clickHandlers = go.GetComponents<IPointerClickHandler>();
            if (clickHandlers != null && clickHandlers.Length > 0)
            {
                var eventData = new PointerEventData(EventSystem.current);
                foreach (var handler in clickHandlers)
                    handler.OnPointerClick(eventData);
                return true;
            }

            _logger.LogWarning($"[UIApi] click: 路径上没有可点击的组件: {path}");
            return false;
        }

        /// <summary>
        /// 获取指定路径 UI 节点上的可交互组件列表
        /// </summary>
        public string getInteractables(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return "[]";

            var result = new List<string>();
            if (go.GetComponent<Button>() != null) result.Add("Button");
            if (go.GetComponent<Toggle>() != null) result.Add("Toggle");
            if (go.GetComponent<Slider>() != null) result.Add("Slider");
            if (go.GetComponent<InputField>() != null) result.Add("InputField");
            if (go.GetComponent<Dropdown>() != null) result.Add("Dropdown");
            if (go.GetComponent<ScrollRect>() != null) result.Add("ScrollRect");
            if (go.GetComponent<Scrollbar>() != null) result.Add("Scrollbar");
            return JSApiHelper.ToJson(result);
        }

        /// <summary>
        /// 设置 Toggle 组件的值
        /// </summary>
        public bool setToggle(string path, bool isOn)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;

            var toggle = go.GetComponent<Toggle>();
            if (toggle == null) return false;

            toggle.isOn = isOn;
            return true;
        }

        /// <summary>
        /// 设置 Slider 组件的值
        /// </summary>
        public bool setSlider(string path, float value)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;

            var slider = go.GetComponent<Slider>();
            if (slider == null) return false;

            slider.value = value;
            return true;
        }

        #endregion

        #region UI 位置与大小

        /// <summary>
        /// 获取节点的 RectTransform 信息
        /// </summary>
        public string getRect(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return "null";

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return "null";

            return JSApiHelper.ToJson(new Dictionary<string, object>
            {
                ["x"] = rect.anchoredPosition.x,
                ["y"] = rect.anchoredPosition.y,
                ["width"] = rect.sizeDelta.x,
                ["height"] = rect.sizeDelta.y,
                ["anchorMinX"] = rect.anchorMin.x,
                ["anchorMinY"] = rect.anchorMin.y,
                ["anchorMaxX"] = rect.anchorMax.x,
                ["anchorMaxY"] = rect.anchorMax.y,
                ["pivotX"] = rect.pivot.x,
                ["pivotY"] = rect.pivot.y,
                ["scaleX"] = rect.localScale.x,
                ["scaleY"] = rect.localScale.y
            });
        }

        /// <summary>
        /// 设置节点的 anchoredPosition
        /// </summary>
        public bool setPosition(string path, float x, float y)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return false;

            rect.anchoredPosition = new Vector2(x, y);
            return true;
        }

        /// <summary>
        /// 设置节点的 sizeDelta
        /// </summary>
        public bool setSize(string path, float width, float height)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return false;

            rect.sizeDelta = new Vector2(width, height);
            return true;
        }

        /// <summary>
        /// 设置节点的 localScale
        /// </summary>
        public bool setScale(string path, float scaleX, float scaleY)
        {
            var go = GameObject.Find(path);
            if (go == null) return false;

            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            return true;
        }

        #endregion

        #region 内部方法

        private object BuildTreeNode(Transform t, int maxDepth, int currentDepth)
        {
            var node = GetNodeInfo(t.gameObject);
            var dict = (Dictionary<string, object>)node;

            if (maxDepth == 0) return dict;

            if (t.childCount > 0 && (maxDepth < 0 || currentDepth < maxDepth))
            {
                var children = new object[t.childCount];
                for (int i = 0; i < t.childCount; i++)
                    children[i] = BuildTreeNode(t.GetChild(i), maxDepth, currentDepth + 1);
                dict["children"] = children;
            }

            return dict;
        }

        private object GetNodeInfo(GameObject go)
        {
            var dict = new Dictionary<string, object>
            {
                ["name"] = go.name,
                ["path"] = GetFullPath(go.transform),
                ["active"] = go.activeSelf,
                ["activeInHierarchy"] = go.activeInHierarchy,
                ["childCount"] = go.transform.childCount
            };

            var rect = go.GetComponent<RectTransform>();
            if (rect != null)
            {
                dict["hasRect"] = true;
                dict["x"] = rect.anchoredPosition.x;
                dict["y"] = rect.anchoredPosition.y;
            }

            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
                dict["alpha"] = cg.alpha;

            // 标记可交互组件
            var interactables = new List<string>();
            if (go.GetComponent<Button>() != null) interactables.Add("Button");
            if (go.GetComponent<Toggle>() != null) interactables.Add("Toggle");
            if (go.GetComponent<Slider>() != null) interactables.Add("Slider");
            if (go.GetComponent<ScrollRect>() != null) interactables.Add("ScrollRect");
            if (interactables.Count > 0)
                dict["interactables"] = interactables.ToArray();

            return dict;
        }

        /// <summary>
        /// 通过路径查找 GameObject，支持未激活的对象
        /// 通过场景根 + Transform 层级遍历实现
        /// </summary>
        private GameObject FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var segments = path.Split('/');
            
            // 先找根对象（Scene.GetRootGameObjects 包含未激活的）
            Transform current = null;
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name == segments[0])
                {
                    current = root.transform;
                    break;
                }
            }
            if (current == null) return null;

            // 逐级用 Transform.Find（可访问未激活子对象）
            for (int i = 1; i < segments.Length; i++)
            {
                current = current.Find(segments[i]);
                if (current == null) return null;
            }

            return current.gameObject;
        }

        private Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                var found = FindChildRecursive(child, childName);
                if (found != null) return found;
            }
            return null;
        }

        private string GetFullPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        #endregion
    }
}
