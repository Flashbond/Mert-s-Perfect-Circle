import React from 'react';
import styles from './MertListBox.module.scss';
import { Tooltip } from "cs2/ui";
import { Scrollable } from "cs2/ui";
interface MertListBoxProps {
    items: string[];
    selectedItem?: string;
    onSelect: (item: string) => void;
    isOpen: boolean;
    onDelete?: (item: string) => void;
}

export const MertListBox: React.FC<MertListBoxProps> = ({
    items,
    selectedItem,
    onSelect,
    isOpen,
    onDelete
}) => {

    return (
        <div className={`${styles.listboxWrapper} ${isOpen ? styles.open : ''}`}>
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
                                    onDelete?.(item);
                                }
                            }}
                        >
                            <Tooltip tooltip={item}>
                                <div className={styles.itemText}>{item}</div>
                            </Tooltip>
                        </li>
                    ))
                )}
                </ul>
            </Scrollable>
        </div>
    );
};