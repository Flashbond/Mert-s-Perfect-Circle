import React, { useEffect, useState } from "react";
import { trigger, call, bindValue, useValue } from "cs2/api";
import crossWalkIcon from "./Icons/CrossWalk.svg";
import { formatMeters, formatSmart } from "./utils/Formatters";
import { VanillaResolver } from "./utils/VanilliaResolver";
import { parseActiveTool } from "./utils/ActiveTool";
import { MertListBox } from './utils/MertListBox';

const activeToolMode$ = bindValue<string>("MertsToolBox", "ActiveTool", "None|None");
const toolBoxVisible$ = bindValue<boolean>("MertsToolBox", "IsToolBoxAllowed");

// Shape bindings
const shapeNames$ = bindValue<string[]>("MertsToolBox", "ShapeNamesArray");
const shapeCurrentIndex$ = bindValue<number>("MertsToolBox", "ShapeCurrentIndex");
const shapeMaxIndex$ = bindValue<number>("MertsToolBox", "ShapeMaxIndex");

// Dimension Bindings
const shapeDimension$ = bindValue<number>("MertsToolBox", "ShapeDimension");
const shapeDimensionStepValue$ = bindValue<number>("MertsToolBox", "ShapeDimensionStepValue");
const shapeDimensionStepArray$ = bindValue<number[]>("MertsToolBox", "ShapeDimensionStepArray");

const suppressCrosswalks$ = bindValue<boolean>("MertsToolBox", "SuppressCrosswalks", false);

// Diğer Bindings...
const elevationValue$ = bindValue<number>("MertsToolBox", "ElevationValue");
const elevationStepValue$ = bindValue<number>("MertsToolBox", "ElevationStepValue");
const elevationStepArray$ = bindValue<number[]>("MertsToolBox", "ElevationStepArray");

const isSnapGeometryActive$ = bindValue<boolean>("MertsToolBox", "IsSnapGeometryActive");

const presetList$ = bindValue<string>("MertsToolBox", "PresetList", "");

