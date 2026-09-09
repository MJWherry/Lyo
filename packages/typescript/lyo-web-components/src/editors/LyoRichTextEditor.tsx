"use client";

import { useEffect, useImperativeHandle, forwardRef } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Box from "@mui/material/Box";
import IconButton from "@mui/material/IconButton";
import Stack from "@mui/material/Stack";
import FormatBold from "@mui/icons-material/FormatBold";
import FormatItalic from "@mui/icons-material/FormatItalic";
import FormatListBulleted from "@mui/icons-material/FormatListBulleted";

export type LyoRichTextEditorHandle = {
  getHtml: () => string;
  setHtml: (html: string) => void;
  focus: () => void;
};

export const LyoRichTextEditor = forwardRef<
  LyoRichTextEditorHandle,
  { value: string; onChange: (html: string) => void; minHeightPx?: number }
>(function LyoRichTextEditor({ value, onChange, minHeightPx = 160 }, ref) {
  const editor = useEditor({
    extensions: [StarterKit],
    content: value,
    immediatelyRender: false,
    onUpdate: ({ editor }) => onChange(editor.getHTML()),
  });

  useEffect(() => {
    if (!editor) return;
    if (editor.getHTML() !== value) editor.commands.setContent(value, { emitUpdate: false });
  }, [editor, value]);

  useImperativeHandle(ref, () => ({
    getHtml: () => editor?.getHTML() ?? "",
    setHtml: (html) => editor?.commands.setContent(html),
    focus: () => editor?.commands.focus(),
  }));

  return (
    <Box className="lyo-rte" sx={{ border: 1, borderColor: "divider", borderRadius: 1 }}>
      <Stack direction="row" spacing={0.5} sx={{ px: 0.5, borderBottom: 1, borderColor: "divider" }}>
        <IconButton size="small" onClick={() => editor?.chain().focus().toggleBold().run()}>
          <FormatBold fontSize="small" />
        </IconButton>
        <IconButton size="small" onClick={() => editor?.chain().focus().toggleItalic().run()}>
          <FormatItalic fontSize="small" />
        </IconButton>
        <IconButton size="small" onClick={() => editor?.chain().focus().toggleBulletList().run()}>
          <FormatListBulleted fontSize="small" />
        </IconButton>
      </Stack>
      <Box sx={{ minHeight: minHeightPx }}>
        <EditorContent editor={editor} />
      </Box>
    </Box>
  );
});
