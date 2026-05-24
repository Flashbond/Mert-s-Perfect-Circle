import React, { useState, useRef, useEffect} from 'react';
import { createPortal } from 'react-dom';
import styles from './MertListBox.module.scss';
import { Scrollable } from "cs2/ui";
import { VanillaResolver } from "./VanilliaResolver";
import saveIcon from "../Icons/Save.svg";
import loadIcon from "../Icons/Load.svg";

interface MertListBoxProps {
    items: string[];
    selectedItem?: string;
    onSelect: (item: string) => void;
    isOpen: boolean;
    onDelete?: (item: string) => void;
    onSave: () => Promise<boolean>;
    onToggleOpen: () => void;
}
const CustomMasterBalloon = ({ data }: { data: { text: string, top: number, left: number } | null }) => {
    const [displayData, setDisplayData] = React.useState(data);
    const [opacity, setOpacity] = React.useState(0);
    const [isRendered, setIsRendered] = React.useState(false);

    React.useEffect(() => {
        if (data) {
            if (isRendered) {
                setDisplayData(data);
                setOpacity(1);
            } else {
                setDisplayData(data);
                setIsRendered(true);
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => setOpacity(1));
                });
            }
        } else {
            setOpacity(0);
            const timer = setTimeout(() => setIsRendered(false), 200);
            return () => clearTimeout(timer);
        }
    }, [data]);
    if (!isRendered || !displayData) return null;
    return createPortal(
            <div
                className="mert-balloon-master"
                style={{
                    position: 'fixed',
                    pointerEvents: 'none',
                    zIndex: 'var(--tooltipIndex)',
                    left: displayData.left,
                    top: displayData.top,
                    opacity: opacity,
                    transition: 'opacity 0.2s ease, top 0.1s ease',
                    willChange: 'opacity, top',
                }}
            >
            <div className="mert_balloon" style={{
                padding: '0 3rem 6rem',
                transform: 'translate(-50%, -100%)',
                whiteSpace: 'nowrap',
                overflowX: 'hidden',
                overflowY: 'hidden',
                position: 'absolute',
                pointerEvents: 'none'
                }}>
                <div className="mert_bounds" style={{ position: 'relative' }}>
                    <div className="mert-container" style={{
                        position: 'absolute',
                        justifyContent: 'center',
                        flexDirection: 'row',
                        backgroundColor:  'var(--tooltipColor)',
                        filter:  'var(--tooltipFilter)',
                        borderRadius: '4rem',
                        width: '100%',
                        height: '100%',
                        display: 'flex',
                        alignItems: 'flex-end',
                        minWidth: '19.2rem'
                        }}>
                        <div className="mert_arrow" style={{
                            clipPath: 'polygon(50% 100%, 0 0, 100% 0)',
                            top:  '6rem',
                            position: 'relative',
                            width:  '40rem',
                            height:  '20rem',
                            backgroundColor: 'var(--tooltipColor)'}}></div>
                    </div>
                    <div className="mert_content" style={{
                        padding: '7rem 10rem',
                        width: '100.01%',
                        fontSize: 'var(--fontSizeS)',
                        color: 'var(--textColorDim)',
                        position: 'relative',
                        zIndex: 2
                    }}>{displayData.text}</div>
                </div>
            </div>
        </div>,
    document.body
    );
};
export const MertListBox: React.FC<MertListBoxProps> = ({
    items,
    selectedItem,
    onSelect,
    isOpen,
    onDelete,
    onSave,
    onToggleOpen
}) => {
    const [isSaving, setIsSaving] = useState(false);

    const handleSaveClick = async () => {
        setIsSaving(true);
        try {
            const isSuccess = await onSave();

            if (isSuccess) {
                setTimeout(() => {
                    setIsSaving(false);
                }, 150);
            } else {
                setIsSaving(false);
            }
        } catch (error) {
            setIsSaving(false);
        }
    };
    const [tooltipData, setTooltipData] = useState<{ text: string, top: number, left: number } | null>(null);

    const handleItemHover = (e: React.MouseEvent<HTMLLIElement>, text: string) => {
        const textElement = e.currentTarget.firstElementChild as HTMLElement;

        if (textElement) {
            const isEllipsisActive = textElement.scrollWidth > textElement.clientWidth;
            if (!isEllipsisActive) return;


            const rect = textElement.getBoundingClientRect();
            setTooltipData({
                text: text,
                left: rect.left + rect.width / 2,
                top: rect.top
            });
        }
    };
    const handleItemLeave = () => setTooltipData(null);

    useEffect(() => {
        if (!isOpen) {
            setTooltipData(null);
        }
    }, [isOpen]);

    return (
        <>
            {/* PRESET ROW */}
            <VanillaResolver.instance.Section title="Preset">
                <VanillaResolver.instance.ToolButton
                    src={saveIcon}
                    selected={isSaving}
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={handleSaveClick}
                    tooltip="Save Preset"
                />

                <VanillaResolver.instance.ToolButton
                    src={loadIcon}
                    selected={isOpen}
                    focusKey={VanillaResolver.instance.FOCUS_DISABLED}
                    onSelect={onToggleOpen}
                    tooltip="Load Preset"
                />
            </VanillaResolver.instance.Section>

            <div className={styles.mertListboxContainer}>
                <div className={`${styles.actionHint} ${isOpen ? styles.open : ''}`}>
                    <img
                        src="Media/Mouse/RMB.svg"
                        alt="Right Click"
                    /><div>Delete</div>
                </div>
                <div className={`${styles.listboxWrapper} ${isOpen ? styles.open : ''}`}>

                    <CustomMasterBalloon data={tooltipData} />
                    <Scrollable vertical={true} className={styles.customScroll}>
                        <ul className={styles.list}>
                            {items.length === 0 ? (
                                <li className={styles.empty}>Empty List...</li>
                            ) : (
                                items.map((item, index) => (
                                    <li
                                        key={index}
                                        className={`${styles.listItem} ${selectedItem === item ? styles.selected : ''}`}
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            onSelect(item);
                                        }}
                                        onMouseDown={(e) => {
                                            if (e.button === 2) {
                                                e.preventDefault();
                                                e.stopPropagation();
                                                setTooltipData(null);
                                                onDelete?.(item);
                                            }
                                        }}
                                        onMouseEnter={(e) => handleItemHover(e, item)}
                                        onMouseLeave={handleItemLeave}
                                    >
                                        <div className={styles.itemText}>{item}</div>
                                    </li>
                                ))
                            )}
                        </ul>
                    </Scrollable>
                </div>
            </div>
        </>
    );
};