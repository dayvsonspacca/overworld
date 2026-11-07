import QtQuick
import qs.theme

Canvas {
    id: root

    readonly property real cell: 140
    readonly property real jitter: 0.45
    readonly property point glow: Qt.point(0.72, 0.38)
    readonly property real hue: Theme.accent.hslHue
    readonly property bool lightTheme: Theme.light

    function vertices(cols: int, rows: int, random: var): var {
        const w = width / cols;
        const h = height / rows;
        const points = [];
        for (let r = 0; r <= rows; r++) {
            for (let c = 0; c <= cols; c++) {
                const dx = c > 0 && c < cols ? (random() - 0.5) * w * jitter : 0;
                const dy = r > 0 && r < rows ? (random() - 0.5) * h * jitter : 0;
                points.push(Qt.point(c * w + dx, r * h + dy));
            }
        }
        return points;
    }

    function shade(triangle: var, random: var): var {
        const cx = (triangle[0].x + triangle[1].x + triangle[2].x) / 3;
        const cy = (triangle[0].y + triangle[1].y + triangle[2].y) / 3;
        const distance = Math.hypot(cx - glow.x * width, cy - glow.y * height) / width;
        const light = Math.max(0, 1 - distance * 2.1);
        const tone = hue + random() * 0.03;
        const lightness = lightTheme ? 0.95 - light * 0.2 - random() * 0.04 : 0.1 + light * 0.32 + random() * 0.05;
        return {
            fill: Qt.hsla(tone, 0.18 + light * 0.4, lightness, 1),
            edge: Qt.hsla(hue, 0.3, lightness + (lightTheme ? -0.03 : 0.04), 1)
        };
    }

    function draw(ctx: var, triangle: var, colors: var): void {
        ctx.beginPath();
        ctx.moveTo(triangle[0].x, triangle[0].y);
        ctx.lineTo(triangle[1].x, triangle[1].y);
        ctx.lineTo(triangle[2].x, triangle[2].y);
        ctx.closePath();
        ctx.fillStyle = colors.fill;
        ctx.fill();
        ctx.strokeStyle = colors.edge;
        ctx.stroke();
    }

    onWidthChanged: requestPaint()
    onHeightChanged: requestPaint()
    onHueChanged: requestPaint()
    onLightThemeChanged: requestPaint()

    onPaint: {
        let seed = 7;
        const random = () => (seed = seed * 16807 % 2147483647) / 2147483647;
        const cols = Math.max(2, Math.round(width / cell));
        const rows = Math.max(2, Math.round(height / cell));
        const points = vertices(cols, rows, random);
        const at = (r, c) => points[r * (cols + 1) + c];

        const ctx = getContext("2d");
        ctx.lineWidth = 0.6;
        for (let r = 0; r < rows; r++) {
            for (let c = 0; c < cols; c++) {
                const a = at(r, c), b = at(r, c + 1), d = at(r + 1, c), e = at(r + 1, c + 1);
                for (const triangle of [[a, b, e], [a, e, d]])
                    draw(ctx, triangle, shade(triangle, random));
            }
        }
    }
}
