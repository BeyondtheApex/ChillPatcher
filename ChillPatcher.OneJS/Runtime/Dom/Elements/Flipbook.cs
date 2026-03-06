using System;
using System.Collections;
using OneJS.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace OneJS.Dom {
    public class Flipbook : Image {
        public object src {
            get { return _src; }
            set {
                if (value is Texture2D tex) {
                    _texture = tex;
                    this.image = _texture;
                    Reset();
                }
            }
        }

        public int numPerRow {
            get { return _numPerRow; }
            set {
                _numPerRow = value;
                Reset();
            }
        }

        public int count {
            get { return _count; }
            set {
                _count = value;
                Reset();
            }
        }

        public float interval {
            get { return _interval; }
            set {
                _interval = value;
                Reset();
            }
        }

        public bool randomRotation {
            get { return _randomRotation; }
            set {
                _randomRotation = value;
                Reset();
            }
        }

        string _src;
        int _numPerRow = 1;
        int _count = 1;
        float _interval = 0.0466667f;
        bool _randomRotation;

        Texture _texture;
        int _cellWidth;
        int _cellHeight;
        int _index;

        IEnumerator _currentCo;

        public Flipbook() {
        }

        void Reset() {
            if (_texture == null)
                return;
            _index = 0;
            _cellWidth = _texture.width / _numPerRow;
            _cellHeight = _texture.height / Math.Max(1, _count / _numPerRow);
            if (_currentCo != null)
                CoroutineUtil.Stop(_currentCo);
            Animate();
        }

        void Animate() {
            this.sourceRect = new Rect((_index % _numPerRow) * _cellWidth,
                (_index / _numPerRow) * _cellHeight,
                _cellWidth, _cellHeight);
            if (_randomRotation && _index == 0) {
                this.style.rotate = new StyleRotate(new Rotate(new Angle(Random.Range(0, 360f))));
            }
            this.MarkDirtyRepaint();
            _index = _index == _count - 1 ? 0 : _index + 1;
            _currentCo = CoroutineUtil.DelaySeconds(Animate, _interval);
            CoroutineUtil.Start(_currentCo);
        }
    }
}
