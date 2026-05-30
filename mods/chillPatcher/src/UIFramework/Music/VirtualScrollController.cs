using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Bulbul;
using ObservableCollections;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// 虚拟滚动控制器实现
    /// </summary>
    public class VirtualScrollController : Core.IVirtualScrollController, IDisposable
    {
        #region Constants

        private const float DEFAULT_ITEM_HEIGHT = 60f;
        private const int DEFAULT_BUFFER_COUNT = 3;
        private const float SCROLL_TO_BOTTOM_THRESHOLD = 0.95f;

        #endregion

        #region Fields

        private ObservableCollections.IReadOnlyObservableList<GameAudioInfo> _dataSource;
        private ScrollRect _scrollRect;
        private RectTransform _contentTransform;
        private RectTransform _viewportTransform;
        private Bulbul.FacilityMusic _facilityMusic; // 保存FacilityMusic引用

        // 对象池
        private UIObjectPool<MusicPlayListButtons> _buttonPool;
        private Dictionary<int, MusicPlayListButtons> _activeItems;

        // 配置
        private float _itemHeight = DEFAULT_ITEM_HEIGHT;
        private int _bufferCount = DEFAULT_BUFFER_COUNT;

        // 状态
        private int _visibleStartIndex = 0;
        private int _visibleEndIndex = 0;
        private float _lastScrollPosition = 0f;
        private bool _isInitialized = false;
        
        // ObservableCollection订阅
        private IDisposable _dataSourceSubscription;

        #endregion

        #region Properties

        public int VisibleStartIndex => _visibleStartIndex;
        public int VisibleEndIndex => _visibleEndIndex;
        public int TotalItemCount => _dataSource?.Count ?? 0;
        public float ScrollPercentage => CalculateScrollPercentage();
        public float ItemHeight
        {
            get => _itemHeight;
            set
            {
                _itemHeight = value;
                UpdateContentSize();
            }
        }
        public int BufferCount
        {
            get => _bufferCount;
            set => _bufferCount = value;
        }

        /// <summary>
        /// 当前数据源数量（用于检测变化）
        /// </summary>
        public int DataSourceCount => _dataSource?.Count ?? 0;
        
        /// <summary>
        /// 是否有活动的渲染项（用于检测是否需要初始渲染）
        /// </summary>
        public bool HasActiveItems => _activeItems.Count > 0;

        #endregion

        #region Events

        public event Action<int, int> OnVisibleRangeChanged;
        public event Action OnScrollToBottom;

        #endregion

        #region Constructor

        public VirtualScrollController()
        {
            _activeItems = new Dictionary<int, MusicPlayListButtons>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 设置FacilityMusic引用（从Patch传入）
        /// </summary>
        public void SetFacilityMusic(Bulbul.FacilityMusic facilityMusic)
        {
            _facilityMusic = facilityMusic;
        }

        /// <summary>
        /// 设置数据源
        /// </summary>
        public void SetDataSource(ObservableCollections.IReadOnlyObservableList<GameAudioInfo> source)
        {
            bool needsSubscribe = (_dataSource != source);

            if (_dataSource != null && needsSubscribe)
            {
                // 取消订阅旧数据源
                UnsubscribeFromDataSource();
            }

            _dataSource = source;

            if (_dataSource != null)
            {
                // 总是清空旧项并重建
                ClearAllActiveItems();
                
                // 如果是新的引用，订阅事件
                if (needsSubscribe)
                {
                    SubscribeToDataSource();
                }
                
                UpdateContentSize();
                RefreshVisible();
                
                // 关键：在下一帧再次强制更新，确保ScrollRect完全刷新
                // if (_scrollRect != null)
                // {
                //     UnityEngine.Canvas.ForceUpdateCanvases();
                // }
            }
        }
        
        /// <summary>
        /// 清空所有活动项
        /// </summary>
        private void ClearAllActiveItems()
        {
            foreach (var pair in _activeItems)
            {
                _buttonPool.Return(pair.Value);
            }
            _activeItems.Clear();
        }

        /// <summary>
        /// 刷新可见项
        /// </summary>
        public void RefreshVisible()
        {
            if (!_isInitialized || _dataSource == null)
                return;

            var (start, end) = CalculateVisibleRange();

            // 回收不可见项
            RecycleInvisibleItems(start, end);

            // 渲染可见项
            RenderVisibleItems(start, end);

            // 更新范围
            if (start != _visibleStartIndex || end != _visibleEndIndex)
            {
                _visibleStartIndex = start;
                _visibleEndIndex = end;
                OnVisibleRangeChanged?.Invoke(start, end);
            }

            // 检查是否滚动到底部
            if (ScrollPercentage >= SCROLL_TO_BOTTOM_THRESHOLD)
            {
                OnScrollToBottom?.Invoke();
            }
        }

        /// <summary>
        /// 刷新指定项
        /// </summary>
        public void RefreshItem(int index)
        {
            if (!_activeItems.TryGetValue(index, out var button))
                return;

            if (index < 0 || index >= TotalItemCount)
                return;

            var audioInfo = _dataSource[index];
            button.Setup(audioInfo, GetFacilityMusic());
        }

        /// <summary>
        /// 滚动到指定项
        /// </summary>
        public void ScrollToItem(int index, bool smooth = true)
        {
            if (!_isInitialized || _scrollRect == null)
                return;

            if (index < 0 || index >= TotalItemCount)
                return;

            float targetPosition = index * _itemHeight;
            float viewportHeight = _viewportTransform.rect.height;
            float contentHeight = _contentTransform.rect.height;

            // 计算滚动位置（0-1）
            float normalizedPosition = 1f - Mathf.Clamp01(targetPosition / (contentHeight - viewportHeight));

            if (smooth)
            {
                // 平滑滚动
                DG.Tweening.DOTween.To(
                    () => _scrollRect.verticalNormalizedPosition,
                    x => _scrollRect.verticalNormalizedPosition = x,
                    normalizedPosition,
                    0.3f
                );
            }
            else
            {
                _scrollRect.verticalNormalizedPosition = normalizedPosition;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化UI组件（由Patch调用）
        /// </summary>
        public void InitializeComponents(ScrollRect scrollRect, GameObject buttonPrefab, Transform contentParent)
        {
            if (_isInitialized)
                return;

            _scrollRect = scrollRect;
            _contentTransform = contentParent as RectTransform;
            _viewportTransform = scrollRect.viewport;

            // 关键修复1：禁用Content上的ContentSizeFitter（如果有）
            // ContentSizeFitter会根据子对象自动计算大小，这会破坏虚拟滚动
            var contentSizeFitter = _contentTransform.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (contentSizeFitter != null)
            {
                contentSizeFitter.enabled = false;
            }
            
            // 关键修复2：禁用LayoutGroup（如果有）
            // LayoutGroup会自动排列子对象，我们需要手动控制位置
            var layoutGroup = _contentTransform.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }

            // 关键修复3：确保Content的锚点设置正确
            // 对于垂直滚动列表，Content应该锚定在顶部
            _contentTransform.anchorMin = new Vector2(0, 1);  // 左上角
            _contentTransform.anchorMax = new Vector2(1, 1);  // 右上角
            _contentTransform.pivot = new Vector2(0.5f, 1);   // 顶部中心为轴心
            
            // 初始位置设为(0, 0)，即顶部对齐
            _contentTransform.anchoredPosition = new Vector2(0, 0);

            // 初始化对象池
            _buttonPool = new UIObjectPool<MusicPlayListButtons>(buttonPrefab, contentParent);

            // 订阅滚动事件
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

            _isInitialized = true;
        }

        #endregion

        #region Private Methods

        private void OnScrollValueChanged(Vector2 position)
        {
            if (Mathf.Abs(position.y - _lastScrollPosition) > 0.001f)
            {
                _lastScrollPosition = position.y;
                RefreshVisible();
            }
        }

        private (int start, int end) CalculateVisibleRange()
        {
            if (_scrollRect == null || _dataSource == null || _dataSource.Count == 0)
                return (0, 0);

            float viewportHeight = _viewportTransform.rect.height;
            // 关键：scrollPosition是Content相对于Viewport的Y偏移
            // 当滚动到顶部时，anchoredPosition.y = 0
            // 当向下滚动时，anchoredPosition.y 增加
            float scrollPosition = _contentTransform.anchoredPosition.y;

            // 计算第一个可见项的索引
            // scrollPosition / itemHeight = 当前滚动了多少个项目
            int start = Mathf.Max(0, Mathf.FloorToInt(scrollPosition / _itemHeight) - _bufferCount);
            
            // 计算视口可以显示多少个项目
            int visibleCount = Mathf.CeilToInt(viewportHeight / _itemHeight);
            
            // 计算最后一个可见项的索引（加上缓冲区）
            int end = Mathf.Min(_dataSource.Count, start + visibleCount + _bufferCount * 2);

            return (start, end);
        }

        private void RecycleInvisibleItems(int newStart, int newEnd)
        {
            var itemsToRecycle = _activeItems
                .Where(pair => pair.Key < newStart || pair.Key >= newEnd || pair.Key >= _dataSource.Count)
                .ToList();

            foreach (var pair in itemsToRecycle)
            {
                _buttonPool.Return(pair.Value);
                _activeItems.Remove(pair.Key);
            }
        }

        private void RenderVisibleItems(int start, int end)
        {
            // 确保有FacilityMusic引用
            if (_facilityMusic == null)
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogWarning("FacilityMusic is null, cannot render items");
                return;
            }
            
            for (int i = start; i < end; i++)
            {
                if (_activeItems.ContainsKey(i))
                    continue;

                if (i >= _dataSource.Count)
                    break;

                var button = _buttonPool.Get();
                
                // 调用Setup初始化按钮（使用游戏原生逻辑）
                button.Setup(_dataSource[i], _facilityMusic);

                
                PositionItem(button, i);

                _activeItems[i] = button;
            }
        }

        private void PositionItem(MusicPlayListButtons button, int index)
        {
            var rt = button.GetComponent<RectTransform>();
            
            // 关键修复：手动设置RectTransform的完整属性
            // 因为我们禁用了LayoutGroup，必须手动配置每个项目
            
            // 1. 锚点：水平拉伸，垂直顶部对齐
            rt.anchorMin = new Vector2(0, 1);  // 左上角
            rt.anchorMax = new Vector2(1, 1);  // 右上角
            rt.pivot = new Vector2(0.5f, 1);   // 顶部中心为轴心
            
            // 2. 大小：宽度自适应父容器，高度固定
            rt.sizeDelta = new Vector2(0, _itemHeight);  // width=0表示使用anchorMin/Max的宽度
            
            // 3. 位置：Y坐标 = -index * itemHeight（从上到下，注意是负数）
            rt.anchoredPosition = new Vector2(0, -index * _itemHeight);
            
            // 4. 确保缩放和旋转正确
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private void UpdateContentSize()
        {
            if (_contentTransform == null || _dataSource == null)
                return;

            // 关键修复：Content的高度必须等于总项目数的高度
            // 这样滚动条才能正确显示比例
            float totalHeight = _dataSource.Count * _itemHeight;
            
            // 保存旧的宽度，只修改高度
            float currentWidth = _contentTransform.sizeDelta.x;
            _contentTransform.sizeDelta = new Vector2(currentWidth, totalHeight);
            
            // 关键：强制ScrollRect立即更新布局
            // 这样滚动条会立即显示
            // if (_scrollRect != null)
            // {
            //     UnityEngine.Canvas.ForceUpdateCanvases();
            //     _scrollRect.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);
            // }
        }

        private float CalculateScrollPercentage()
        {
            if (_scrollRect == null || _contentTransform == null)
                return 0f;

            float contentHeight = _contentTransform.rect.height;
            float viewportHeight = _viewportTransform.rect.height;

            if (contentHeight <= viewportHeight)
                return 0f;

            float scrollPosition = _contentTransform.anchoredPosition.y;
            return scrollPosition / (contentHeight - viewportHeight);
        }

        private void SubscribeToDataSource()
        {
            UnsubscribeFromDataSource(); // 先清理旧订阅
            
            if (_dataSource == null)
                return;
            
            // 订阅Count变化事件（当列表添加/删除项时触发）
            _dataSourceSubscription = _dataSource
                .ObserveCountChanged(notifyCurrentCount: false, cancellationToken: default(CancellationToken))
                .Subscribe(_ =>
                {
                    // Count变化时，更新内容大小并刷新可见区域
                    UpdateContentSize();
                    RefreshVisible();
                });
        }

        private void UnsubscribeFromDataSource()
        {
            _dataSourceSubscription?.Dispose();
            _dataSourceSubscription = null;
        }

        private FacilityMusic GetFacilityMusic()
        {
            // 从游戏中获取FacilityMusic实例
            // TODO: 需要正确的VContainer API
            /*
            try
            {
                var roomLifetime = VContainer.RoomLifetimeScope.Find();
                if (roomLifetime != null)
                {
                    return roomLifetime.Container.Resolve<FacilityMusic>();
                }
            }
            catch (Exception ex)
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogWarning($"Failed to get FacilityMusic: {ex.Message}");
            }
            */

            return null;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 场景重载前重置状态，允许重新初始化
        /// </summary>
        public void ResetForSceneReload()
        {
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            _buttonPool?.Clear();
            _activeItems?.Clear();
            UnsubscribeFromDataSource();

            _scrollRect = null;
            _contentTransform = null;
            _viewportTransform = null;
            _dataSource = null;
            _isInitialized = false;
        }

        public void Dispose()
        {
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }

            _buttonPool?.Clear();
            _activeItems?.Clear();

            UnsubscribeFromDataSource();

            _scrollRect = null;
            _contentTransform = null;
            _viewportTransform = null;
            _dataSource = null;
            _buttonPool = null;
            _activeItems = null;
        }

        #endregion
    }
}

