using UnityEngine;
using UnityEngine.UIElements;

namespace OneJS.Dom {
    public class GradientRect : VisualElement {
        static readonly Vertex[] vertices = new Vertex[4];
        static readonly ushort[] indices = { 0, 1, 2, 2, 3, 0 };

        public Color[] Colors { get; set; }

        public GradientRect() {
            generateVisualContent = GenerateVisualContent;
        }

        void GenerateVisualContent(MeshGenerationContext mgc) {
            var rect = contentRect;
            if (rect.width < 0.1f || rect.height < 0.1f)
                return;
            if (Colors.Length == 1) {
                vertices[0].tint = Colors[0];
                vertices[1].tint = Colors[0];
                vertices[2].tint = Colors[0];
                vertices[3].tint = Colors[0];
            } else if (Colors.Length == 2) {
                vertices[0].tint = Colors[0];
                vertices[1].tint = Colors[0];
                vertices[2].tint = Colors[1];
                vertices[3].tint = Colors[1];
            } else if (Colors.Length == 3) {
                vertices[0].tint = Colors[0];
                vertices[1].tint = Colors[1];
                vertices[2].tint = Colors[2];
                vertices[3].tint = Colors[2];
            } else if (Colors.Length == 4) {
                vertices[0].tint = Colors[0];
                vertices[1].tint = Colors[1];
                vertices[2].tint = Colors[2];
                vertices[3].tint = Colors[3];
            }

            var left = rect.xMin;
            var right = rect.xMax;
            var top = rect.yMin;
            var bottom = rect.yMax;

            vertices[0].position = new Vector3(left, bottom, Vertex.nearZ);
            vertices[1].position = new Vector3(left, top, Vertex.nearZ);
            vertices[2].position = new Vector3(right, top, Vertex.nearZ);
            vertices[3].position = new Vector3(right, bottom, Vertex.nearZ);

            MeshWriteData mwd = mgc.Allocate(vertices.Length, indices.Length);
            mwd.SetAllVertices(vertices);
            mwd.SetAllIndices(indices);
        }
    }
}