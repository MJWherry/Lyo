"use client";

import { forwardRef, useEffect, useImperativeHandle, type ReactNode } from "react";
import { EditorContent, useEditor, useEditorState, type Editor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import { FontFamily, FontSize, TextStyle } from "@tiptap/extension-text-style";
import Box from "@mui/material/Box";
import Divider from "@mui/material/Divider";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Code from "@mui/icons-material/Code";
import FormatBold from "@mui/icons-material/FormatBold";
import FormatItalic from "@mui/icons-material/FormatItalic";
import FormatListBulleted from "@mui/icons-material/FormatListBulleted";
import FormatListNumbered from "@mui/icons-material/FormatListNumbered";
import FormatQuote from "@mui/icons-material/FormatQuote";
import FormatStrikethrough from "@mui/icons-material/FormatStrikethrough";
import FormatUnderlined from "@mui/icons-material/FormatUnderlined";
import Redo from "@mui/icons-material/Redo";
import Undo from "@mui/icons-material/Undo";

export type LyoRichTextEditorHandle = {
  getHtml: () => string;
  setHtml: (html: string) => void;
  focus: () => void;
};

export type LyoRichTextOption = { label: string; value: string };

export const LYO_RTE_FONTS: LyoRichTextOption[] = [
  { label: "Sans", value: "Inter, Roboto, Helvetica, Arial, sans-serif" },
  { label: "Serif", value: "Georgia, 'Times New Roman', serif" },
  { label: "Mono", value: "'JetBrains Mono', Consolas, 'Courier New', monospace" },
];

export const LYO_RTE_SIZES: LyoRichTextOption[] = ["12px", "14px", "16px", "18px", "24px", "32px"].map((v) => ({ label: v, value: v }));

export const LYO_RTE_CODE_LANGUAGES = ["plaintext", "csharp", "typescript", "javascript", "json", "sql", "python", "bash", "html", "css", "yaml", "xml"];

export type LyoRichTextEditorProps = {
  value: string;
  onChange: (html: string) => void;
  minHeightPx?: number;
  readOnly?: boolean;
  fonts?: LyoRichTextOption[];
  sizes?: LyoRichTextOption[];
  codeLanguages?: string[];
};

const DEFAULT = "__default__";

function ToolButton({ title, active, disabled, onClick, children }: { title: string; active?: boolean; disabled?: boolean; onClick: () => void; children: ReactNode }) {
  return (
    <Tooltip title={title}>
      <span>
        <IconButton size="small" color={active ? "primary" : "default"} disabled={disabled} onClick={onClick} aria-label={title} aria-pressed={active}>
          {children}
        </IconButton>
      </span>
    </Tooltip>
  );
}

function toolbarState(editor: Editor | null) {
  return {
    bold: editor?.isActive("bold") ?? false,
    italic: editor?.isActive("italic") ?? false,
    underline: editor?.isActive("underline") ?? false,
    strike: editor?.isActive("strike") ?? false,
    bullet: editor?.isActive("bulletList") ?? false,
    ordered: editor?.isActive("orderedList") ?? false,
    quote: editor?.isActive("blockquote") ?? false,
    code: editor?.isActive("codeBlock") ?? false,
    language: (editor?.getAttributes("codeBlock").language as string | undefined) ?? "",
    font: (editor?.getAttributes("textStyle").fontFamily as string | undefined) ?? "",
    size: (editor?.getAttributes("textStyle").fontSize as string | undefined) ?? "",
    canUndo: editor?.can().undo() ?? false,
    canRedo: editor?.can().redo() ?? false,
  };
}

/** Tiptap HTML editor with a toolbar for font, size, marks, lists, blockquote, and code blocks with a language. */
export const LyoRichTextEditor = forwardRef<LyoRichTextEditorHandle, LyoRichTextEditorProps>(function LyoRichTextEditor(
  { value, onChange, minHeightPx = 160, readOnly = false, fonts = LYO_RTE_FONTS, sizes = LYO_RTE_SIZES, codeLanguages = LYO_RTE_CODE_LANGUAGES },
  ref
) {
  const editor = useEditor({
    extensions: [StarterKit, TextStyle, FontFamily, FontSize],
    content: value,
    editable: !readOnly,
    immediatelyRender: false,
    onUpdate: ({ editor }) => onChange(editor.getHTML()),
  });
  const state = useEditorState({ editor, selector: ({ editor }) => toolbarState(editor) }) ?? toolbarState(null);

  useEffect(() => {
    if (!editor) return;
    if (editor.getHTML() !== value) editor.commands.setContent(value, { emitUpdate: false });
  }, [editor, value]);

  useEffect(() => {
    editor?.setEditable(!readOnly);
  }, [editor, readOnly]);

  useImperativeHandle(ref, () => ({
    getHtml: () => editor?.getHTML() ?? "",
    setHtml: (html) => editor?.commands.setContent(html),
    focus: () => editor?.commands.focus(),
  }));

  const chain = () => editor!.chain().focus();
  const disabled = !editor || readOnly;

  return (
    <Box className="lyo-rte" sx={{ border: 1, borderColor: "divider", borderRadius: 1 }}>
      <Stack direction="row" spacing={0.5} alignItems="center" flexWrap="wrap" useFlexGap sx={{ px: 0.5, py: 0.5, borderBottom: 1, borderColor: "divider" }}>
        <TextField
          select
          size="small"
          label="Font"
          value={state.font || DEFAULT}
          disabled={disabled}
          onChange={(e) => (e.target.value === DEFAULT ? chain().unsetFontFamily().run() : chain().setFontFamily(e.target.value).run())}
          sx={{ width: 110 }}
        >
          <MenuItem value={DEFAULT}>Default</MenuItem>
          {fonts.map((f) => (
            <MenuItem key={f.value} value={f.value} sx={{ fontFamily: f.value }}>
              {f.label}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          size="small"
          label="Size"
          value={state.size || DEFAULT}
          disabled={disabled}
          onChange={(e) => (e.target.value === DEFAULT ? chain().unsetFontSize().run() : chain().setFontSize(e.target.value).run())}
          sx={{ width: 96 }}
        >
          <MenuItem value={DEFAULT}>Default</MenuItem>
          {sizes.map((s) => (
            <MenuItem key={s.value} value={s.value}>
              {s.label}
            </MenuItem>
          ))}
        </TextField>
        <Divider orientation="vertical" flexItem />
        <ToolButton title="Bold" active={state.bold} disabled={disabled} onClick={() => chain().toggleBold().run()}>
          <FormatBold fontSize="small" />
        </ToolButton>
        <ToolButton title="Italic" active={state.italic} disabled={disabled} onClick={() => chain().toggleItalic().run()}>
          <FormatItalic fontSize="small" />
        </ToolButton>
        <ToolButton title="Underline" active={state.underline} disabled={disabled} onClick={() => chain().toggleUnderline().run()}>
          <FormatUnderlined fontSize="small" />
        </ToolButton>
        <ToolButton title="Strikethrough" active={state.strike} disabled={disabled} onClick={() => chain().toggleStrike().run()}>
          <FormatStrikethrough fontSize="small" />
        </ToolButton>
        <Divider orientation="vertical" flexItem />
        <ToolButton title="Bulleted list" active={state.bullet} disabled={disabled} onClick={() => chain().toggleBulletList().run()}>
          <FormatListBulleted fontSize="small" />
        </ToolButton>
        <ToolButton title="Numbered list" active={state.ordered} disabled={disabled} onClick={() => chain().toggleOrderedList().run()}>
          <FormatListNumbered fontSize="small" />
        </ToolButton>
        <ToolButton title="Blockquote" active={state.quote} disabled={disabled} onClick={() => chain().toggleBlockquote().run()}>
          <FormatQuote fontSize="small" />
        </ToolButton>
        <ToolButton title="Code block" active={state.code} disabled={disabled} onClick={() => chain().toggleCodeBlock().run()}>
          <Code fontSize="small" />
        </ToolButton>
        {state.code ? (
          <TextField
            select
            size="small"
            label="Language"
            value={state.language || "plaintext"}
            disabled={disabled}
            onChange={(e) => chain().updateAttributes("codeBlock", { language: e.target.value }).run()}
            sx={{ width: 130 }}
          >
            {codeLanguages.map((l) => (
              <MenuItem key={l} value={l}>
                {l}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        <Divider orientation="vertical" flexItem />
        <ToolButton title="Undo" disabled={disabled || !state.canUndo} onClick={() => chain().undo().run()}>
          <Undo fontSize="small" />
        </ToolButton>
        <ToolButton title="Redo" disabled={disabled || !state.canRedo} onClick={() => chain().redo().run()}>
          <Redo fontSize="small" />
        </ToolButton>
      </Stack>
      <Box sx={{ minHeight: minHeightPx, px: 1.5, "& blockquote": { borderLeft: 3, borderColor: "divider", pl: 1.5, ml: 0, color: "text.secondary" } }}>
        <EditorContent editor={editor} />
      </Box>
    </Box>
  );
});