// --- COMPONENT DEFINITION ---
export const ShapePanelSection = () => {

    // --- VISIBILITY & LIFECYCLE ---

    const activeToolRaw = useValue(activeToolMode$) as string;
    const activeTool = parseActiveTool(activeToolRaw);

    const isToolBoxAllowed = useValue(toolBoxVisible$) as boolean;
    const rawShow: boolean = isToolBoxAllowed && activeTool.id === "Shape";
    const [delayedShow, setDelayedShow] = useState(false);

    const [presetPanelOpen, setPresetPanelOpen] = useState(false);

    useEffect(() => {
        let timeoutId: ReturnType<typeof setTimeout> | undefined;

        if (rawShow) {
            setDelayedShow(true);
        } else {
            timeoutId = setTimeout(() => {
                setDelayedShow(false);
                setPresetPanelOpen(false);
            }, 150);
        }

        return () => {
            if (timeoutId) clearTimeout(timeoutId);
        };
    }, [rawShow]);

    const shapeNames = useValue(shapeNames$) || [];
    const currentIndex = useValue(shapeCurrentIndex$) || 0;
    const maxIndex = useValue(shapeMaxIndex$) || 0;
    const currentShapeName = shapeNames[currentIndex] || "Circle";

    // --- DATA BINDING EVALUATION ---
    const dimension = useValue(shapeDimension$) as number;
    const dimensionStepValue = useValue(shapeDimensionStepValue$) as number;
    const dimensionStepValues = useValue(shapeDimensionStepArray$) as number[];

    const suppressCrosswalks = useValue(suppressCrosswalks$) as boolean;

    const elevationValue = useValue(elevationValue$) as number;
    const elevationStepValue = useValue(elevationStepValue$) as number;
    const elevationStepValues = useValue(elevationStepArray$) as number[];

    const isSnapGeometryActive = useValue(isSnapGeometryActive$) as boolean;

    const presetListRaw = useValue(presetList$) as string;

    type PresetListItem = {
        label: string;
        value: string;
    };

    const presetList: PresetListItem[] = (presetListRaw || "")
        .split(";")
        .map((item) => item.trim())
        .filter((item) => item.length > 0)
        .map((item) => {
            const [label, value] = item.split("|");

            return {
                label: label?.trim() ?? item,
                value: value?.trim() ?? label?.trim() ?? item,
            };
        });
    // --- RENDER ---
    if (!delayedShow) return null;

    return (
        <div
            className={`shape-panel-container`}
            onMouseDown={(e) => { e.stopPropagation(); }}
            onContextMenu={(e) => { e.stopPropagation(); }}
            style={{ display: "flex", flexDirection: "column" }}
        >

            <div className={'panel-header'} style={{
                fontSize: "1.1em",
                fontWeight: 600,
                padding: "2rem 10rem"
            }}>{activeTool.name}</div>

            {/* SHAPE ROW */}
            <VanillaResolver.instance.Section title="Shape">
                <VanillaResolver.instance.ToolButton
                    src="Media/Glyphs/ThickStrokeArrowDown.svg"
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    disabled={currentIndex === 0}
                    onSelect={() => trigger("MertsToolBox", "ShapeSidesDown")}
                />

                <div className={VanillaResolver.instance.mouseToolOptionsTheme["number-field"]} style={{ width: "33.33%" }}>
                    {currentShapeName}
                </div>

                <VanillaResolver.instance.ToolButton
                    src="Media/Glyphs/ThickStrokeArrowUp.svg"
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    disabled={currentIndex === maxIndex}
                    onSelect={() => trigger("MertsToolBox", "ShapeSidesUp")}
                />
            </VanillaResolver.instance.Section>

            {/* DIAMETER ROW */}
            <VanillaResolver.instance.Section title="Dimension">
                <VanillaResolver.instance.ToolButton
                    src="Media/Glyphs/ThickStrokeArrowDown.svg"
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={() => trigger("MertsToolBox", "ShapeDimensionDown")}
                />

                <div className={VanillaResolver.instance.mouseToolOptionsTheme["number-field"]}>
                    {formatMeters(dimension)}
                </div>

                <VanillaResolver.instance.ToolButton
                    src="Media/Glyphs/ThickStrokeArrowUp.svg"
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={() => trigger("MertsToolBox", "ShapeDimensionUp")}
                />

                <VanillaResolver.instance.StepToolButton
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    tooltip={`${formatSmart(dimensionStepValue)}`}
                    values={dimensionStepValues}
                    selectedValue={dimensionStepValue}
                    onSelect={(val) => trigger("MertsToolBox", "ShapeDimensionStep", val)}
                />
            </VanillaResolver.instance.Section>

            <VanillaResolver.instance.Section title="Remove Crosswalks">
                <VanillaResolver.instance.ToolButton
                    src={crossWalkIcon}
                    selected={suppressCrosswalks}
                    tooltip={suppressCrosswalks ? "Crosswalks are removed" : "Crosswalks are allowed"}
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={() => trigger("MertsToolBox", "ToggleSuppressCrosswalks")}
                />
            </VanillaResolver.instance.Section>

            {/* ELEVATION ROW */}
            <VanillaResolver.instance.Section title="Elevation">
                <VanillaResolver.instance.ToolButton
                    src="Media/Glyphs/ThickStrokeArrowDown.svg"
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={() => trigger("MertsToolBox", "ElevationDown")}
                />

                <div className={VanillaResolver.instance.mouseToolOptionsTheme["number-field"]}>
                    {formatMeters(elevationValue)}
                </div>

                <VanillaResolver.instance.ToolButton
                    src="Media/Glyphs/ThickStrokeArrowUp.svg"
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={() => trigger("MertsToolBox", "ElevationUp")}
                />

                <VanillaResolver.instance.StepToolButton
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    tooltip={`${elevationStepValue}`}
                    values={elevationStepValues}
                    selectedValue={elevationStepValue}
                    onSelect={(val) => {
                        trigger("MertsToolBox", "ElevationStep", val);
                    }}
                />
            </VanillaResolver.instance.Section>

            {/* SNAP ROW */}
            <VanillaResolver.instance.Section title="Snap">
                <VanillaResolver.instance.ToolButton
                    src="Media/Tools/Snap Options/ExistingGeometry.svg"
                    selected={isSnapGeometryActive}
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={() => trigger("MertsToolBox", "ToggleSnap")}
                    tooltip={`Snap to existing geometry`}
                />
            </VanillaResolver.instance.Section>

            {/* MERT LISTBOX (KLASİK) */}
            <MertListBox
                items={presetList.map(p => p.label)}
                isOpen={presetPanelOpen}
                onToggleOpen={() => {
                    setPresetPanelOpen(!presetPanelOpen);
                    if (!presetPanelOpen) trigger("MertsToolBox", "RefreshPresetList");
                }}
                onSave={async () => {
                    const success = await call<boolean>("MertsToolBox", "SavePreset", activeTool.id);
                    if (success) {
                        trigger("MertsToolBox", "RefreshPresetList");
                    }
                    return success;
                }}
                onSelect={(presetLabel) => {
                    const preset = presetList.find(p => p.label === presetLabel);
                    if (!preset) return;

                    trigger("MertsToolBox", "LoadPreset", preset.value);
                    setPresetPanelOpen(false);
                }}
                onDelete={(presetLabel) => {
                    const preset = presetList.find(p => p.label === presetLabel);
                    if (!preset) return;

                    trigger("MertsToolBox", "DeletePreset", preset.value);
                }}
            />
        </div>
    );
};