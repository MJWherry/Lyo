"use client";

import {useMemo, useState} from "react";
import {LyoCheckbox, LyoStack, LyoTextField} from "../primitives/primitives.js";
import {createTreeLoader, filterNodes, type FileTreeNode} from "./fileTree.js";

export type LyoFileTreeProps = {
    rootPath?: string;
    listChildren: (path: string) => Promise<readonly FileTreeNode[]>;
    multiSelect?: boolean;
    selected?: readonly string[];
    onSelectedChange?: (paths: string[]) => void;
};

/** Lazy folder tree. Children load when a directory opens. */
export function LyoFileTree({rootPath = "/", listChildren, multiSelect = false, selected = [], onSelectedChange}: LyoFileTreeProps) {
    const loader = useMemo(() => createTreeLoader(listChildren), [listChildren]);
    const [query, setQuery] = useState("");
    const [open, setOpen] = useState<Record<string, readonly FileTreeNode[]>>({});

    const toggle = async (path: string) => {
        if (open[path]) {
            setOpen((current) => {
                const next = {...current};
                delete next[path];
                return next;
            });
            return;
        }
        const children = await loader.expand(path);
        setOpen((current) => ({...current, [path]: children}));
    };

    const renderNodes = (nodes: readonly FileTreeNode[], depth: number) =>
        filterNodes(nodes, query).map((node) => (
            <div key={node.path} style={{paddingLeft: depth * 12}}>
                <LyoStack direction="row" spacing={1} alignItems="center">
                    {multiSelect ? (
                        <LyoCheckbox
                            checked={selected.includes(node.path)}
                            onChange={(_event, checked) => {
                                const next = checked ? [...selected, node.path] : selected.filter((path) => path !== node.path);
                                onSelectedChange?.(next);
                            }}
                        />
                    ) : null}
                    {node.isDirectory ? (
                        <button type="button" onClick={() => void toggle(node.path)}>
                            {node.name}
                        </button>
                    ) : (
                        <span>{node.name}</span>
                    )}
                </LyoStack>
                {open[node.path] ? renderNodes(open[node.path], depth + 1) : null}
            </div>
        ));

    return (
        <LyoStack spacing={1}>
            <LyoTextField label="Filter" value={query} onChange={(event) => setQuery(event.target.value)} />
            <button type="button" onClick={() => void toggle(rootPath)}>
                {rootPath}
            </button>
            {open[rootPath] ? renderNodes(open[rootPath], 1) : null}
        </LyoStack>
    );
}
