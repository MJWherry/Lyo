"use client";

import type {ReactNode} from "react";
import {LyoButton, LyoStack} from "../primitives/primitives.js";
import {moveDown, moveItem, moveUp, showSectionLabel} from "./reorder.js";

export type LyoReorderListProps<T> = {
    items: readonly T[];
    onChange: (items: T[]) => void;
    renderItem: (item: T) => ReactNode;
    sectionLabel?: (item: T) => string | null;
    onDelete?: (item: T) => void;
};

/** Ordered list. Drag or the arrows change position. Order is the list index. */
export function LyoReorderList<T>({items, onChange, renderItem, sectionLabel, onDelete}: LyoReorderListProps<T>) {
    const drop = (from: number, to: number) => {
        const insertAt = to > from ? to - 1 : to;
        onChange(moveItem(items, from, insertAt));
    };

    return (
        <LyoStack spacing={0.5}>
            {items.map((item, index) => {
                const section = showSectionLabel(items, index, sectionLabel);
                return (
                    <div key={index}>
                        {section ? <strong>{section}</strong> : null}
                        <LyoStack
                            direction="row"
                            spacing={1}
                            alignItems="center"
                            draggable
                            onDragStart={(event) => event.dataTransfer.setData("text/plain", String(index))}
                            onDragOver={(event) => event.preventDefault()}
                            onDrop={(event) => {
                                event.preventDefault();
                                const from = Number(event.dataTransfer.getData("text/plain"));
                                if (Number.isFinite(from)) drop(from, index);
                            }}
                        >
                            <span>{index + 1}</span>
                            <div>{renderItem(item)}</div>
                            <LyoButton size="small" onClick={() => onChange(moveUp(items, index))} disabled={index === 0}>
                                Up
                            </LyoButton>
                            <LyoButton size="small" onClick={() => onChange(moveDown(items, index))} disabled={index === items.length - 1}>
                                Down
                            </LyoButton>
                            {onDelete ? (
                                <LyoButton size="small" onClick={() => onDelete(item)}>
                                    Delete
                                </LyoButton>
                            ) : null}
                        </LyoStack>
                    </div>
                );
            })}
        </LyoStack>
    );
}
