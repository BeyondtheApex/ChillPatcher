using System.Collections.Generic;
using OmniMixPlayer.SDK.Interfaces;

namespace OmniMixPlayer.SDK.Interfaces
{
    public static class SlintUi
    {
        public static SlintNode Column(float spacing = 8, float padding = 0)
        {
            return new SlintNode
            {
                NodeType = "Container",
                Direction = "Vertical",
                Spacing = spacing,
                Padding = padding
            };
        }

        public static SlintNode Row(float spacing = 8, float padding = 0)
        {
            return new SlintNode
            {
                NodeType = "Container",
                Direction = "Horizontal",
                Spacing = spacing,
                Padding = padding
            };
        }

        public static SlintNode Text(string text, float fontSize = 14, string color = null)
        {
            return new SlintNode
            {
                NodeType = "Text",
                Text = text,
                FontSize = fontSize,
                Color = color ?? "#e0e0e0"
            };
        }

        public static SlintNode Input(string id, string placeholder = "", string value = "",
            string inputType = "text")
        {
            return new SlintNode
            {
                Id = id,
                NodeType = "Input",
                Text = placeholder,
                Value = value,
                InputType = inputType
            };
        }

        public static SlintNode Button(string id, string text, string variant = "primary")
        {
            return new SlintNode
            {
                Id = id,
                NodeType = "Button",
                Text = text,
                ButtonVariant = variant
            };
        }

        public static SlintNode Switch(string id, string label, bool @checked = false)
        {
            return new SlintNode
            {
                Id = id,
                NodeType = "Switch",
                Text = label,
                Checked = @checked
            };
        }

        public static SlintNode Image(string id, string source, float width = 200, float height = 200,
            string fit = "contain")
        {
            return new SlintNode
            {
                Id = id,
                NodeType = "Image",
                Source = source,
                ImageWidth = width,
                ImageHeight = height,
                ImageFit = fit
            };
        }

        public static SlintNode Select(string id, string label, string selected,
            List<SlintOption> options)
        {
            return new SlintNode
            {
                Id = id,
                NodeType = "Select",
                Text = label,
                SelectedValue = selected,
                Options = options
            };
        }

        public static SlintNode List(string id, List<SlintNode> items)
        {
            return new SlintNode
            {
                Id = id,
                NodeType = "List",
                Items = items
            };
        }

        public static SlintNode AddChild(this SlintNode parent, SlintNode child)
        {
            parent.Children.Add(child);
            return parent;
        }

        public static SlintNode AddChildren(this SlintNode parent, params SlintNode[] children)
        {
            foreach (var child in children)
            {
                parent.Children.Add(child);
            }
            return parent;
        }

        public static SlintNode SetId(this SlintNode node, string id)
        {
            node.Id = id;
            return node;
        }
    }
}
