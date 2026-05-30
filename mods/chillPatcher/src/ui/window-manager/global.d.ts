/// <reference path="./node_modules/onejs-core/definitions/index.d.ts" />

import { HTMLAttributes } from "preact/src/jsx";

declare global {
    namespace JSX {
        interface IntrinsicElements {
            /**
             * 自定义毛玻璃面板组件
             */
            "blur-panel": HTMLAttributes<HTMLElement> & {
                "blur-radius"?: number;      // 旧版属性兼容
                "blur-iterations"?: number;  // 模糊迭代次数
                "downsample"?: number;       // 采样倍率 (分辨率缩放)
                "interval"?: number;         // 更新间隔帧数
                "tint"?: string;             // 叠加颜色 (hex)
                "capture-divisor"?: number;  // 抓取分辨率倍率
                class?: string;
                style?: any;
            };

            /**
             * 自定义场景摄像机视图组件
             */
            "camera-view": HTMLAttributes<HTMLElement> & {
                "fov"?: number;              // 视场角 (1-179, 默认 60)
                "interval"?: number;         // 渲染帧间隔 (1+, 默认 2)
                "resolution-scale"?: number; // 输出分辨率缩放 (0.1-2, 默认 0.5)
                "pos-x"?: number;            // 摄像机 X 位置
                "pos-y"?: number;            // 摄像机 Y 位置
                "pos-z"?: number;            // 摄像机 Z 位置
                "rot-x"?: number;            // 摄像机 X 旋转
                "rot-y"?: number;            // 摄像机 Y 旋转
                "rot-z"?: number;            // 摄像机 Z 旋转
                "near-clip"?: number;        // 近裁切面 (默认 0.3)
                "far-clip"?: number;         // 远裁切面 (默认 1000)
                "clear-color"?: string;      // 清除色 (hex, 默认 #000000)
                "depth"?: number;            // 摄像机深度/优先级
                "culling-mask"?: number;     // 剔除罩码
                class?: string;
                style?: any;
            };

            /**
             * 通用 2D 画布组件 (Painter2D)
             * 通过 ref.current.ve 调用绘图方法:
             *   BeginPath, ClosePath, MoveTo, LineTo, Arc, ArcTo,
             *   BezierCurveTo, QuadraticCurveTo, Fill, Stroke,
             *   SetFillColor, SetStrokeColor, SetLineWidth,
             *   SetLineCap, SetLineJoin, ClearCommands, Commit
             */
            "canvas-2d": HTMLAttributes<HTMLElement> & {
                class?: string;
                style?: any;
            };
        }
    }
}