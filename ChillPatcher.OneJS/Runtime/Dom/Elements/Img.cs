using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom {
    public class Img : Image {
        public string Src { get { return _src; } set { SetSrc(value); } }

        IDocument _document;
        string _src;

        Coroutine _imageCoroutine;

        public Img() {
        }

        public void SetSrc(string src) {
            _src = src;
            if (string.IsNullOrEmpty(src)) {
                this.image = null;
                return;
            }
            if (IsRemoteUrl(src)) {
                StaticCoroutine.Stop(_imageCoroutine);
                _imageCoroutine = _document.loadRemoteImage(src, (texture) => {
                    this.image = texture;
                    _imageCoroutine = null;
                });
                return;
            }
            this.image = _document.loadImage(src);
        }
        
        static bool IsRemoteUrl(string path) {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uriResult)) {
                return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFtp;
            }
            return false;
        }
    }
}