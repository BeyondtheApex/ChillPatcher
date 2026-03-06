// ChillPatcher OneJS - Entry Script
// This renders a simple overlay using UIToolkit via the OneJS DOM bridge.

(function () {
    var doc = ___document;

    // Create a container div
    var container = doc.createElement("div");
    container.style.position = "absolute";
    container.style.bottom = "20px";
    container.style.right = "20px";
    container.style.backgroundColor = "rgba(0, 0, 0, 0.7)";
    container.style.color = "white";
    container.style.paddingTop = "12px";
    container.style.paddingBottom = "12px";
    container.style.paddingLeft = "20px";
    container.style.paddingRight = "20px";
    container.style.borderTopLeftRadius = "8px";
    container.style.borderTopRightRadius = "8px";
    container.style.borderBottomLeftRadius = "8px";
    container.style.borderBottomRightRadius = "8px";
    container.style.fontSize = "14px";

    // Create title
    var title = doc.createElement("div");
    title.style.fontSize = "16px";
    title.style.marginBottom = "4px";
    title.style.color = "#4FC3F7";
    title.textContent = "ChillPatcher OneJS";

    // Create subtitle
    var subtitle = doc.createElement("div");
    subtitle.style.fontSize = "12px";
    subtitle.style.color = "#BDBDBD";
    subtitle.textContent = "UI Engine Ready - Edit ui/app.js to customize";

    container.appendChild(title);
    container.appendChild(subtitle);
    doc.body.appendChild(container);
})();
