import { ModRegistrar } from "cs2/modding";
import React, { useEffect, useLayoutEffect } from "react";
import { trigger, bindValue, useValue } from "cs2/api";

import { VanillaResolver } from "./utils/VanilliaResolver";
import { parseActiveTool, ActiveTool } from "./utils/ActiveTool";

import { CirclePanelSection } from "./CirclePanelSection";
import { HelixPanelSection } from "./HelixPanelSection";
import { SoftBlockPanelSection } from "./SoftBlockPanelSection";
import { GridPanelSection } from "./GridPanelSection";
import { ToolBoxActionHints } from "./utils/ToolBoxActionHints";

import circleIcon from "./Icons/Circle.svg";
import helixIcon from "./Icons/Helix.svg";
import softBlockIcon from "./Icons/SoftBlock.svg";
import gridIcon from "./Icons/SmartGrid.svg";

type ToolDef = {
    id: string;
    icon: string;
    tooltip: string;
};

const ModId = "MertsToolBox";

const toolList$ = bindValue<string>(ModId, "ToolList", "");
const activeToolMode$ = bindValue<string>(ModId, "ActiveTool", "None|None");
const isToolBoxAllowed$ = bindValue<boolean>(ModId, "IsToolBoxAllowed", false);

const icons: Record<string, string> = {
    Circle: circleIcon,
    Helix: helixIcon,
    SoftBlock: softBlockIcon,
    Grid: gridIcon
};

let hasPreloadedIcons = false;
let pendingOneShotCleanup = false;

function buildToolDefs(toolListRaw: string): ToolDef[] {
    if (!toolListRaw) return [];

    return toolListRaw
        .split(";")
        .map((entry: string) => {
            const [id, name, icon] = entry.split("|");

            return {
                id: id || "",
                icon: icons[icon || id] ?? "",
                tooltip: name || id || ""
            };
        })
        .filter((tool: ToolDef) => tool.id !== "" && tool.icon !== "");
}

function preloadAllToolIcons() {
    if (hasPreloadedIcons) return;
    hasPreloadedIcons = true;

    Object.values(icons).forEach((src: string) => {
        const img = new Image();
        img.src = src;
    });
}

const ToolBoxModeRow = () => {
    const activeToolRaw = useValue(activeToolMode$) as string;
    const activeTool = parseActiveTool(activeToolRaw);

    const toolsJson = useValue(toolList$) as string;
    const toolDefs = buildToolDefs(toolsJson);
    function hideForeignMouseToolRowsOnce() {
        const root = document.querySelector(".merts-toolbox-root") as HTMLElement | null;
        if (!root) return;

        const parent = root.parentElement;
        if (!parent) return;

        Array.from(parent.children).forEach((child) => {
            if (child === root) return;

            const el = child as HTMLElement;
            el.style.display = "none";
        });
    }
    return (
        <VanillaResolver.instance.Section title="Mert's ToolBox">
            {toolDefs.map((tool: ToolDef) => {
                const isSelected = activeTool.id === tool.id;

                return (
                    <VanillaResolver.instance.ToolButton
                        key={tool.id}
                        src={tool.icon}
                        selected={isSelected}
                        tooltip={tool.tooltip}
                        focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                        onSelect={() => {
                            pendingOneShotCleanup = true;
                            trigger(ModId, "ToggleTool", tool.id);
                        }}
                    />
                );
            })}
        </VanillaResolver.instance.Section>
    );
};

const register: ModRegistrar = (moduleRegistry) => {
    VanillaResolver.setRegistry(moduleRegistry);

    const mouseToolPath = "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx";

    moduleRegistry.extend(mouseToolPath, "MouseToolOptions", (OriginalMouseToolOptions: any) => {
        return (props: any) => {
            const isAllowed = useValue(isToolBoxAllowed$) as boolean;
            const activeToolJson = useValue(activeToolMode$) as string;
            const activeTool = parseActiveTool(activeToolJson);
            const isActive = isAllowed && activeTool.id !== "None";

            useLayoutEffect(() => {
                if (!isActive) return;

                const hideForeignRows = () => {
                    const root = document.querySelector(".merts-toolbox-root") as HTMLElement | null;
                    if (!root) return;

                    Array.from(root.children).forEach((child) => {
                        const el = child as HTMLElement;

                        const isOurPanel =
                            el.classList.contains("circle-panel-container") ||
                            el.classList.contains("helix-panel-container") ||
                            el.classList.contains("softblock-panel-container") ||
                            el.classList.contains("grid-panel-container");

                        if (isOurPanel) return;

                        if (el.className.includes("item_")) {
                            el.style.display = "none";
                        }
                    });
                };

                hideForeignRows();

                const raf = requestAnimationFrame(() => {
                    hideForeignRows();
                });

                const t = setTimeout(() => {
                    hideForeignRows();
                }, 16);

                return () => {
                    cancelAnimationFrame(raf);
                    clearTimeout(t);
                };
            }, [isActive, activeTool.id]);

            useEffect(() => {
                preloadAllToolIcons();
            }, []);

            if (!isActive) {
                return (
                    <>
                        <OriginalMouseToolOptions {...props} />
                        {isAllowed && <ToolBoxModeRow />}
                    </>
                );
            }

            return (
                <div
                    className="merts-toolbox-root"
                    style={{
                        width: "100%",
                        display: "flex",
                        flexDirection: "column",
                        pointerEvents: "auto"
                    }}
                >
                    <ToolBoxActionHints />

                    <CirclePanelSection />
                    <HelixPanelSection />
                    <SoftBlockPanelSection />
                    <GridPanelSection />
                </div>
            );
        };
    });
};

export default register;